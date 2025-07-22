using Microsoft.EntityFrameworkCore;
using HoppyRoute.Domain.Entities;
using BCrypt.Net;

namespace HoppyRoute.Infrastructure.Data
{
    public class HoppyDbContext : DbContext
    {
        public HoppyDbContext(DbContextOptions<HoppyDbContext> options) : base(options)
        {
        }

        public DbSet<Swapper> Swappers { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Region> Regions { get; set; }
        public DbSet<ParkingZone> ParkingZones { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Region configuration
            modelBuilder.Entity<Region>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Country).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(3);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).IsRequired();
            });

            // Zone configuration
            modelBuilder.Entity<Zone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(3);
                entity.Property(e => e.GeoJsonBoundary).IsRequired();

                entity.HasOne(z => z.Region)
                      .WithMany(r => r.Zones)
                      .HasForeignKey(z => z.RegionId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // ParkingZone configuration
            modelBuilder.Entity<ParkingZone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Latitude).IsRequired();
                entity.Property(e => e.Longitude).IsRequired();
                entity.Property(e => e.RadiusMeters).IsRequired().HasDefaultValue(50);
                entity.Property(e => e.MaxCapacity).IsRequired().HasDefaultValue(10);
                entity.Property(e => e.CurrentVehicleCount).IsRequired().HasDefaultValue(0);
                entity.Property(e => e.IsActive).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(pz => pz.Zone)
                      .WithMany(z => z.ParkingZones)
                      .HasForeignKey(pz => pz.ZoneId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Vehicle configuration
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.RegistrationNumber).IsRequired().HasMaxLength(20);
                entity.Property(e => e.VehicleType).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Latitude).IsRequired();
                entity.Property(e => e.Longitude).IsRequired();
                entity.Property(e => e.BatteryLevel).IsRequired();
                entity.Property(e => e.NeedsBatteryReplacement).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.IsAvailable).IsRequired().HasDefaultValue(true);
                entity.Property(e => e.LastUpdated).IsRequired();

                entity.HasOne(v => v.Zone)
                      .WithMany(z => z.Vehicles)
                      .HasForeignKey(v => v.ZoneId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(v => v.CurrentParkingZone)
                      .WithMany(pz => pz.Vehicles)
                      .HasForeignKey(v => v.CurrentParkingZoneId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Swapper configuration
            modelBuilder.Entity<Swapper>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Phone).HasMaxLength(20);
            });

            // Route configuration
            modelBuilder.Entity<Route>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.TargetDurationMinutes).IsRequired();
                entity.Property(e => e.EstimatedDistanceKm).HasPrecision(8, 2);
                entity.Property(e => e.TotalVehicleCount).IsRequired().HasDefaultValue(0);
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(r => r.AssignedSwapper)
                      .WithMany(u => u.AssignedRoutes)
                      .HasForeignKey(r => r.AssignedSwapperId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.CreatedByUser)
                      .WithMany(u => u.CreatedRoutes)
                      .HasForeignKey(r => r.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Zone)
                      .WithMany(z => z.Routes)
                      .HasForeignKey(r => r.ZoneId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(r => r.Region)
                      .WithMany(rg => rg.Routes)
                      .HasForeignKey(r => r.RegionId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // RouteStop configuration
            modelBuilder.Entity<RouteStop>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Order).IsRequired();
                entity.Property(e => e.StopType).IsRequired();
                entity.Property(e => e.EstimatedArrivalOffset).IsRequired();
                entity.Property(e => e.EstimatedDurationAtStop).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.Notes).HasMaxLength(500);
                entity.Property(e => e.Latitude).HasPrecision(10, 7);
                entity.Property(e => e.Longitude).HasPrecision(10, 7);

                entity.HasOne(rs => rs.Route)
                      .WithMany(r => r.Stops)
                      .HasForeignKey(rs => rs.RouteId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rs => rs.Vehicle)
                      .WithMany(v => v.RouteStops)
                      .HasForeignKey(rs => rs.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(rs => rs.PickupParkingZone)
                      .WithMany(pz => pz.PickupStops)
                      .HasForeignKey(rs => rs.PickupParkingZoneId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(rs => rs.DropoffParkingZone)
                      .WithMany(pz => pz.DropoffStops)
                      .HasForeignKey(rs => rs.DropoffParkingZoneId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).IsRequired();
                entity.Property(e => e.FirstName).HasMaxLength(100);
                entity.Property(e => e.LastName).HasMaxLength(100);
                entity.Property(e => e.IsActive).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.IsTemporaryPassword).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.HasCompletedFirstLogin).IsRequired().HasDefaultValue(false);
                entity.Property(e => e.TemporaryPasswordExpiresAt).IsRequired(false);

                // Unique constraints
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();

                // Self-referencing relationship for hierarchy
                entity.HasOne(u => u.CreatedByUser)
                      .WithMany(u => u.CreatedUsers)
                      .HasForeignKey(u => u.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Zone relationship
                entity.HasOne(u => u.AssignedZone)
                      .WithMany()
                      .HasForeignKey(u => u.AssignedZoneId)
                      .OnDelete(DeleteBehavior.SetNull);

                // Region relationship
                entity.HasOne(u => u.AssignedRegion)
                      .WithMany(r => r.AssignedUsers)
                      .HasForeignKey(u => u.AssignedRegionId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Regions first
            modelBuilder.Entity<Region>().HasData(
                new Region
                {
                    Id = 1,
                    Name = "België",
                    Country = "Belgium",
                    CountryCode = "BE",
                    Description = "Belgische regio's waar Hoppy actief is",
                    Latitude = 50.8503,
                    Longitude = 4.3517,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Region
                {
                    Id = 2,
                    Name = "Spanje",
                    Country = "Spain", 
                    CountryCode = "ES",
                    Description = "Spaanse regio's inclusief eilanden waar Hoppy actief is",
                    Latitude = 40.4168,
                    Longitude = -3.7038,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Region
                {
                    Id = 3,
                    Name = "Griekenland",
                    Country = "Greece",
                    CountryCode = "GR", 
                    Description = "Griekse eilanden waar Hoppy actief is",
                    Latitude = 37.9838,
                    Longitude = 23.7275,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new Region
                {
                    Id = 4,
                    Name = "Gibraltar",
                    Country = "Gibraltar",
                    CountryCode = "GI",
                    Description = "Gibraltar waar Hoppy actief is", 
                    Latitude = 36.1408,
                    Longitude = -5.3536,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed Zones with real Hoppy locations and Region linkage
            modelBuilder.Entity<Zone>().HasData(
                // Belgium - RegionId = 1
                new Zone
                {
                    Id = 1,
                    Name = "Cadzand",
                    CountryCode = "BE",
                    RegionId = 1,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[3.37,51.36],[3.40,51.36],[3.40,51.38],[3.37,51.38],[3.37,51.36]]]}"
                },
                new Zone
                {
                    Id = 2,
                    Name = "Kortrijk",
                    CountryCode = "BE",
                    RegionId = 1,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[3.24,50.81],[3.29,50.81],[3.29,50.84],[3.24,50.84],[3.24,50.81]]]}"
                },
                new Zone
                {
                    Id = 3,
                    Name = "Kempen",
                    CountryCode = "BE",
                    RegionId = 1,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[5.15,51.20],[5.25,51.20],[5.25,51.30],[5.15,51.30],[5.15,51.20]]]}"
                },
                new Zone
                {
                    Id = 4,
                    Name = "Sint-Niklaas",
                    CountryCode = "BE",
                    RegionId = 1,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[4.12,51.15],[4.17,51.15],[4.17,51.18],[4.12,51.18],[4.12,51.15]]]}"
                },
                new Zone
                {
                    Id = 5,
                    Name = "Genk",
                    CountryCode = "BE",
                    RegionId = 1,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[5.48,50.95],[5.53,50.95],[5.53,50.98],[5.48,50.98],[5.48,50.95]]]}"
                },
                new Zone
                {
                    Id = 6,
                    Name = "Mechelen",
                    CountryCode = "BE",
                    RegionId = 1,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[4.46,51.01],[4.50,51.01],[4.50,51.04],[4.46,51.04],[4.46,51.01]]]}"
                },
                new Zone
                {
                    Id = 7,
                    Name = "Blankenberge",
                    CountryCode = "BE",
                    RegionId = 1,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[3.12,51.30],[3.17,51.30],[3.17,51.33],[3.12,51.33],[3.12,51.30]]]}"
                },
                new Zone
                {
                    Id = 8,
                    Name = "Dender en Vlaamse Ardennen",
                    CountryCode = "BE",
                    RegionId = 1,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[3.85,50.75],[4.05,50.75],[4.05,50.85],[3.85,50.85],[3.85,50.75]]]}"
                },
                // Spain - RegionId = 2  
                new Zone
                {
                    Id = 9,
                    Name = "Torrevieja",
                    CountryCode = "ES",
                    RegionId = 2,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-0.70,37.96],[-0.65,37.96],[-0.65,37.99],[-0.70,37.99],[-0.70,37.96]]]}"
                },
                new Zone
                {
                    Id = 10,
                    Name = "Orihuela",
                    CountryCode = "ES",
                    RegionId = 2,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-0.95,37.98],[-0.90,37.98],[-0.90,38.01],[-0.95,38.01],[-0.95,37.98]]]}"
                },
                new Zone
                {
                    Id = 11,
                    Name = "Albir",
                    CountryCode = "ES",
                    RegionId = 2,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-0.07,38.55],[0.02,38.55],[0.02,38.60],[-0.07,38.60],[-0.07,38.55]]]}"
                },
                new Zone
                {
                    Id = 12,
                    Name = "Lanzarote",
                    CountryCode = "ES",
                    RegionId = 2,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-13.60,28.95],[-13.55,28.95],[-13.55,28.98],[-13.60,28.98],[-13.60,28.95]]]}"
                },
                new Zone
                {
                    Id = 13,
                    Name = "Altea",
                    CountryCode = "ES",
                    RegionId = 2,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-0.05,38.58],[0.00,38.58],[0.00,38.61],[-0.05,38.61],[-0.05,38.58]]]}"
                },
                new Zone
                {
                    Id = 14,
                    Name = "La Nucia",
                    CountryCode = "ES",
                    RegionId = 2,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-0.15,38.62],[-0.10,38.62],[-0.10,38.65],[-0.15,38.65],[-0.15,38.62]]]}"
                },
                new Zone
                {
                    Id = 15,
                    Name = "Fuertaventura",
                    CountryCode = "ES",
                    RegionId = 2,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-14.10,28.35],[-14.05,28.35],[-14.05,28.38],[-14.10,28.38],[-14.10,28.35]]]}"
                },
                new Zone
                {
                    Id = 16,
                    Name = "Tenerife",
                    CountryCode = "ES",
                    RegionId = 2,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-16.55,28.45],[-16.50,28.45],[-16.50,28.48],[-16.55,28.48],[-16.55,28.45]]]}"
                },
                // Greece - RegionId = 3
                new Zone
                {
                    Id = 17,
                    Name = "Rhodos",
                    CountryCode = "GR",
                    RegionId = 3,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[28.20,36.42],[28.25,36.42],[28.25,36.45],[28.20,36.45],[28.20,36.42]]]}"
                },
                new Zone
                {
                    Id = 18,
                    Name = "Kos",
                    CountryCode = "GR",
                    RegionId = 3,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[27.27,36.88],[27.32,36.88],[27.32,36.91],[27.27,36.91],[27.27,36.88]]]}"
                },
                new Zone
                {
                    Id = 19,
                    Name = "Corfu",
                    CountryCode = "GR",
                    RegionId = 3,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[19.90,39.61],[19.95,39.61],[19.95,39.64],[19.90,39.64],[19.90,39.61]]]}"
                },
                new Zone
                {
                    Id = 20,
                    Name = "Rethymnon",
                    CountryCode = "GR",
                    RegionId = 3,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[24.46,35.36],[24.50,35.36],[24.50,35.39],[24.46,35.39],[24.46,35.36]]]}"
                },
                // Gibraltar - RegionId = 4
                new Zone
                {
                    Id = 21,
                    Name = "Gibraltar",
                    CountryCode = "GI",
                    RegionId = 4,
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-5.36,36.12],[-5.34,36.12],[-5.34,36.14],[-5.36,36.14],[-5.36,36.12]]]}"
                }
            );
            // Seed some sample ParkingZones for each zone (2-3 per zone)
            var parkingZones = new List<ParkingZone>();
            var parkingZoneId = 1;
            
            for (int zoneId = 1; zoneId <= 21; zoneId++)
            {
                var (baseLat, baseLon) = GetZoneCoordinates(zoneId, new Random(42));
                
                // Create 2-3 parking zones per zone
                for (int pzNum = 1; pzNum <= 3; pzNum++)
                {
                    var pzRandom = new Random(42 + parkingZoneId);
                    var offsetLat = (pzRandom.NextDouble() - 0.5) * 0.01; // Small offset
                    var offsetLon = (pzRandom.NextDouble() - 0.5) * 0.01;
                    
                    parkingZones.Add(new ParkingZone
                    {
                        Id = parkingZoneId,
                        Name = $"Parkeerzone {pzNum}",
                        Description = $"Parking zone {pzNum} in zone",
                        Latitude = baseLat + offsetLat,
                        Longitude = baseLon + offsetLon,
                        RadiusMeters = 75,
                        MaxCapacity = 15,
                        CurrentVehicleCount = pzRandom.Next(3, 12),
                        ZoneId = zoneId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    
                    parkingZoneId++;
                }
            }
            
            modelBuilder.Entity<ParkingZone>().HasData(parkingZones);

            // Seed Swappers
            modelBuilder.Entity<Swapper>().HasData(
                new Swapper
                {
                    Id = 1,
                    Name = "Pieter Van Der Berg",
                    Email = "pieter@hoppy.be",
                    Phone = "+32478123456"
                },
                new Swapper
                {
                    Id = 2,
                    Name = "Maria Gonzalez",
                    Email = "maria@hoppy.es",
                    Phone = "+34612345678"
                },
                new Swapper
                {
                    Id = 3,
                    Name = "Dimitris Papadopoulos",
                    Email = "dimitris@hoppy.gr",
                    Phone = "+30698765432"
                },
                new Swapper
                {
                    Id = 4,
                    Name = "Sarah Mitchell",
                    Email = "sarah@hoppy.gi",
                    Phone = "+35020012345"
                }
            );

            // Seed Vehicles with some low battery ones across different zones
            var vehicles = new List<Vehicle>();
            var vehicleRandom = new Random(42); // Fixed seed for consistent data
            
            // Create vehicles for each zone (8-12 vehicles per zone)
            for (int zoneId = 1; zoneId <= 21; zoneId++)
            {
                var vehicleCount = vehicleRandom.Next(8, 13); // Random number of vehicles per zone
                
                for (int vehicleNum = 1; vehicleNum <= vehicleCount; vehicleNum++)
                {
                    var vehicleId = ((zoneId - 1) * 15) + vehicleNum; // Ensure unique IDs
                    var batteryLevel = vehicleId <= 50 ? vehicleRandom.Next(5, 25) : vehicleRandom.Next(25, 95); // First 50 have low battery
                    var needsBattery = batteryLevel < 20;
                    
                    // Get approximate coordinates for each zone
                    var (lat, lon) = GetZoneCoordinates(zoneId, vehicleRandom);
                    
                    // Random vehicle type
                    var vehicleTypes = new[] { "E-Scooter", "E-Bike", "E-Moped" };
                    var vehicleType = vehicleTypes[vehicleRandom.Next(vehicleTypes.Length)];
                    
                    vehicles.Add(new Vehicle
                    {
                        Id = vehicleId,
                        ExternalId = $"HOP_{zoneId:D2}_{vehicleNum:D3}",
                        RegistrationNumber = $"HOP{vehicleId:D4}",
                        VehicleType = vehicleType,
                        ZoneId = zoneId,
                        Latitude = lat,
                        Longitude = lon,
                        BatteryLevel = batteryLevel,
                        NeedsBatteryReplacement = needsBattery,
                        IsAvailable = true,
                        LastUpdated = DateTime.UtcNow.AddMinutes(-vehicleRandom.Next(1, 180))
                    });
                }
            }
            
            modelBuilder.Entity<Vehicle>().HasData(vehicles);

            // Note: User seeding is now done in Program.cs with runtime password hashing
        }

        private (double lat, double lon) GetZoneCoordinates(int zoneId, Random random)
        {
            return zoneId switch
            {
                // Belgium
                1 => (51.37 + (random.NextDouble() * 0.02), 3.38 + (random.NextDouble() * 0.02)), // Cadzand
                2 => (50.82 + (random.NextDouble() * 0.02), 3.26 + (random.NextDouble() * 0.03)), // Kortrijk
                3 => (51.25 + (random.NextDouble() * 0.08), 5.20 + (random.NextDouble() * 0.08)), // Kempen
                4 => (51.16 + (random.NextDouble() * 0.02), 4.14 + (random.NextDouble() * 0.03)), // Sint-Niklaas
                5 => (50.96 + (random.NextDouble() * 0.02), 5.50 + (random.NextDouble() * 0.03)), // Genk
                6 => (51.02 + (random.NextDouble() * 0.02), 4.47 + (random.NextDouble() * 0.03)), // Mechelen
                7 => (51.31 + (random.NextDouble() * 0.02), 3.14 + (random.NextDouble() * 0.03)), // Blankenberge
                8 => (50.80 + (random.NextDouble() * 0.08), 3.95 + (random.NextDouble() * 0.15)), // Dender en Vlaamse Ardennen
                
                // Spain
                9 => (37.97 + (random.NextDouble() * 0.02), -0.67 + (random.NextDouble() * 0.03)), // Torrevieja
                10 => (37.99 + (random.NextDouble() * 0.02), -0.92 + (random.NextDouble() * 0.03)), // Orihuela
                11 => (38.57 + (random.NextDouble() * 0.03), -0.02 + (random.NextDouble() * 0.06)), // Albir
                12 => (28.96 + (random.NextDouble() * 0.02), -13.57 + (random.NextDouble() * 0.03)), // Lanzarote
                13 => (38.59 + (random.NextDouble() * 0.02), -0.02 + (random.NextDouble() * 0.03)), // Altea
                14 => (38.63 + (random.NextDouble() * 0.02), -0.12 + (random.NextDouble() * 0.03)), // La Nucia
                15 => (28.36 + (random.NextDouble() * 0.02), -14.07 + (random.NextDouble() * 0.03)), // Fuertaventura
                16 => (28.46 + (random.NextDouble() * 0.02), -16.52 + (random.NextDouble() * 0.03)), // Tenerife
                
                // Greece
                17 => (36.43 + (random.NextDouble() * 0.02), 28.22 + (random.NextDouble() * 0.03)), // Rhodos
                18 => (36.89 + (random.NextDouble() * 0.02), 27.29 + (random.NextDouble() * 0.03)), // Kos
                19 => (39.62 + (random.NextDouble() * 0.02), 19.92 + (random.NextDouble() * 0.03)), // Corfu
                20 => (35.37 + (random.NextDouble() * 0.02), 24.48 + (random.NextDouble() * 0.03)), // Rethymnon
                
                // Gibraltar
                21 => (36.13 + (random.NextDouble() * 0.01), -5.35 + (random.NextDouble() * 0.01)), // Gibraltar
                
                _ => (50.85, 4.35) // Default to Brussels if unknown
            };
        }
    }
}
