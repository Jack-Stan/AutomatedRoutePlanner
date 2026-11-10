using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
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
    /// PUT routestops/{id}/status and PUT routestops/{id}/instructions, plus the shape of the
    /// numeric enum values the frontend has to agree with.
    /// </summary>
    public class RouteStopEndpointTests : IClassFixture<HoppyApiFactory>
    {
        private const string Password = "test-only-user-password";
        private const int Pending = 1, InProgress = 2, Completed = 3, Skipped = 4; // RouteStopStatus on the wire

        private readonly HoppyApiFactory _factory;
        private readonly HttpClient _client;

        public RouteStopEndpointTests(HoppyApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateSeededClient();
        }

        private async Task<(int Id, string Token)> UserAsync(UserRole role)
        {
            var username = $"{role.ToString().ToLowerInvariant()}.{Guid.NewGuid():N}";
            var id = _factory.SeedUser(role, username, Password);
            var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = username, Password = Password });
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            return (id, (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token);
        }

        private Task<HttpResponseMessage> PutAsync(string url, string? token, object body)
        {
            var request = new HttpRequestMessage(HttpMethod.Put, url) { Content = JsonContent.Create(body) };
            if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return _client.SendAsync(request);
        }

        private int SeedStop(int swapperId, RouteStopStatus status = RouteStopStatus.Pending)
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
                Status = RouteStatus.Confirmed,
                CreatedAt = DateTime.UtcNow
            };
            db.Routes.Add(route);
            db.SaveChanges();
            var stop = new RouteStop { RouteId = route.Id, VehicleId = vehicle.Id, Order = 1, Status = status };
            db.RouteStops.Add(stop);
            db.SaveChanges();
            return stop.Id;
        }

        private RouteStop ReadStop(int id)
        {
            using var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<HoppyDbContext>().RouteStops.AsNoTracking().Single(s => s.Id == id);
        }

        // ================= PUT routestops/{id}/status =================

        [Fact]
        public async Task Status_WithoutToken_Returns401()
        {
            (await PutAsync("/api/routestops/1/status", null, new { status = Skipped })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Status_OnAnotherSwappersStop_Returns403_AndChangesNothing()
        {
            var (ownerId, _) = await UserAsync(UserRole.BatterySwapper);
            var (_, otherToken) = await UserAsync(UserRole.BatterySwapper);
            var stopId = SeedStop(ownerId);

            (await PutAsync($"/api/routestops/{stopId}/status", otherToken, new { status = Skipped })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ReadStop(stopId).Status.Should().Be(RouteStopStatus.Pending);
        }

        [Fact]
        public async Task Status_UnknownStop_Returns404_ForSwapperAndManager()
        {
            var (_, swapperToken) = await UserAsync(UserRole.BatterySwapper);
            var (_, adminToken) = await UserAsync(UserRole.Admin);
            (await PutAsync("/api/routestops/987654/status", swapperToken, new { status = Skipped })).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await PutAsync("/api/routestops/987654/status", adminToken, new { status = Skipped })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Status_AssignedSwapper_CanSkipTheirOwnStop()
        {
            var (swapperId, token) = await UserAsync(UserRole.BatterySwapper);
            var stopId = SeedStop(swapperId);

            var response = await PutAsync($"/api/routestops/{stopId}/status", token, new { status = Skipped });

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("id").GetInt32().Should().Be(stopId);
            doc.RootElement.GetProperty("status").GetInt32().Should().Be(Skipped); // numeric on the wire
            ReadStop(stopId).Status.Should().Be(RouteStopStatus.Skipped);
        }

        [Fact]
        public async Task Status_AssignedSwapper_CanStartTheirStop_AndArrivalIsStamped()
        {
            var (swapperId, token) = await UserAsync(UserRole.BatterySwapper);
            var stopId = SeedStop(swapperId);

            var response = await PutAsync($"/api/routestops/{stopId}/status", token, new { status = InProgress });

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            var stop = ReadStop(stopId);
            stop.Status.Should().Be(RouteStopStatus.InProgress);
            stop.ActualArrivalTime.Should().NotBeNull();
        }

        [Fact]
        public async Task Status_FleetManager_CanSkipAnySwappersStop()
        {
            var (swapperId, _) = await UserAsync(UserRole.BatterySwapper);
            var (_, managerToken) = await UserAsync(UserRole.FleetManager);
            var stopId = SeedStop(swapperId);

            (await PutAsync($"/api/routestops/{stopId}/status", managerToken, new { status = Skipped })).StatusCode.Should().Be(HttpStatusCode.OK);
            ReadStop(stopId).Status.Should().Be(RouteStopStatus.Skipped);
        }

        [Theory]
        [InlineData(Pending)]
        [InlineData(Completed)] // completing has its own endpoint, which stamps the times
        [InlineData(0)]
        [InlineData(99)]
        public async Task Status_OtherValues_Return400_AndChangeNothing(int status)
        {
            var (swapperId, token) = await UserAsync(UserRole.BatterySwapper);
            var stopId = SeedStop(swapperId);

            (await PutAsync($"/api/routestops/{stopId}/status", token, new { status })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ReadStop(stopId).Status.Should().Be(RouteStopStatus.Pending);
        }

        [Theory]
        [InlineData(RouteStopStatus.Completed)]
        [InlineData(RouteStopStatus.Skipped)]
        public async Task Status_OnAFinishedStop_Returns409(RouteStopStatus finished)
        {
            var (swapperId, token) = await UserAsync(UserRole.BatterySwapper);
            var stopId = SeedStop(swapperId, finished);

            (await PutAsync($"/api/routestops/{stopId}/status", token, new { status = Skipped })).StatusCode.Should().Be(HttpStatusCode.Conflict);
            ReadStop(stopId).Status.Should().Be(finished);
        }

        // ================= PUT routestops/{id}/instructions =================

        [Fact]
        public async Task Instructions_WithoutToken_Returns401()
        {
            (await PutAsync("/api/routestops/1/instructions", null, new { notes = "x" })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Instructions_AsSwapper_Returns403_EvenOnTheirOwnStop()
        {
            var (swapperId, token) = await UserAsync(UserRole.BatterySwapper);
            var stopId = SeedStop(swapperId);

            (await PutAsync($"/api/routestops/{stopId}/instructions", token, new { notes = "test-only note" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            ReadStop(stopId).Notes.Should().BeNull();
        }

        [Theory]
        [InlineData(UserRole.Admin)]
        [InlineData(UserRole.FleetManager)]
        public async Task Instructions_AsManager_SetsAndReturnsTheNote(UserRole role)
        {
            var (swapperId, _) = await UserAsync(UserRole.BatterySwapper);
            var (_, token) = await UserAsync(role);
            var stopId = SeedStop(swapperId);

            var response = await PutAsync($"/api/routestops/{stopId}/instructions", token, new { notes = "test-only note" });

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("notes").GetString().Should().Be("test-only note");
            ReadStop(stopId).Notes.Should().Be("test-only note");
        }

        [Fact]
        public async Task Instructions_AreVisibleToTheAssignedSwapperOnTheRoute()
        {
            var (swapperId, swapperToken) = await UserAsync(UserRole.BatterySwapper);
            var (_, managerToken) = await UserAsync(UserRole.FleetManager);
            var stopId = SeedStop(swapperId);
            var routeId = ReadStop(stopId).RouteId;
            (await PutAsync($"/api/routestops/{stopId}/instructions", managerToken, new { notes = "test-only note" })).StatusCode.Should().Be(HttpStatusCode.OK);

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/routes/{routeId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", swapperToken);
            var route = await _client.SendAsync(request);

            route.StatusCode.Should().Be(HttpStatusCode.OK);
            (await route.Content.ReadAsStringAsync()).Should().Contain("test-only note");
        }

        [Fact]
        public async Task Instructions_EmptyNote_ClearsIt()
        {
            var (swapperId, _) = await UserAsync(UserRole.BatterySwapper);
            var (_, token) = await UserAsync(UserRole.Admin);
            var stopId = SeedStop(swapperId);
            await PutAsync($"/api/routestops/{stopId}/instructions", token, new { notes = "test-only note" });

            (await PutAsync($"/api/routestops/{stopId}/instructions", token, new { notes = "" })).StatusCode.Should().Be(HttpStatusCode.OK);
            ReadStop(stopId).Notes.Should().BeNullOrEmpty();
        }

        [Fact]
        public async Task Instructions_OverFiveHundredCharacters_Returns400_AndChangesNothing()
        {
            var (swapperId, _) = await UserAsync(UserRole.BatterySwapper);
            var (_, token) = await UserAsync(UserRole.Admin);
            var stopId = SeedStop(swapperId);

            (await PutAsync($"/api/routestops/{stopId}/instructions", token, new { notes = new string('x', 501) })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
            ReadStop(stopId).Notes.Should().BeNull();
        }

        [Fact]
        public async Task Instructions_UnknownStop_Returns404()
        {
            var (_, token) = await UserAsync(UserRole.Admin);
            (await PutAsync("/api/routestops/987654/instructions", token, new { notes = "x" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        // ================= wire format the frontend must match =================

        [Fact]
        public async Task StopStatus_IsNumeric_AndPendingIsOne()
        {
            var (swapperId, token) = await UserAsync(UserRole.BatterySwapper);
            var stopId = SeedStop(swapperId);
            var routeId = ReadStop(stopId).RouteId;

            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/routes/{routeId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await _client.SendAsync(request);

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("stops")[0].GetProperty("status").GetInt32().Should().Be(Pending);
        }
    }
}
