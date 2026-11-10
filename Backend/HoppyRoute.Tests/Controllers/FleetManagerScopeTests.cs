using System.Net;
using System.Text.Json;
using FluentAssertions;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;
using HoppyRoute.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using static HoppyRoute.Tests.TestSupport.ApiTestHelpers;

namespace HoppyRoute.Tests.Controllers
{
    /// <summary>
    /// A FleetManager only manages routes and stops in their own zone, or in the zones of their own region;
    /// the Admin is global; a FleetManager with no assignment manages nothing. Out of scope is 403, an
    /// unknown id is still 404, and the lists only contain the manager's own routes.
    /// </summary>
    public class FleetManagerScopeTests : IClassFixture<HoppyApiFactory>
    {
        private readonly HoppyApiFactory _factory;
        private readonly HttpClient _client;
        private readonly (int RegionA, int ZoneA1, int ZoneA2, int RegionB, int ZoneB) _geo;

        public FleetManagerScopeTests(HoppyApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateSeededClient();
            _geo = factory.Geography();
        }

        private record Arranged(int SwapperId, (int RouteId, int StopId, string Reg) A1, (int RouteId, int StopId, string Reg) A2, (int RouteId, int StopId, string Reg) B);

        /// <summary>One route (with a stop) in each of the zones A1, A2 and B, all assigned to one swapper.</summary>
        private async Task<Arranged> ArrangeAsync()
        {
            var (swapperId, _) = await _factory.NewUserAsync(_client, UserRole.BatterySwapper);
            return new Arranged(swapperId,
                _factory.SeedRoute(swapperId, _geo.ZoneA1),
                _factory.SeedRoute(swapperId, _geo.ZoneA2),
                _factory.SeedRoute(swapperId, _geo.ZoneB));
        }

        private Task<(int Id, string Token)> ZoneManagerAsync(int zoneId) => _factory.NewUserAsync(_client, UserRole.FleetManager, zoneId: zoneId);
        private Task<(int Id, string Token)> RegionManagerAsync(int regionId) => _factory.NewUserAsync(_client, UserRole.FleetManager, regionId: regionId);

        private Task<HttpResponseMessage> SendAsync(string method, string url, string token, object? body = null) =>
            _client.SendAsync(Request(new HttpMethod(method), url, token, body ?? (method is "POST" or "PUT" ? new { } : null)));

        private RouteStatus RouteStatusOf(int routeId) => _factory.Read(db => db.Routes.AsNoTracking().Single(r => r.Id == routeId).Status);
        private RouteApprovalStatus ApprovalOf(int routeId) => _factory.Read(db => db.Routes.AsNoTracking().Single(r => r.Id == routeId).ApprovalStatus);
        private HoppyRoute.Domain.Entities.RouteStop Stop(int stopId) => _factory.Read(db => db.RouteStops.AsNoTracking().Single(s => s.Id == stopId));

        private static async Task<List<int>> RouteIdsAsync(HttpResponseMessage response)
        {
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.EnumerateArray().Select(r => r.GetProperty("id").GetInt32()).ToList();
        }

        // ================= approve / reject =================

        [Theory]
        [InlineData("approve", RouteApprovalStatus.Approved)]
        [InlineData("reject", RouteApprovalStatus.Rejected)]
        public async Task ApproveAndReject_InOwnZone_Work_OutsideItAre403(string action, RouteApprovalStatus expected)
        {
            var a = await ArrangeAsync();
            var (_, token) = await ZoneManagerAsync(_geo.ZoneA1);

            (await SendAsync("POST", $"/api/routes/{a.A2.RouteId}/{action}", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden); // same region, other zone
            (await SendAsync("POST", $"/api/routes/{a.B.RouteId}/{action}", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);  // other region
            ApprovalOf(a.A2.RouteId).Should().Be(RouteApprovalStatus.PendingApproval);
            ApprovalOf(a.B.RouteId).Should().Be(RouteApprovalStatus.PendingApproval);

            (await SendAsync("POST", $"/api/routes/{a.A1.RouteId}/{action}", token)).StatusCode.Should().Be(HttpStatusCode.OK);
            ApprovalOf(a.A1.RouteId).Should().Be(expected);
        }

        // ================= confirm =================

        [Fact]
        public async Task Confirm_InOwnZone_Works_OutsideItIs403()
        {
            var a = await ArrangeAsync();
            var (_, token) = await ZoneManagerAsync(_geo.ZoneA1);

            (await SendAsync("POST", $"/api/routes/{a.B.RouteId}/confirm", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            RouteStatusOf(a.B.RouteId).Should().Be(RouteStatus.Suggested);

            (await SendAsync("POST", $"/api/routes/{a.A1.RouteId}/confirm", token)).StatusCode.Should().Be(HttpStatusCode.OK);
            RouteStatusOf(a.A1.RouteId).Should().Be(RouteStatus.Confirmed);
        }

        // ================= complete / status / instructions =================

        [Fact]
        public async Task CompleteStop_InOwnZone_Works_OutsideItIs403()
        {
            var a = await ArrangeAsync();
            var (_, token) = await ZoneManagerAsync(_geo.ZoneA1);

            (await SendAsync("POST", $"/api/routestops/{a.B.StopId}/complete", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Stop(a.B.StopId).Status.Should().Be(RouteStopStatus.Pending);

            (await SendAsync("POST", $"/api/routestops/{a.A1.StopId}/complete", token)).StatusCode.Should().Be(HttpStatusCode.OK);
            Stop(a.A1.StopId).Status.Should().Be(RouteStopStatus.Completed);
        }

        [Fact]
        public async Task SetStatus_InOwnZone_Works_OutsideItIs403()
        {
            var a = await ArrangeAsync();
            var (_, token) = await ZoneManagerAsync(_geo.ZoneA1);

            (await SendAsync("PUT", $"/api/routestops/{a.A2.StopId}/status", token, new { status = 4, reason = "test-only" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Stop(a.A2.StopId).Status.Should().Be(RouteStopStatus.Pending);
            Stop(a.A2.StopId).SkipReason.Should().BeNull();

            (await SendAsync("PUT", $"/api/routestops/{a.A1.StopId}/status", token, new { status = 4, reason = "test-only" })).StatusCode.Should().Be(HttpStatusCode.OK);
            Stop(a.A1.StopId).Status.Should().Be(RouteStopStatus.Skipped);
        }

        [Fact]
        public async Task SetInstructions_InOwnZone_Works_OutsideItIs403()
        {
            var a = await ArrangeAsync();
            var (_, token) = await ZoneManagerAsync(_geo.ZoneA1);

            (await SendAsync("PUT", $"/api/routestops/{a.B.StopId}/instructions", token, new { notes = "test-only" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            Stop(a.B.StopId).Notes.Should().BeNull();

            (await SendAsync("PUT", $"/api/routestops/{a.A1.StopId}/instructions", token, new { notes = "test-only" })).StatusCode.Should().Be(HttpStatusCode.OK);
            Stop(a.A1.StopId).Notes.Should().Be("test-only");
        }

        // ================= suggest =================

        [Fact]
        public async Task Suggest_OutsideOwnZone_Returns403_AndCreatesNothing()
        {
            var a = await ArrangeAsync();
            var (_, token) = await ZoneManagerAsync(_geo.ZoneA1);
            var before = _factory.Read(db => db.Routes.Count());

            var response = await SendAsync("POST", "/api/routes/suggest", token, new
            {
                assignedSwapperId = a.SwapperId, zoneId = _geo.ZoneB, targetDurationMinutes = 120, name = "test-only"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            _factory.Read(db => db.Routes.Count()).Should().Be(before);
        }

        [Fact]
        public async Task Suggest_InOwnZone_PassesTheScopeCheck()
        {
            // No available vehicle in the zone, so generation answers 400 at once (no 30 s solver run):
            // 400 rather than 403 shows the scope check let the request through.
            var a = await ArrangeAsync();
            _factory.Read(db =>
            {
                db.Vehicles.Where(v => v.ZoneId == _geo.ZoneA1).ToList().ForEach(v => v.IsAvailable = false);
                db.SaveChanges();
                return 0;
            });
            var (_, token) = await ZoneManagerAsync(_geo.ZoneA1);

            var response = await SendAsync("POST", "/api/routes/suggest", token, new
            {
                assignedSwapperId = a.SwapperId, zoneId = _geo.ZoneA1, targetDurationMinutes = 120, name = "test-only"
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ================= lists =================

        [Fact]
        public async Task AllRoutesAndPendingApproval_ListOnlyTheManagersOwnZone()
        {
            var a = await ArrangeAsync();
            var (_, token) = await ZoneManagerAsync(_geo.ZoneA1);

            foreach (var url in new[] { "/api/routes/all", "/api/routes/pending-approval" })
            {
                var ids = await RouteIdsAsync(await SendAsync("GET", url, token));
                ids.Should().Contain(a.A1.RouteId);
                ids.Should().NotContain(new[] { a.A2.RouteId, a.B.RouteId }, url);
            }
        }

        [Fact]
        public async Task RegionManager_SeesAndManagesEveryZoneOfTheirRegion_ButNotOtherRegions()
        {
            var a = await ArrangeAsync();
            var (_, token) = await RegionManagerAsync(_geo.RegionA);

            var ids = await RouteIdsAsync(await SendAsync("GET", "/api/routes/all", token));
            ids.Should().Contain(new[] { a.A1.RouteId, a.A2.RouteId });
            ids.Should().NotContain(a.B.RouteId);

            (await SendAsync("POST", $"/api/routes/{a.A2.RouteId}/approve", token)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await SendAsync("POST", $"/api/routes/{a.B.RouteId}/approve", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task PendingApproval_WithAZoneFilterOutsideScope_IsEmpty_NotA403()
        {
            var a = await ArrangeAsync();
            var (_, token) = await ZoneManagerAsync(_geo.ZoneA1);

            var ids = await RouteIdsAsync(await SendAsync("GET", $"/api/routes/pending-approval?zoneId={_geo.ZoneB}", token));
            ids.Should().BeEmpty();
        }

        // ================= no assignment, zone beats region, admin =================

        [Fact]
        public async Task FleetManagerWithoutAssignment_ManagesNothing_AndListsNothing()
        {
            var a = await ArrangeAsync();
            var (_, token) = await _factory.NewUserAsync(_client, UserRole.FleetManager);

            (await RouteIdsAsync(await SendAsync("GET", "/api/routes/all", token))).Should().BeEmpty();
            (await RouteIdsAsync(await SendAsync("GET", "/api/routes/pending-approval", token))).Should().BeEmpty();
            (await SendAsync("POST", $"/api/routes/{a.A1.RouteId}/approve", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await SendAsync("POST", $"/api/routestops/{a.A1.StopId}/complete", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task WhenBothAreSet_TheZoneWinsOverTheRegion()
        {
            var a = await ArrangeAsync();
            // Zone A1 plus region B: scope is zone A1 only, not region B.
            var (_, token) = await _factory.NewUserAsync(_client, UserRole.FleetManager, zoneId: _geo.ZoneA1, regionId: _geo.RegionB);

            (await SendAsync("POST", $"/api/routes/{a.A1.RouteId}/approve", token)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await SendAsync("POST", $"/api/routes/{a.B.RouteId}/approve", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Admin_IsGlobal()
        {
            var a = await ArrangeAsync();
            var (_, token) = await _factory.NewUserAsync(_client, UserRole.Admin);

            var ids = await RouteIdsAsync(await SendAsync("GET", "/api/routes/all", token));
            ids.Should().Contain(new[] { a.A1.RouteId, a.A2.RouteId, a.B.RouteId });
            (await SendAsync("POST", $"/api/routes/{a.B.RouteId}/approve", token)).StatusCode.Should().Be(HttpStatusCode.OK);
            (await SendAsync("POST", $"/api/routestops/{a.A2.StopId}/complete", token)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        // ================= the scope is read live, unknown ids stay 404 =================

        [Fact]
        public async Task ChangingTheAssignment_TakesEffectOnTheSameToken()
        {
            var a = await ArrangeAsync();
            var (managerId, token) = await ZoneManagerAsync(_geo.ZoneA1);
            (await SendAsync("POST", $"/api/routes/{a.A1.RouteId}/confirm", token)).StatusCode.Should().Be(HttpStatusCode.OK);

            // An admin moves the manager to the other region; the old token must not keep the old scope.
            _factory.Read(db =>
            {
                var u = db.Users.Single(x => x.Id == managerId);
                u.AssignedZoneId = _geo.ZoneB;
                db.SaveChanges();
                return 0;
            });

            var again = await ArrangeAsync();
            (await SendAsync("POST", $"/api/routes/{again.A1.RouteId}/confirm", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await SendAsync("POST", $"/api/routes/{again.B.RouteId}/confirm", token)).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task DeactivatedFleetManager_WithAStillValidToken_ManagesNothing()
        {
            var a = await ArrangeAsync();
            var (managerId, token) = await ZoneManagerAsync(_geo.ZoneA1);
            _factory.Read(db =>
            {
                db.Users.Single(x => x.Id == managerId).IsActive = false;
                db.SaveChanges();
                return 0;
            });

            (await SendAsync("POST", $"/api/routes/{a.A1.RouteId}/approve", token)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await RouteIdsAsync(await SendAsync("GET", "/api/routes/all", token))).Should().BeEmpty();
        }

        [Fact]
        public async Task UnknownIds_AreStill404_ForAFleetManager()
        {
            var (_, token) = await ZoneManagerAsync(_geo.ZoneA1);

            (await SendAsync("POST", "/api/routes/987654/approve", token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await SendAsync("POST", "/api/routes/987654/confirm", token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await SendAsync("POST", "/api/routestops/987654/complete", token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await SendAsync("PUT", "/api/routestops/987654/status", token, new { status = 4 })).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await SendAsync("PUT", "/api/routestops/987654/instructions", token, new { notes = "x" })).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
