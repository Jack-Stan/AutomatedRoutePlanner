using System.Net;
using System.Text.Json;
using FluentAssertions;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using static HoppyRoute.Tests.TestSupport.ApiTestHelpers;

namespace HoppyRoute.Tests.Controllers
{
    /// <summary>
    /// The vehicle on each route stop must carry its registration number, type, zone name and flags.
    /// There is no lazy loading in this app, so a zone name can only be present when the same query Included it
    /// (one query with a join, not one extra query per stop).
    /// </summary>
    public class RouteStopVehicleFieldsTests : IClassFixture<HoppyApiFactory>
    {
        private readonly HoppyApiFactory _factory;
        private readonly HttpClient _client;

        public RouteStopVehicleFieldsTests(HoppyApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateSeededClient();
        }

        private async Task<(string Token, int RouteId, int ZoneId, string ZoneName, string Registration)> ArrangeAsync()
        {
            var (swapperId, _) = await _factory.NewUserAsync(_client, UserRole.BatterySwapper);
            var (_, adminToken) = await _factory.NewUserAsync(_client, UserRole.Admin);
            var zoneId = _factory.Geography().ZoneA1;
            var zoneName = _factory.Read(db => db.Zones.AsNoTracking().Single(z => z.Id == zoneId).Name);
            var (routeId, _, registration) = _factory.SeedRoute(swapperId, zoneId);
            return (adminToken, routeId, zoneId, zoneName, registration);
        }

        private static void AssertVehicle(JsonElement stop, string registration, string zoneName, int zoneId)
        {
            var v = stop.GetProperty("vehicle");
            v.GetProperty("registrationNumber").GetString().Should().Be(registration);
            v.GetProperty("vehicleType").GetString().Should().Be("TestScooter");
            v.GetProperty("zoneId").GetInt32().Should().Be(zoneId);
            v.GetProperty("zoneName").GetString().Should().Be(zoneName);
            v.GetProperty("batteryLevel").GetInt32().Should().Be(12);
            v.GetProperty("needsBatteryReplacement").GetBoolean().Should().BeTrue();
            v.GetProperty("isAvailable").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task GetRoute_StopsCarryRegistrationTypeAndZoneName()
        {
            var (token, routeId, zoneId, zoneName, registration) = await ArrangeAsync();

            var response = await _client.SendAsync(Request(HttpMethod.Get, $"/api/routes/{routeId}", token));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            AssertVehicle(doc.RootElement.GetProperty("stops")[0], registration, zoneName, zoneId);
        }

        [Fact]
        public async Task AllRoutes_StopsCarryTheVehicleFields()
        {
            var (token, routeId, zoneId, zoneName, registration) = await ArrangeAsync();

            var response = await _client.SendAsync(Request(HttpMethod.Get, "/api/routes/all", token));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var route = doc.RootElement.EnumerateArray().Single(r => r.GetProperty("id").GetInt32() == routeId);
            AssertVehicle(route.GetProperty("stops")[0], registration, zoneName, zoneId);
        }

        [Fact]
        public async Task Suggestions_StopsCarryTheVehicleFields()
        {
            var (token, routeId, zoneId, zoneName, registration) = await ArrangeAsync();

            var response = await _client.SendAsync(Request(HttpMethod.Get, $"/api/routes/suggestions?zoneId={zoneId}", token));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var route = doc.RootElement.EnumerateArray().Single(r => r.GetProperty("id").GetInt32() == routeId);
            AssertVehicle(route.GetProperty("stops")[0], registration, zoneName, zoneId);
        }

        [Fact]
        public async Task ConfirmRoute_ResponseStopsCarryTheVehicleFields()
        {
            var (token, routeId, zoneId, zoneName, registration) = await ArrangeAsync();

            var response = await _client.SendAsync(Request(HttpMethod.Post, $"/api/routes/{routeId}/confirm", token));

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            AssertVehicle(doc.RootElement.GetProperty("stops")[0], registration, zoneName, zoneId);
        }

        [Fact]
        public async Task RouteStopEndpoints_ReturnTheSameVehicleFields()
        {
            var (token, _, zoneId, zoneName, registration) = await ArrangeAsync();
            var (swapperId, _) = await _factory.NewUserAsync(_client, UserRole.BatterySwapper);
            var (_, stopId, reg2) = _factory.SeedRoute(swapperId, zoneId);

            var response = await _client.SendAsync(Request(HttpMethod.Put, $"/api/routestops/{stopId}/instructions", token, new { notes = "test-only" }));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            AssertVehicle(doc.RootElement, reg2, zoneName, zoneId);
        }
    }
}
