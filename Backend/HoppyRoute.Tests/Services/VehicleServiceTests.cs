using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using HoppyRoute.Application.Services;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Infrastructure.Data;
using Xunit;

namespace HoppyRoute.Tests.Services
{
    public class VehicleServiceTests : IDisposable
    {
        private readonly HoppyDbContext _context;
        private readonly VehicleService _vehicleService;

        public VehicleServiceTests()
        {
            var options = new DbContextOptionsBuilder<HoppyDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new HoppyDbContext(options);
            _vehicleService = new VehicleService(_context);
        }

        [Fact]
        public async Task GetLowBatteryVehiclesAsync_ReturnsOnlyLowBatteryVehicles()
        {
            // Arrange
            var zone = new Zone
            {
                Name = "Test Zone",
                CountryCode = "BE",
                GeoJsonBoundary = "{}"
            };
            _context.Zones.Add(zone);
            await _context.SaveChangesAsync();

            var vehicles = new[]
            {
                new Vehicle
                {
                    ExternalId = "VH001",
                    ZoneId = zone.Id,
                    BatteryLevel = 10, // Low battery
                    Latitude = 51.0,
                    Longitude = 4.0,
                    LastUpdated = DateTime.UtcNow
                },
                new Vehicle
                {
                    ExternalId = "VH002",
                    ZoneId = zone.Id,
                    BatteryLevel = 80, // High battery
                    Latitude = 51.0,
                    Longitude = 4.0,
                    LastUpdated = DateTime.UtcNow
                },
                new Vehicle
                {
                    ExternalId = "VH003",
                    ZoneId = zone.Id,
                    BatteryLevel = 20, // Low battery
                    Latitude = 51.0,
                    Longitude = 4.0,
                    LastUpdated = DateTime.UtcNow
                }
            };

            _context.Vehicles.AddRange(vehicles);
            await _context.SaveChangesAsync();

            // Act
            var result = await _vehicleService.GetLowBatteryVehiclesAsync(zone.Id, 25);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(v => v.BatteryLevel <= 25);
            result.Should().BeInAscendingOrder(v => v.BatteryLevel);
        }

        [Fact]
        public async Task GetVehiclesByZoneAsync_ReturnsOnlyVehiclesInZone()
        {
            // Arrange
            var zone1 = new Zone
            {
                Name = "Zone 1",
                CountryCode = "BE",
                GeoJsonBoundary = "{}"
            };
            var zone2 = new Zone
            {
                Name = "Zone 2",
                CountryCode = "BE",
                GeoJsonBoundary = "{}"
            };
            _context.Zones.AddRange(zone1, zone2);
            await _context.SaveChangesAsync();

            var vehicles = new[]
            {
                new Vehicle
                {
                    ExternalId = "VH001",
                    ZoneId = zone1.Id,
                    BatteryLevel = 50,
                    Latitude = 51.0,
                    Longitude = 4.0,
                    LastUpdated = DateTime.UtcNow
                },
                new Vehicle
                {
                    ExternalId = "VH002",
                    ZoneId = zone2.Id,
                    BatteryLevel = 60,
                    Latitude = 51.0,
                    Longitude = 4.0,
                    LastUpdated = DateTime.UtcNow
                },
                new Vehicle
                {
                    ExternalId = "VH003",
                    ZoneId = zone1.Id,
                    BatteryLevel = 70,
                    Latitude = 51.0,
                    Longitude = 4.0,
                    LastUpdated = DateTime.UtcNow
                }
            };

            _context.Vehicles.AddRange(vehicles);
            await _context.SaveChangesAsync();

            // Act
            var result = await _vehicleService.GetVehiclesByZoneAsync(zone1.Id);

            // Assert
            result.Should().HaveCount(2);
            result.Should().OnlyContain(v => v.ZoneId == zone1.Id);
            result.Should().BeInAscendingOrder(v => v.ExternalId);
        }

        [Fact]
        public async Task GetVehiclesByZoneAsync_IncludesOutOfZoneVehicles()
        {
            // Arrange
            var zone = new Zone
            {
                Name = "Test Zone",
                CountryCode = "BE",
                GeoJsonBoundary = "{}"
            };
            _context.Zones.Add(zone);
            await _context.SaveChangesAsync();

            var vehicles = new[]
            {
                new Vehicle
                {
                    ExternalId = "VH001",
                    ZoneId = zone.Id,
                    BatteryLevel = 50,
                    Latitude = 51.0,
                    Longitude = 4.0,
                    LastUpdated = DateTime.UtcNow
                },
                new Vehicle
                {
                    ExternalId = "VH002",
                    ZoneId = null, // Out of zone
                    BatteryLevel = 20,
                    Latitude = 51.0,
                    Longitude = 4.0,
                    LastUpdated = DateTime.UtcNow
                }
            };

            _context.Vehicles.AddRange(vehicles);
            await _context.SaveChangesAsync();

            // Act - This should only return vehicles IN the zone
            var result = await _vehicleService.GetVehiclesByZoneAsync(zone.Id);

            // Assert
            result.Should().HaveCount(1);
            result.Should().OnlyContain(v => v.ZoneId == zone.Id);
        }

        [Fact]
        public async Task GetVehicleByIdAsync_ExistingVehicle_ReturnsVehicle()
        {
            // Arrange
            var zone = new Zone
            {
                Name = "Test Zone",
                CountryCode = "BE",
                GeoJsonBoundary = "{}"
            };
            _context.Zones.Add(zone);
            await _context.SaveChangesAsync();

            var vehicle = new Vehicle
            {
                ExternalId = "VH001",
                ZoneId = zone.Id,
                BatteryLevel = 50,
                Latitude = 51.0,
                Longitude = 4.0,
                LastUpdated = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            // Act
            var result = await _vehicleService.GetVehicleByIdAsync(vehicle.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(vehicle.Id);
            result.ExternalId.Should().Be("VH001");
            result.ZoneId.Should().Be(zone.Id);
            result.ZoneName.Should().Be("Test Zone");
        }

        [Fact]
        public async Task GetVehicleByIdAsync_NonExistingVehicle_ReturnsNull()
        {
            // Act
            var result = await _vehicleService.GetVehicleByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetVehicleByIdAsync_VehicleWithoutZone_ReturnsVehicleWithNullZone()
        {
            // Arrange
            var vehicle = new Vehicle
            {
                ExternalId = "VH001",
                ZoneId = null, // Out of zone
                BatteryLevel = 50,
                Latitude = 51.0,
                Longitude = 4.0,
                LastUpdated = DateTime.UtcNow
            };

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();

            // Act
            var result = await _vehicleService.GetVehicleByIdAsync(vehicle.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(vehicle.Id);
            result.ExternalId.Should().Be("VH001");
            result.ZoneId.Should().BeNull();
            result.ZoneName.Should().BeNull();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
