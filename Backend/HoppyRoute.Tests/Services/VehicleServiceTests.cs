using FluentAssertions;
using HoppyRoute.Application.Services;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Infrastructure.Data;
using HoppyRoute.Tests.TestSupport;

namespace HoppyRoute.Tests.Services
{
    public class VehicleServiceTests : IDisposable
    {
        private readonly HoppyDbContext _db = TestDb.Create();
        private readonly VehicleService _service;
        private readonly Zone _zoneA;
        private readonly Zone _zoneB;
        private int _seq;

        public VehicleServiceTests()
        {
            _service = new VehicleService(_db);
            var region = new Region { Name = "Mechelen", Country = "België", CountryCode = "BE" };
            _db.Regions.Add(region);
            _db.SaveChanges();
            _zoneA = new Zone { Name = "Mechelen Centrum", CountryCode = "BE", RegionId = region.Id, GeoJsonBoundary = "{}" };
            _zoneB = new Zone { Name = "Mechelen Station", CountryCode = "BE", RegionId = region.Id, GeoJsonBoundary = "{}" };
            _db.Zones.AddRange(_zoneA, _zoneB);
            _db.SaveChanges();
        }

        public void Dispose() => _db.Dispose();

        private Vehicle Add(int battery, int? zoneId, bool available = true, bool needsReplacement = false)
        {
            var v = new Vehicle
            {
                ExternalId = $"HOP-{++_seq:D4}",
                RegistrationNumber = $"R{_seq:D4}",
                VehicleType = "E-Scooter",
                BatteryLevel = battery,
                ZoneId = zoneId,
                IsAvailable = available,
                NeedsBatteryReplacement = needsReplacement,
                Latitude = 51.0275,
                Longitude = 4.4775,
                LastUpdated = DateTime.UtcNow
            };
            _db.Vehicles.Add(v);
            _db.SaveChanges();
            return v;
        }

        // ---------- low-battery detection ----------

        [Fact]
        public async Task GetLowBattery_DefaultThreshold25_IsInclusive_AndSortedLowestFirst()
        {
            var at25 = Add(25, _zoneA.Id);
            Add(26, _zoneA.Id);
            var at5 = Add(5, _zoneA.Id);
            var at0 = Add(0, _zoneA.Id);

            var result = await _service.GetLowBatteryVehiclesAsync(_zoneA.Id);

            result.Select(v => v.Id).Should().Equal(at0.Id, at5.Id, at25.Id);
            result.Should().OnlyContain(v => v.ZoneName == "Mechelen Centrum");
        }

        [Fact]
        public async Task GetLowBattery_CustomThreshold_IsRespected()
        {
            Add(15, _zoneA.Id);
            var at10 = Add(10, _zoneA.Id);

            var result = await _service.GetLowBatteryVehiclesAsync(_zoneA.Id, batteryThreshold: 10);

            result.Select(v => v.Id).Should().Equal(at10.Id);
        }

        [Fact]
        public async Task GetLowBattery_ExcludesUnavailableVehicles_AndOtherZones()
        {
            Add(5, _zoneA.Id, available: false);
            Add(5, _zoneB.Id);
            Add(5, zoneId: null);
            var mine = Add(5, _zoneA.Id);

            var result = await _service.GetLowBatteryVehiclesAsync(_zoneA.Id);

            result.Select(v => v.Id).Should().Equal(mine.Id);
        }

        [Fact]
        public async Task GetAllLowBattery_SpansZones_AndIncludesVehiclesWithNoZone()
        {
            var a = Add(10, _zoneA.Id);
            var b = Add(10, _zoneB.Id);
            var outOfZone = Add(3, zoneId: null);
            Add(80, _zoneA.Id);
            Add(10, _zoneB.Id, available: false);

            var result = await _service.GetAllLowBatteryVehiclesAsync();

            result.Select(v => v.Id).Should().BeEquivalentTo(new[] { outOfZone.Id, a.Id, b.Id });
            result.First().Id.Should().Be(outOfZone.Id, "lowest battery first");
            result.Single(v => v.Id == outOfZone.Id).ZoneName.Should().BeNull();
        }

        [Theory]
        [InlineData(25, true)]
        [InlineData(26, false)]
        [InlineData(0, true)]
        [InlineData(100, false)]
        public async Task UpdateBattery_SetsNeedsReplacementAtOrBelow25(int level, bool expected)
        {
            var v = Add(50, _zoneA.Id);

            var dto = await _service.UpdateVehicleBatteryAsync(v.Id, level);

            dto!.BatteryLevel.Should().Be(level);
            dto.NeedsBatteryReplacement.Should().Be(expected);
            (await _db.Vehicles.FindAsync(v.Id))!.NeedsBatteryReplacement.Should().Be(expected);
        }

        [Fact]
        public async Task UpdateBattery_UnknownVehicle_ReturnsNull()
        {
            (await _service.UpdateVehicleBatteryAsync(9999, 10)).Should().BeNull();
        }

        [Fact]
        public async Task NeedingReplacement_UsesTheFlag_NotTheRawLevel()
        {
            var flagged = Add(30, _zoneA.Id, needsReplacement: true);
            Add(5, _zoneA.Id, needsReplacement: false);
            Add(30, _zoneA.Id, available: false, needsReplacement: true);

            var result = await _service.GetVehiclesNeedingBatteryReplacementAsync(_zoneA.Id);

            result.Select(v => v.Id).Should().Equal(flagged.Id);
        }

        // ---------- zone membership (ZoneId == null is what the seed data calls "out of zone") ----------

        [Fact]
        public async Task GetVehiclesByZone_ExcludesVehiclesWithNoZone_AndOtherZones()
        {
            var inA = Add(50, _zoneA.Id);
            Add(50, _zoneB.Id);
            Add(50, zoneId: null);

            var result = await _service.GetVehiclesByZoneAsync(_zoneA.Id);

            result.Select(v => v.Id).Should().Equal(inA.Id);
        }

        [Fact]
        public async Task GetVehicleById_NoZone_ReturnsVehicleWithNullZone()
        {
            var v = Add(40, zoneId: null);

            var dto = await _service.GetVehicleByIdAsync(v.Id);

            dto!.ZoneId.Should().BeNull();
            dto.ZoneName.Should().BeNull();
            (await _service.GetVehicleByIdAsync(9999)).Should().BeNull();
        }

        // ---------- parking-zone bookkeeping ----------

        [Fact]
        public async Task MoveToParkingZone_UpdatesBothCounters()
        {
            var from = new ParkingZone { Name = "P1", ZoneId = _zoneA.Id, CurrentVehicleCount = 2 };
            var to = new ParkingZone { Name = "P2", ZoneId = _zoneA.Id, CurrentVehicleCount = 0 };
            _db.ParkingZones.AddRange(from, to);
            await _db.SaveChangesAsync();
            var v = Add(50, _zoneA.Id);
            v.CurrentParkingZoneId = from.Id;
            await _db.SaveChangesAsync();

            (await _service.MoveVehicleToParkingZoneAsync(v.Id, to.Id)).Should().BeTrue();

            (await _db.ParkingZones.FindAsync(from.Id))!.CurrentVehicleCount.Should().Be(1);
            (await _db.ParkingZones.FindAsync(to.Id))!.CurrentVehicleCount.Should().Be(1);
            (await _db.Vehicles.FindAsync(v.Id))!.CurrentParkingZoneId.Should().Be(to.Id);
        }
    }
}
