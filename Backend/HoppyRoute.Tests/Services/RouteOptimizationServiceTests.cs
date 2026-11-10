using System.Diagnostics;
using FluentAssertions;
using HoppyRoute.Application.Services;
using HoppyRoute.Domain.Entities;

namespace HoppyRoute.Tests.Services
{
    public class RouteOptimizationServiceTests
    {
        // With R = 6371 km, one degree of arc along a meridian is 6371 * PI / 180 km.
        private const double OneDegreeKm = 6371.0 * Math.PI / 180.0; // ~111.195 km

        private readonly RouteOptimizationService _service = new();

        private static Vehicle V(int id, double lat, double lon) => new()
        {
            Id = id,
            ExternalId = $"HOP-TEST-{id:D4}",
            RegistrationNumber = $"T{id:D4}",
            VehicleType = "E-Scooter",
            Latitude = lat,
            Longitude = lon,
            BatteryLevel = 10,
            LastUpdated = DateTime.UtcNow
        };

        // ---------- Haversine ----------

        [Fact]
        public async Task CalculateDistance_SamePoint_IsZero()
        {
            (await _service.CalculateDistanceAsync(51.0275, 4.4775, 51.0275, 4.4775)).Should().Be(0);
        }

        [Fact]
        public async Task CalculateDistance_OneDegreeOfLatitude_IsAbout111Km()
        {
            var d = await _service.CalculateDistanceAsync(50.0, 4.0, 51.0, 4.0);
            d.Should().BeApproximately(OneDegreeKm, 1e-9);
        }

        [Fact]
        public async Task CalculateDistance_ParisToLondon_MatchesKnownGreatCircleDistance()
        {
            // Paris (Notre-Dame area) to London (Charing Cross area): ~343.5 km great-circle.
            var d = await _service.CalculateDistanceAsync(48.8566, 2.3522, 51.5074, -0.1278);
            d.Should().BeApproximately(343.5, 1.0);
        }

        [Fact]
        public async Task CalculateDistance_IsSymmetric()
        {
            var ab = await _service.CalculateDistanceAsync(51.3667, 3.3917, 50.9667, 5.5000);
            var ba = await _service.CalculateDistanceAsync(50.9667, 5.5000, 51.3667, 3.3917);
            ab.Should().BeApproximately(ba, 1e-9);
            ab.Should().BeInRange(150, 160); // Cadzand -> Genk, ~154 km
        }

        // ---------- Travel time: 30 km/h + 5 min per stop, rounded up ----------

        [Fact]
        public async Task CalculateTravelTime_SamePoint_IsJustTheFiveMinuteStop()
        {
            (await _service.CalculateTravelTimeAsync(51.0, 4.0, 51.0, 4.0)).Should().Be(5);
        }

        [Theory]
        [InlineData(0.1, 28)]  // 11.12 km -> 22.24 min + 5 = 27.24 -> 28
        [InlineData(1.0, 228)] // 111.19 km -> 222.39 min + 5 = 227.39 -> 228
        public async Task CalculateTravelTime_Uses30KmhPlusFiveMinutes_RoundedUp(double degreesNorth, int expectedMinutes)
        {
            var minutes = await _service.CalculateTravelTimeAsync(50.0, 4.0, 50.0 + degreesNorth, 4.0);
            minutes.Should().Be(expectedMinutes);

            var exact = OneDegreeKm * degreesNorth / 30.0 * 60.0 + 5.0;
            minutes.Should().Be((int)Math.Ceiling(exact));
        }

        // ---------- OR-Tools route optimisation ----------

        [Fact]
        public async Task OptimizeRoute_EmptyList_FailsWithMessage()
        {
            var result = await _service.OptimizeRouteAsync(new List<Vehicle>(), 60);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("No vehicles provided for optimization");
            result.OptimalOrder.Should().BeEmpty();
        }

        [Fact]
        public async Task OptimizeRoute_OneVehicle_VisitsItOnce()
        {
            var result = await _service.OptimizeRouteAsync(new List<Vehicle> { V(1, 51.0275, 4.4775) }, 60);

            result.Success.Should().BeTrue(result.ErrorMessage);
            result.OptimalOrder.Should().Equal(0);
            // Depot is the centroid, i.e. the vehicle itself: no distance, just the 5 minute stop.
            result.TotalDistanceKm.Should().BeApproximately(0, 1e-9);
            result.TotalDurationMinutes.Should().Be(5);
        }

        [Fact]
        public async Task OptimizeRoute_SeveralVehicles_VisitsEveryVehicleExactlyOnce_WithinTargetDuration()
        {
            // Six scooters scattered around Mechelen, within ~1.5 km of each other.
            var vehicles = new List<Vehicle>
            {
                V(1, 51.0275, 4.4775), V(2, 51.0300, 4.4800), V(3, 51.0250, 4.4900),
                V(4, 51.0350, 4.4700), V(5, 51.0200, 4.4650), V(6, 51.0320, 4.4880),
            };
            const int target = 120;

            var sw = Stopwatch.StartNew();
            var result = await _service.OptimizeRouteAsync(vehicles, target);
            sw.Stop();

            result.Success.Should().BeTrue(result.ErrorMessage);
            result.OptimalOrder.Should().HaveCount(vehicles.Count);
            result.OptimalOrder.Should().OnlyHaveUniqueItems();
            result.OptimalOrder.Should().BeEquivalentTo(Enumerable.Range(0, vehicles.Count));
            result.TotalDurationMinutes.Should().BeGreaterThanOrEqualTo(vehicles.Count * 5, "every stop costs 5 minutes");
            result.TotalDurationMinutes.Should().BeLessThanOrEqualTo(target);
            // Solver wall-clock limit is 30 s (RouteOptimizationService.cs:47); allow some slack.
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(45));
        }

        [Fact]
        public async Task OptimizeRoute_TargetDurationTooShort_ReportsNoSolution()
        {
            // Two vehicles ~14 km apart: the round trip alone needs ~70 minutes.
            var vehicles = new List<Vehicle> { V(1, 51.0, 4.0), V(2, 51.0, 4.2) };

            var result = await _service.OptimizeRouteAsync(vehicles, 30);

            result.Success.Should().BeFalse();
            result.ErrorMessage.Should().Be("No solution found within time limit");
        }

        [Fact]
        public async Task OptimizeRoute_TotalDistance_StartsFromTheDepotNotFromTheFirstVehicle()
        {
            // Depot is the centroid (51.0, 4.1). Path depot -> first -> second is
            // ~7 km + ~14 km. Regression test: the distance used to be computed from
            // vehicles[0] instead of the depot for the first leg.
            var a = V(1, 51.0, 4.0);
            var b = V(2, 51.0, 4.2);
            var result = await _service.OptimizeRouteAsync(new List<Vehicle> { a, b }, 240);

            result.Success.Should().BeTrue(result.ErrorMessage);
            var visited = result.OptimalOrder.Select(i => new[] { a, b }[i]).ToList();
            var expected =
                await _service.CalculateDistanceAsync(51.0, 4.1, visited[0].Latitude, visited[0].Longitude) +
                await _service.CalculateDistanceAsync(visited[0].Latitude, visited[0].Longitude, visited[1].Latitude, visited[1].Longitude);

            result.TotalDistanceKm.Should().BeApproximately(expected, 0.01);
        }
    }
}
