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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Zone configuration
            modelBuilder.Entity<Zone>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CountryCode).IsRequired().HasMaxLength(3);
                entity.Property(e => e.GeoJsonBoundary).IsRequired();
            });

            // Vehicle configuration
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.ExternalId).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Latitude).IsRequired();
                entity.Property(e => e.Longitude).IsRequired();
                entity.Property(e => e.BatteryLevel).IsRequired();
                entity.Property(e => e.LastUpdated).IsRequired();

                entity.HasOne(v => v.Zone)
                      .WithMany(z => z.Vehicles)
                      .HasForeignKey(v => v.ZoneId)
                      .OnDelete(DeleteBehavior.Cascade);
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
                entity.Property(e => e.Date).IsRequired();
                entity.Property(e => e.TargetDurationMinutes).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne(r => r.Swapper)
                      .WithMany(s => s.Routes)
                      .HasForeignKey(r => r.SwapperId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(r => r.Zone)
                      .WithMany(z => z.Routes)
                      .HasForeignKey(r => r.ZoneId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // RouteStop configuration
            modelBuilder.Entity<RouteStop>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Order).IsRequired();
                entity.Property(e => e.EstimatedArrivalOffset).IsRequired();
                entity.Property(e => e.EstimatedDurationAtStop).IsRequired();
                entity.Property(e => e.Status).IsRequired();

                entity.HasOne(rs => rs.Route)
                      .WithMany(r => r.Stops)
                      .HasForeignKey(rs => rs.RouteId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(rs => rs.Vehicle)
                      .WithMany(v => v.RouteStops)
                      .HasForeignKey(rs => rs.VehicleId)
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
            });

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Zones with real Hoppy locations
            modelBuilder.Entity<Zone>().HasData(
                // Belgium
                new Zone
                {
                    Id = 1,
                    Name = "Kortrijk",
                    CountryCode = "BE",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[3.24,50.81],[3.29,50.81],[3.29,50.84],[3.24,50.84],[3.24,50.81]]]}"
                },
                new Zone
                {
                    Id = 2,
                    Name = "Mechelen",
                    CountryCode = "BE", 
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[4.46,51.01],[4.50,51.01],[4.50,51.04],[4.46,51.04],[4.46,51.01]]]}"
                },
                new Zone
                {
                    Id = 3,
                    Name = "Sint-Niklaas",
                    CountryCode = "BE",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[4.12,51.15],[4.17,51.15],[4.17,51.18],[4.12,51.18],[4.12,51.15]]]}"
                },
                new Zone
                {
                    Id = 4,
                    Name = "Genk",
                    CountryCode = "BE",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[5.48,50.95],[5.53,50.95],[5.53,50.98],[5.48,50.98],[5.48,50.95]]]}"
                },
                new Zone
                {
                    Id = 5,
                    Name = "Blankenberge",
                    CountryCode = "BE",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[3.12,51.30],[3.17,51.30],[3.17,51.33],[3.12,51.33],[3.12,51.30]]]}"
                },
                // Spain
                new Zone
                {
                    Id = 6,
                    Name = "Torrevieja",
                    CountryCode = "ES",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-0.70,37.96],[-0.65,37.96],[-0.65,37.99],[-0.70,37.99],[-0.70,37.96]]]}"
                },
                new Zone
                {
                    Id = 7,
                    Name = "Altea",
                    CountryCode = "ES",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-0.05,38.58],[0.00,38.58],[0.00,38.61],[-0.05,38.61],[-0.05,38.58]]]}"
                },
                new Zone
                {
                    Id = 8,
                    Name = "Lanzarote",
                    CountryCode = "ES",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-13.60,28.95],[-13.55,28.95],[-13.55,28.98],[-13.60,28.98],[-13.60,28.95]]]}"
                },
                new Zone
                {
                    Id = 9,
                    Name = "Tenerife",
                    CountryCode = "ES",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-16.55,28.45],[-16.50,28.45],[-16.50,28.48],[-16.55,28.48],[-16.55,28.45]]]}"
                },
                // Greece
                new Zone
                {
                    Id = 10,
                    Name = "Rhodos",
                    CountryCode = "GR",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[28.20,36.42],[28.25,36.42],[28.25,36.45],[28.20,36.45],[28.20,36.42]]]}"
                },
                new Zone
                {
                    Id = 11,
                    Name = "Kos",
                    CountryCode = "GR",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[27.27,36.88],[27.32,36.88],[27.32,36.91],[27.27,36.91],[27.27,36.88]]]}"
                },
                new Zone
                {
                    Id = 12,
                    Name = "Corfu",
                    CountryCode = "GR",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[19.90,39.61],[19.95,39.61],[19.95,39.64],[19.90,39.64],[19.90,39.61]]]}"
                },
                // Gibraltar
                new Zone
                {
                    Id = 13,
                    Name = "Gibraltar",
                    CountryCode = "GI",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[-5.36,36.12],[-5.34,36.12],[-5.34,36.14],[-5.36,36.14],[-5.36,36.12]]]}"
                }
            );

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
            var random = new Random(42); // Fixed seed for consistent data
            
            // Create vehicles for each zone (5 vehicles per zone)
            for (int zoneId = 1; zoneId <= 13; zoneId++)
            {
                for (int vehicleNum = 1; vehicleNum <= 5; vehicleNum++)
                {
                    var vehicleId = (zoneId - 1) * 5 + vehicleNum;
                    var batteryLevel = vehicleId <= 20 ? random.Next(5, 25) : random.Next(25, 95); // First 20 have low battery
                    
                    // Get approximate coordinates for each zone
                    var (lat, lon) = GetZoneCoordinates(zoneId, random);
                    
                    vehicles.Add(new Vehicle
                    {
                        Id = vehicleId,
                        ExternalId = $"HOP_{zoneId:D2}_{vehicleNum:D2}",
                        ZoneId = zoneId,
                        Latitude = lat,
                        Longitude = lon,
                        BatteryLevel = batteryLevel,
                        LastUpdated = DateTime.UtcNow.AddMinutes(-random.Next(1, 180))
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
                1 => (50.82 + (random.NextDouble() * 0.02), 3.26 + (random.NextDouble() * 0.03)), // Kortrijk
                2 => (51.02 + (random.NextDouble() * 0.02), 4.47 + (random.NextDouble() * 0.03)), // Mechelen
                3 => (51.16 + (random.NextDouble() * 0.02), 4.14 + (random.NextDouble() * 0.03)), // Sint-Niklaas
                4 => (50.96 + (random.NextDouble() * 0.02), 5.50 + (random.NextDouble() * 0.03)), // Genk
                5 => (51.31 + (random.NextDouble() * 0.02), 3.14 + (random.NextDouble() * 0.03)), // Blankenberge
                6 => (37.97 + (random.NextDouble() * 0.02), -0.67 + (random.NextDouble() * 0.03)), // Torrevieja
                7 => (38.59 + (random.NextDouble() * 0.02), -0.02 + (random.NextDouble() * 0.03)), // Altea
                8 => (28.96 + (random.NextDouble() * 0.02), -13.57 + (random.NextDouble() * 0.03)), // Lanzarote
                9 => (28.46 + (random.NextDouble() * 0.02), -16.52 + (random.NextDouble() * 0.03)), // Tenerife
                10 => (36.43 + (random.NextDouble() * 0.02), 28.22 + (random.NextDouble() * 0.03)), // Rhodos
                11 => (36.89 + (random.NextDouble() * 0.02), 27.29 + (random.NextDouble() * 0.03)), // Kos
                12 => (39.62 + (random.NextDouble() * 0.02), 19.92 + (random.NextDouble() * 0.03)), // Corfu
                13 => (36.13 + (random.NextDouble() * 0.01), -5.35 + (random.NextDouble() * 0.01)), // Gibraltar
                _ => (50.85, 4.35) // Default to Brussels if unknown
            };
        }
    }
}
