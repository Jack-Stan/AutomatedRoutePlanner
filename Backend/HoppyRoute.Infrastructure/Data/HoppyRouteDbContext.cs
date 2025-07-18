using Microsoft.EntityFrameworkCore;
using HoppyRoute.Domain.Entities;

namespace HoppyRoute.Infrastructure.Data
{
    public class HoppyRouteDbContext : DbContext
    {
        public HoppyRouteDbContext(DbContextOptions<HoppyRouteDbContext> options) : base(options)
        {
        }

        public DbSet<Swapper> Swappers { get; set; }
        public DbSet<Zone> Zones { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<RouteStop> RouteStops { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Zone)
                .WithMany(z => z.Vehicles)
                .HasForeignKey(v => v.ZoneId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Route>()
                .HasOne(r => r.Swapper)
                .WithMany(s => s.Routes)
                .HasForeignKey(r => r.SwapperId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Route>()
                .HasOne(r => r.Zone)
                .WithMany(z => z.Routes)
                .HasForeignKey(r => r.ZoneId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.Route)
                .WithMany(r => r.Stops)
                .HasForeignKey(rs => rs.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.Vehicle)
                .WithMany(v => v.RouteStops)
                .HasForeignKey(rs => rs.VehicleId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure indexes
            modelBuilder.Entity<Vehicle>()
                .HasIndex(v => new { v.ZoneId, v.BatteryLevel });

            modelBuilder.Entity<Route>()
                .HasIndex(r => new { r.Date, r.Status });

            modelBuilder.Entity<RouteStop>()
                .HasIndex(rs => new { rs.RouteId, rs.Order })
                .IsUnique();

            // Seed data for development
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Zones
            modelBuilder.Entity<Zone>().HasData(
                new Zone
                {
                    Id = 1,
                    Name = "Amsterdam",
                    CountryCode = "NL",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[4.7,52.3],[4.8,52.3],[4.8,52.4],[4.7,52.4],[4.7,52.3]]]}"
                },
                new Zone
                {
                    Id = 2,
                    Name = "Rotterdam",
                    CountryCode = "NL",
                    GeoJsonBoundary = "{\"type\":\"Polygon\",\"coordinates\":[[[4.4,51.9],[4.5,51.9],[4.5,52.0],[4.4,52.0],[4.4,51.9]]]}"
                }
            );

            // Seed Swappers
            modelBuilder.Entity<Swapper>().HasData(
                new Swapper
                {
                    Id = 1,
                    Name = "Jan de Vries",
                    Email = "jan@hoppy.nl",
                    Phone = "+31612345678"
                },
                new Swapper
                {
                    Id = 2,
                    Name = "Sarah Johnson",
                    Email = "sarah@hoppy.nl",
                    Phone = "+31687654321"
                }
            );

            // Seed Vehicles (mock data)
            var vehicles = new List<Vehicle>();
            var random = new Random(42); // Fixed seed for consistent data
            
            for (int i = 1; i <= 20; i++)
            {
                vehicles.Add(new Vehicle
                {
                    Id = i,
                    ExternalId = $"BIKE_{i:D3}",
                    ZoneId = i <= 10 ? 1 : 2, // 10 vehicles per zone
                    Latitude = i <= 10 ? 52.37 + (random.NextDouble() * 0.04) : 51.92 + (random.NextDouble() * 0.04),
                    Longitude = i <= 10 ? 4.89 + (random.NextDouble() * 0.04) : 4.48 + (random.NextDouble() * 0.04),
                    BatteryLevel = random.Next(5, 95), // Random battery level
                    LastUpdated = DateTime.UtcNow.AddMinutes(-random.Next(1, 60))
                });
            }
            
            modelBuilder.Entity<Vehicle>().HasData(vehicles);
        }
    }
}
