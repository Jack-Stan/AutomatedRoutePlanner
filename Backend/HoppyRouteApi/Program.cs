using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HoppyRoute.Infrastructure.Data;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Application.Services;
using HoppyRoute.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database configuration
builder.Services.AddDbContext<HoppyDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// JWT Configuration
var jwtSecret = builder.Configuration["Jwt:Secret"] ?? "HoppySecretKey123!@#$%^&*()_+";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "HoppyApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = false,
        RequireExpirationTime = true,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRouteOptimizationService, HoppyRoute.Application.Services.RouteOptimizationService>();
builder.Services.AddScoped<IRouteService, HoppyRoute.Application.Services.RouteService>();
builder.Services.AddScoped<IVehicleService, HoppyRoute.Application.Services.VehicleService>();
builder.Services.AddScoped<IZoneService, HoppyRoute.Application.Services.ZoneService>();
builder.Services.AddScoped<IRouteStopService, HoppyRoute.Application.Services.RouteStopService>();

// CORS configuration for frontend
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:8081", "http://localhost:19006")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HoppyDbContext>();
    try
    {
        // Delete existing database and recreate with all tables including Users
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();
        
        // Check if we need to seed admin user
        if (!context.Users.Any())
        {
            // Create admin user with properly hashed password
            var adminUser = new HoppyRoute.Domain.Entities.User
            {
                Username = "admin",
                Email = "admin@hoppy.be",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("HoppyAdmin2024!", 12),
                Role = HoppyRoute.Domain.Enums.UserRole.Admin,
                FirstName = "Hoppy",
                LastName = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            context.SaveChanges();
            Console.WriteLine("Admin user created successfully!");
        }

        // Seed zones with realistic Hoppy regions: each region gets 10 zones
        if (!context.Zones.Any())
        {
            var zones = new List<Zone>();
            var random = new Random();
            
            // Define real Hoppy regions with base coordinates
            var regions = new[]
            {
                // België
                new { Name = "Cadzand", CountryCode = "BE", BaseLat = 51.3667, BaseLng = 3.3917 },
                new { Name = "Kortrijk", CountryCode = "BE", BaseLat = 50.8240, BaseLng = 3.2650 },
                new { Name = "Kempen", CountryCode = "BE", BaseLat = 51.1000, BaseLng = 5.0000 },
                new { Name = "Sint-Niklaas", CountryCode = "BE", BaseLat = 51.1667, BaseLng = 4.1333 },
                new { Name = "Genk", CountryCode = "BE", BaseLat = 50.9667, BaseLng = 5.5000 },
                new { Name = "Mechelen", CountryCode = "BE", BaseLat = 51.0275, BaseLng = 4.4775 },
                new { Name = "Blankenberge", CountryCode = "BE", BaseLat = 51.3133, BaseLng = 3.1317 },
                new { Name = "Dender en Vlaamse Ardennen", CountryCode = "BE", BaseLat = 50.7667, BaseLng = 3.8833 },
                
                // Spanje
                new { Name = "Torrevieja", CountryCode = "ES", BaseLat = 37.9785, BaseLng = -0.6820 },
                new { Name = "Orihuela", CountryCode = "ES", BaseLat = 38.0833, BaseLng = -0.9500 },
                new { Name = "Albir", CountryCode = "ES", BaseLat = 38.5333, BaseLng = 0.0167 },
                new { Name = "Lanzarote", CountryCode = "ES", BaseLat = 29.0469, BaseLng = -13.5896 },
                new { Name = "Altea", CountryCode = "ES", BaseLat = 38.5995, BaseLng = -0.0439 },
                new { Name = "La Nucia", CountryCode = "ES", BaseLat = 38.6225, BaseLng = -0.1308 },
                new { Name = "Fuerteventura", CountryCode = "ES", BaseLat = 28.3587, BaseLng = -14.0537 },
                new { Name = "Tenerife", CountryCode = "ES", BaseLat = 28.2916, BaseLng = -16.6291 },
                
                // Griekenland
                new { Name = "Rhodos", CountryCode = "GR", BaseLat = 36.4341, BaseLng = 28.2176 },
                new { Name = "Kos", CountryCode = "GR", BaseLat = 36.8928, BaseLng = 27.2881 },
                new { Name = "Corfu", CountryCode = "GR", BaseLat = 39.6243, BaseLng = 19.9217 },
                new { Name = "Rethymnon", CountryCode = "GR", BaseLat = 35.3662, BaseLng = 24.4824 },
                
                // Gibraltar
                new { Name = "Gibraltar", CountryCode = "GI", BaseLat = 36.1408, BaseLng = -5.3536 }
            };

            var zoneTypes = new[] { "Centrum", "Station", "Haven", "Strand", "Winkelcentrum", "Woonwijk", "Industriegebied", "Park", "Sportcomplex", "Toeristische Zone" };

            foreach (var region in regions)
            {
                for (int i = 0; i < 10; i++)
                {
                    // Create zone boundaries with small offsets from base coordinates
                    var offsetLat = (random.NextDouble() - 0.5) * 0.02; // ±0.01 degrees (~1km)
                    var offsetLng = (random.NextDouble() - 0.5) * 0.02; // ±0.01 degrees (~1km)
                    
                    var zoneLat = region.BaseLat + offsetLat;
                    var zoneLng = region.BaseLng + offsetLng;
                    
                    // Create small zone boundary (approximately 200m x 200m)
                    var zoneSize = 0.002; // ~200 meters
                    
                    zones.Add(new Zone
                    {
                        Name = $"{region.Name} {zoneTypes[i]}",
                        CountryCode = region.CountryCode,
                        GeoJsonBoundary = $@"{{
                            ""type"": ""Polygon"",
                            ""coordinates"": [[
                                [{zoneLng:F6}, {zoneLat:F6}],
                                [{zoneLng + zoneSize:F6}, {zoneLat:F6}],
                                [{zoneLng + zoneSize:F6}, {zoneLat + zoneSize:F6}],
                                [{zoneLng:F6}, {zoneLat + zoneSize:F6}],
                                [{zoneLng:F6}, {zoneLat:F6}]
                            ]]
                        }}"
                    });
                }
            }

            context.Zones.AddRange(zones);
            context.SaveChanges();
            Console.WriteLine($"Zones seeded successfully! Added {zones.Count} zones across {regions.Length} Hoppy regions (10 zones per region).");
        }
        
        // Seed vehicles distributed across zones: 50 vehicles per zone (45 in-zone + 5 out-of-zone)
        if (!context.Vehicles.Any())
        {
            var random = new Random();
            var vehicleTypes = new[] { "E-Scooter", "E-Bike" };
            var zones = context.Zones.ToList();
            var vehicles = new List<Vehicle>();

            foreach (var zone in zones)
            {
                // Add 45 vehicles per zone (in-zone)
                for (int i = 0; i < 45; i++)
                {
                    var vehicleType = vehicleTypes[random.Next(vehicleTypes.Length)];
                    var batteryLevel = random.Next(5, 101); // 5% to 100%
                    
                    // Generate random coordinates within zone boundary using the zone's GeoJSON
                    var coordinates = GetRandomCoordinatesInZone(zone);
                    
                    vehicles.Add(new Vehicle
                    {
                        ExternalId = $"HOP-{zone.Name.Substring(0, 3).ToUpper().Replace(" ", "")}-{i:D4}",
                        RegistrationNumber = $"{zone.Name.Substring(0, 3).ToUpper().Replace(" ", "")}{random.Next(1000, 9999)}",
                        VehicleType = vehicleType,
                        BatteryLevel = batteryLevel,
                        Latitude = coordinates.lat,
                        Longitude = coordinates.lng,
                        ZoneId = zone.Id,
                        LastUpdated = DateTime.UtcNow.AddMinutes(-random.Next(0, 180)) // Updated within last 3 hours
                    });
                }
                
                // Add 5 out-of-zone vehicles per zone (parked randomly outside zone boundaries)
                for (int i = 0; i < 5; i++)
                {
                    var vehicleType = vehicleTypes[random.Next(vehicleTypes.Length)];
                    var batteryLevel = random.Next(5, 101);
                    
                    // Generate coordinates OUTSIDE the zone but nearby (for pickup tasks)
                    var outOfZoneCoordinates = GetRandomOutOfZoneCoordinates(zone);
                    
                    vehicles.Add(new Vehicle
                    {
                        ExternalId = $"HOP-OUT-{zone.Name.Substring(0, 3).ToUpper().Replace(" ", "")}-{i:D4}",
                        RegistrationNumber = $"OUT{zone.Name.Substring(0, 3).ToUpper().Replace(" ", "")}{random.Next(1000, 9999)}",
                        VehicleType = vehicleType,
                        BatteryLevel = batteryLevel,
                        Latitude = outOfZoneCoordinates.lat,
                        Longitude = outOfZoneCoordinates.lng,
                        ZoneId = null, // No zone assigned - needs pickup by battery swapper!
                        LastUpdated = DateTime.UtcNow.AddMinutes(-random.Next(0, 360)) // Updated within last 6 hours
                    });
                }
            }

            context.Vehicles.AddRange(vehicles);
            context.SaveChanges();
            Console.WriteLine($"Vehicles seeded successfully! Added {vehicles.Count} vehicles across {zones.Count} zones (50 vehicles per zone: 45 in-zone + 5 out-of-zone).");
        }

        // Helper function to generate coordinates within zone boundary
        static (double lat, double lng) GetRandomCoordinatesInZone(Zone zone)
        {
            var random = new Random();
            
            // Extract coordinates from GeoJSON boundary (simplified approach)
            // For our generated zones, we know the pattern and can extract the bounds
            try
            {
                // Parse the zone name to determine region base coordinates
                var regionName = zone.Name.Split(' ')[0];
                
                // Use region-based coordinates with small random offset within zone
                var (baseLat, baseLng) = GetRegionBaseCoordinates(regionName);
                
                // Add small random offset within zone boundary (~200m zone)
                var offsetLat = (random.NextDouble() - 0.5) * 0.002; // ±0.001 degrees (~100m)
                var offsetLng = (random.NextDouble() - 0.5) * 0.002; // ±0.001 degrees (~100m)
                
                return (baseLat + offsetLat, baseLng + offsetLng);
            }
            catch
            {
                // Fallback to Belgium default
                return (50.8503, 4.3517);
            }
        }

        // Helper function to generate coordinates OUTSIDE zone boundaries (for pickup tasks)
        static (double lat, double lng) GetRandomOutOfZoneCoordinates(Zone zone)
        {
            var random = new Random();
            
            try
            {
                var regionName = zone.Name.Split(' ')[0];
                var (baseLat, baseLng) = GetRegionBaseCoordinates(regionName);
                
                // Generate coordinates outside the zone but nearby (for pickup tasks)
                // Add larger offset to be outside zone boundary but still in the area
                var offsetLat = (random.NextDouble() - 0.5) * 0.01 + (random.Next(2) == 0 ? 0.005 : -0.005); // ±0.005-0.01 degrees
                var offsetLng = (random.NextDouble() - 0.5) * 0.01 + (random.Next(2) == 0 ? 0.005 : -0.005); // ±0.005-0.01 degrees
                
                return (baseLat + offsetLat, baseLng + offsetLng);
            }
            catch
            {
                // Fallback to Belgium default (outside zone)
                return (50.8553, 4.3567);
            }
        }

        // Helper function to get base coordinates for each region
        static (double lat, double lng) GetRegionBaseCoordinates(string regionName)
        {
            return regionName switch
            {
                // België
                "Cadzand" => (51.3667, 3.3917),
                "Kortrijk" => (50.8240, 3.2650),
                "Kempen" => (51.1000, 5.0000),
                "Sint-Niklaas" => (51.1667, 4.1333),
                "Genk" => (50.9667, 5.5000),
                "Mechelen" => (51.0275, 4.4775),
                "Blankenberge" => (51.3133, 3.1317),
                "Dender" => (50.7667, 3.8833),
                
                // Spanje
                "Torrevieja" => (37.9785, -0.6820),
                "Orihuela" => (38.0833, -0.9500),
                "Albir" => (38.5333, 0.0167),
                "Lanzarote" => (29.0469, -13.5896),
                "Altea" => (38.5995, -0.0439),
                "La" => (38.6225, -0.1308), // La Nucia
                "Fuerteventura" => (28.3587, -14.0537),
                "Tenerife" => (28.2916, -16.6291),
                
                // Griekenland
                "Rhodos" => (36.4341, 28.2176),
                "Kos" => (36.8928, 27.2881),
                "Corfu" => (39.6243, 19.9217),
                "Rethymnon" => (35.3662, 24.4824),
                
                // Gibraltar
                "Gibraltar" => (36.1408, -5.3536),
                
                _ => (50.8503, 4.3517) // Default Brussels
            };
        }

        Console.WriteLine("Database initialized successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Database initialization failed: {ex.Message}");
    }
}

app.Run();
