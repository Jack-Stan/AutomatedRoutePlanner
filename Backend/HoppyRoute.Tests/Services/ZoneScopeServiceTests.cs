using FluentAssertions;
using HoppyRoute.Application.Services;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Tests.TestSupport;

namespace HoppyRoute.Tests.Services
{
    public class ZoneScopeServiceTests : IDisposable
    {
        private readonly HoppyRoute.Infrastructure.Data.HoppyDbContext _db = TestDb.Create();
        private readonly ZoneScopeService _service;
        private readonly int _regionA, _zoneA1, _zoneA2, _zoneB;
        private int _seq;

        public ZoneScopeServiceTests()
        {
            _service = new ZoneScopeService(_db);
            var a = new Region { Name = "A", Country = "België", CountryCode = "BE" };
            var b = new Region { Name = "B", Country = "België", CountryCode = "BE" };
            _db.Regions.AddRange(a, b);
            _db.SaveChanges();
            var za1 = new Zone { Name = "A1", CountryCode = "BE", RegionId = a.Id, GeoJsonBoundary = "{}" };
            var za2 = new Zone { Name = "A2", CountryCode = "BE", RegionId = a.Id, GeoJsonBoundary = "{}" };
            var zb = new Zone { Name = "B1", CountryCode = "BE", RegionId = b.Id, GeoJsonBoundary = "{}" };
            _db.Zones.AddRange(za1, za2, zb);
            _db.SaveChanges();
            _regionA = a.Id; _zoneA1 = za1.Id; _zoneA2 = za2.Id; _zoneB = zb.Id;
        }

        public void Dispose() => _db.Dispose();

        private int AddUser(UserRole role, int? zoneId = null, int? regionId = null, bool active = true)
        {
            var n = ++_seq;
            var u = new User
            {
                Username = $"u{n}", Email = $"u{n}@test.invalid", PasswordHash = "x", Role = role,
                AssignedZoneId = zoneId, AssignedRegionId = regionId, IsActive = active
            };
            _db.Users.Add(u);
            _db.SaveChanges();
            return u.Id;
        }

        [Fact]
        public async Task ZoneAssignment_IsThatZoneOnly()
        {
            var id = AddUser(UserRole.FleetManager, zoneId: _zoneA1);
            (await _service.GetAllowedZoneIdsAsync(id)).Should().BeEquivalentTo(new[] { _zoneA1 });
        }

        [Fact]
        public async Task RegionAssignment_IsEveryZoneOfTheRegion()
        {
            var id = AddUser(UserRole.FleetManager, regionId: _regionA);
            (await _service.GetAllowedZoneIdsAsync(id)).Should().BeEquivalentTo(new[] { _zoneA1, _zoneA2 });
        }

        [Fact]
        public async Task ZoneBeatsRegion_WhenBothAreSet()
        {
            var id = AddUser(UserRole.FleetManager, zoneId: _zoneB, regionId: _regionA);
            (await _service.GetAllowedZoneIdsAsync(id)).Should().BeEquivalentTo(new[] { _zoneB });
        }

        [Fact]
        public async Task NoAssignment_IsNoZones()
        {
            var id = AddUser(UserRole.FleetManager);
            (await _service.GetAllowedZoneIdsAsync(id)).Should().BeEmpty();
        }

        [Theory]
        [InlineData(UserRole.Admin)]
        [InlineData(UserRole.BatterySwapper)]
        public async Task NonFleetManagers_GetNoZones_EvenWhenAssigned(UserRole role)
        {
            var id = AddUser(role, zoneId: _zoneA1);
            (await _service.GetAllowedZoneIdsAsync(id)).Should().BeEmpty();
        }

        [Fact]
        public async Task InactiveAndUnknownUsers_GetNoZones()
        {
            var inactive = AddUser(UserRole.FleetManager, zoneId: _zoneA1, active: false);
            (await _service.GetAllowedZoneIdsAsync(inactive)).Should().BeEmpty();
            (await _service.GetAllowedZoneIdsAsync(987654)).Should().BeEmpty();
        }
    }
}
