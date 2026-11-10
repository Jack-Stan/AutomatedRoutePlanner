using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;
using HoppyRoute.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HoppyRoute.Tests.Controllers
{
    /// <summary>
    /// Auth on Vehicles, Zones, Regions, RouteStops and Routes over the real pipeline.
    /// The app's startup seed fills regions, zones and vehicles; routes are seeded per test.
    /// </summary>
    public class ControllerAuthTests : IClassFixture<HoppyApiFactory>
    {
        private const string Password = "test-only-user-password";
        private readonly HoppyApiFactory _factory;
        private readonly HttpClient _client;

        public ControllerAuthTests(HoppyApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateSeededClient();
        }

        // ---- helpers ----

        private async Task<(int Id, string Token)> UserAsync(UserRole role)
        {
            var username = $"{role.ToString().ToLowerInvariant()}.{Guid.NewGuid():N}";
            var id = _factory.SeedUser(role, username, Password);
            var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = username, Password = Password });
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            return (id, (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token);
        }

        private Task<HttpResponseMessage> SendAsync(string method, string url, string? token, object? body = null)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body != null) request.Content = JsonContent.Create(body);
            else if (method is "POST" or "PUT") request.Content = JsonContent.Create(new { });
            return _client.SendAsync(request);
        }

        /// <summary>Seeds a Suggested route with one pending stop, assigned to the given swapper.</summary>
        private (int RouteId, int StopId, int ZoneId) SeedRoute(int swapperId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HoppyDbContext>();
            var vehicle = db.Vehicles.AsNoTracking().First(v => v.ZoneId != null);
            var route = new Route
            {
                Name = $"Test route {Guid.NewGuid():N}",
                AssignedSwapperId = swapperId,
                CreatedByUserId = 1,
                ZoneId = vehicle.ZoneId!.Value,
                Date = DateTime.UtcNow.Date,
                TargetDurationMinutes = 60,
                Status = RouteStatus.Suggested,
                CreatedAt = DateTime.UtcNow
            };
            db.Routes.Add(route);
            db.SaveChanges();
            var stop = new RouteStop { RouteId = route.Id, VehicleId = vehicle.Id, Order = 1, Status = RouteStopStatus.Pending };
            db.RouteStops.Add(stop);
            db.SaveChanges();
            return (route.Id, stop.Id, route.ZoneId);
        }

        private T Read<T>(Func<HoppyDbContext, T> query)
        {
            using var scope = _factory.Services.CreateScope();
            return query(scope.ServiceProvider.GetRequiredService<HoppyDbContext>());
        }

        // ---- 401 without a token: a sample from every controller ----

        [Theory]
        [InlineData("GET", "/api/vehicles/all")]
        [InlineData("GET", "/api/vehicles/lowbattery/all")]
        [InlineData("GET", "/api/vehicles/1")]
        [InlineData("GET", "/api/vehicles/stats/zone/1")]
        [InlineData("GET", "/api/zones")]
        [InlineData("GET", "/api/zones/1")]
        [InlineData("GET", "/api/zones/count")]
        [InlineData("GET", "/api/regions")]
        [InlineData("GET", "/api/regions/countries")]
        [InlineData("GET", "/api/regions/1/zones")]
        [InlineData("POST", "/api/routestops/1/complete")]
        [InlineData("GET", "/api/routes/all")]
        [InlineData("GET", "/api/routes/1")]
        [InlineData("GET", "/api/routes/today?swapperId=1")]
        [InlineData("POST", "/api/routes/1/confirm")]
        [InlineData("POST", "/api/routes/1/approve")]
        [InlineData("POST", "/api/routes/suggest")]
        public async Task WithoutToken_Returns401(string method, string url)
        {
            var response = await SendAsync(method, url, null);
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("/api/vehicles/all")]
        [InlineData("/api/zones")]
        [InlineData("/api/regions")]
        public async Task ReadEndpoints_AsSwapper_Return200(string url)
        {
            var (_, token) = await UserAsync(UserRole.BatterySwapper);
            (await SendAsync("GET", url, token)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // ---- manager-only route actions: 403 for a swapper, nothing changes ----

        [Theory]
        [InlineData("POST", "/api/routes/{0}/approve")]
        [InlineData("POST", "/api/routes/{0}/reject")]
        [InlineData("POST", "/api/routes/suggest")]
        [InlineData("GET", "/api/routes/all")]
        [InlineData("GET", "/api/routes/pending-approval")]
        public async Task ManagerOnlyRouteActions_AsSwapper_Return403_AndChangeNothing(string method, string urlFormat)
        {
            var (swapperId, token) = await UserAsync(UserRole.BatterySwapper);
            var (routeId, _, _) = SeedRoute(swapperId); // even their own route

            var response = await SendAsync(method, string.Format(urlFormat, routeId), token,
                urlFormat.Contains("approve") || urlFormat.Contains("reject") ? new RouteApprovalRequest { ApprovedBy = swapperId } : null);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Read(db => db.Routes.AsNoTracking().Single(r => r.Id == routeId).ApprovalStatus)
                .Should().Be(RouteApprovalStatus.PendingApproval);
        }

        [Theory]
        [InlineData(UserRole.Admin)]
        [InlineData(UserRole.FleetManager)]
        public async Task ApproveRoute_AsManager_Approves(UserRole role)
        {
            var (managerId, token) = await UserAsync(role);
            var (swapperId, _) = await UserAsync(UserRole.BatterySwapper);
            var (routeId, _, _) = SeedRoute(swapperId);

            var response = await SendAsync("POST", $"/api/routes/{routeId}/approve", token, new RouteApprovalRequest { ApprovedBy = managerId });

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            Read(db => db.Routes.AsNoTracking().Single(r => r.Id == routeId).ApprovalStatus).Should().Be(RouteApprovalStatus.Approved);
        }

        [Fact]
        public async Task RejectRoute_AsFleetManager_Rejects()
        {
            var (managerId, token) = await UserAsync(UserRole.FleetManager);
            var (swapperId, _) = await UserAsync(UserRole.BatterySwapper);
            var (routeId, _, _) = SeedRoute(swapperId);

            var response = await SendAsync("POST", $"/api/routes/{routeId}/reject", token, new RouteApprovalRequest { ApprovedBy = managerId });

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            Read(db => db.Routes.AsNoTracking().Single(r => r.Id == routeId).ApprovalStatus).Should().Be(RouteApprovalStatus.Rejected);
        }

        [Theory]
        [InlineData("/api/routes/all")]
        [InlineData("/api/routes/pending-approval")]
        public async Task ManagerReads_AsFleetManager_Return200(string url)
        {
            var (_, token) = await UserAsync(UserRole.FleetManager);
            (await SendAsync("GET", url, token)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // ---- swapper writes on their own route still work ----

        [Fact]
        public async Task CompleteStop_AsAssignedSwapper_Completes()
        {
            var (swapperId, token) = await UserAsync(UserRole.BatterySwapper);
            var (_, stopId, _) = SeedRoute(swapperId);

            var response = await SendAsync("POST", $"/api/routestops/{stopId}/complete", token);

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            Read(db => db.RouteStops.AsNoTracking().Single(s => s.Id == stopId).Status).Should().Be(RouteStopStatus.Completed);
        }

        [Fact]
        public async Task ConfirmRoute_AsAssignedSwapper_Confirms()
        {
            var (swapperId, token) = await UserAsync(UserRole.BatterySwapper);
            var (routeId, _, _) = SeedRoute(swapperId);

            var response = await SendAsync("POST", $"/api/routes/{routeId}/confirm", token);

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            Read(db => db.Routes.AsNoTracking().Single(r => r.Id == routeId).Status).Should().Be(RouteStatus.Confirmed);
        }

        [Fact]
        public async Task GetRoute_AsAssignedSwapper_Returns200()
        {
            var (swapperId, token) = await UserAsync(UserRole.BatterySwapper);
            var (routeId, _, _) = SeedRoute(swapperId);
            (await SendAsync("GET", $"/api/routes/{routeId}", token)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // ---- a swapper cannot touch another swapper's route ----

        [Fact]
        public async Task CompleteStop_OnAnotherSwappersRoute_Returns403_AndLeavesItPending()
        {
            var (ownerId, _) = await UserAsync(UserRole.BatterySwapper);
            var (_, otherToken) = await UserAsync(UserRole.BatterySwapper);
            var (_, stopId, _) = SeedRoute(ownerId);

            (await SendAsync("POST", $"/api/routestops/{stopId}/complete", otherToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Read(db => db.RouteStops.AsNoTracking().Single(s => s.Id == stopId).Status).Should().Be(RouteStopStatus.Pending);
        }

        [Fact]
        public async Task ConfirmRoute_OnAnotherSwappersRoute_Returns403_AndLeavesItSuggested()
        {
            var (ownerId, _) = await UserAsync(UserRole.BatterySwapper);
            var (_, otherToken) = await UserAsync(UserRole.BatterySwapper);
            var (routeId, _, _) = SeedRoute(ownerId);

            (await SendAsync("POST", $"/api/routes/{routeId}/confirm", otherToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Read(db => db.Routes.AsNoTracking().Single(r => r.Id == routeId).Status).Should().Be(RouteStatus.Suggested);
        }

        [Fact]
        public async Task GetRoute_OnAnotherSwappersRoute_Returns403()
        {
            var (ownerId, _) = await UserAsync(UserRole.BatterySwapper);
            var (_, otherToken) = await UserAsync(UserRole.BatterySwapper);
            var (routeId, _, _) = SeedRoute(ownerId);
            (await SendAsync("GET", $"/api/routes/{routeId}", otherToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task TodaysRoute_ForAnotherSwapper_Returns403_ButOwnWorks()
        {
            var (ownerId, ownerToken) = await UserAsync(UserRole.BatterySwapper);
            var (_, otherToken) = await UserAsync(UserRole.BatterySwapper);

            (await SendAsync("GET", $"/api/routes/today?swapperId={ownerId}", otherToken)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            // Own lookup passes the auth check (404 here: no confirmed route today).
            (await SendAsync("GET", $"/api/routes/today?swapperId={ownerId}", ownerToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ---- managers may act on any swapper's route ----

        [Fact]
        public async Task CompleteStop_AsFleetManager_OnAnySwappersRoute_Completes()
        {
            var (_, managerToken) = await UserAsync(UserRole.FleetManager);
            var (swapperId, _) = await UserAsync(UserRole.BatterySwapper);
            var (_, stopId, _) = SeedRoute(swapperId);

            (await SendAsync("POST", $"/api/routestops/{stopId}/complete", managerToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task ConfirmRoute_AsAdmin_OnAnySwappersRoute_Confirms()
        {
            var (_, adminToken) = await UserAsync(UserRole.Admin);
            var (swapperId, _) = await UserAsync(UserRole.BatterySwapper);
            var (routeId, _, _) = SeedRoute(swapperId);

            (await SendAsync("POST", $"/api/routes/{routeId}/confirm", adminToken)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task CompleteStop_UnknownStop_AsSwapper_Returns404()
        {
            var (_, token) = await UserAsync(UserRole.BatterySwapper);
            (await SendAsync("POST", "/api/routestops/987654/complete", token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
