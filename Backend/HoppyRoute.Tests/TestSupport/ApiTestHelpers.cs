using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HoppyRoute.Tests.TestSupport
{
    /// <summary>Shared arrange helpers for the HTTP tests: users with tokens, routes with stops, zones and regions.</summary>
    public static class ApiTestHelpers
    {
        public const string UserPassword = "test-only-user-password";

        /// <summary>Seeds a fresh user (optionally with a zone or region assignment) and logs in as them.</summary>
        public static async Task<(int Id, string Token)> NewUserAsync(
            this HoppyApiFactory factory, HttpClient client, UserRole role, int? zoneId = null, int? regionId = null)
        {
            var username = $"{role.ToString().ToLowerInvariant()}.{Guid.NewGuid():N}";
            var id = factory.SeedUser(role, username, UserPassword, zoneId, regionId);
            var login = await client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = username, Password = UserPassword });
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            return (id, (await login.Content.ReadFromJsonAsync<LoginResponseDto>())!.Token);
        }

        public static HttpRequestMessage Request(HttpMethod method, string url, string? token, object? body = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (token != null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body != null) request.Content = JsonContent.Create(body);
            return request;
        }

        public static T Read<T>(this HoppyApiFactory factory, Func<HoppyDbContext, T> query)
        {
            using var scope = factory.Services.CreateScope();
            return query(scope.ServiceProvider.GetRequiredService<HoppyDbContext>());
        }

        /// <summary>
        /// Two zones in one region plus one zone in another region, from the app's startup seed
        /// (10 zones per region). Zone ids are stable within one factory.
        /// </summary>
        public static (int RegionA, int ZoneA1, int ZoneA2, int RegionB, int ZoneB) Geography(this HoppyApiFactory factory)
        {
            return factory.Read(db =>
            {
                var zones = db.Zones.AsNoTracking().OrderBy(z => z.Id).ToList();
                var regionA = zones[0].RegionId;
                var inA = zones.Where(z => z.RegionId == regionA).Take(2).ToList();
                var b = zones.First(z => z.RegionId != regionA);
                return (regionA, inA[0].Id, inA[1].Id, b.RegionId, b.Id);
            });
        }

        /// <summary>
        /// Seeds a route in <paramref name="zoneId"/> assigned to the swapper, with one stop on a vehicle
        /// that has recognisable values. Returns the route id, stop id and the vehicle's registration number.
        /// </summary>
        public static (int RouteId, int StopId, string Registration) SeedRoute(
            this HoppyApiFactory factory, int swapperId, int zoneId,
            RouteStatus routeStatus = RouteStatus.Suggested,
            RouteApprovalStatus approval = RouteApprovalStatus.PendingApproval,
            RouteStopStatus stopStatus = RouteStopStatus.Pending)
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HoppyDbContext>();
            var registration = $"TST-{Guid.NewGuid():N}"[..12];
            var vehicle = new Vehicle
            {
                ExternalId = $"EXT-{Guid.NewGuid():N}",
                RegistrationNumber = registration,
                VehicleType = "TestScooter",
                ZoneId = zoneId,
                Latitude = 51.0,
                Longitude = 3.0,
                BatteryLevel = 12,
                NeedsBatteryReplacement = true,
                IsAvailable = true,
                LastUpdated = DateTime.UtcNow
            };
            db.Vehicles.Add(vehicle);
            var route = new Route
            {
                Name = $"Test route {Guid.NewGuid():N}",
                AssignedSwapperId = swapperId,
                CreatedByUserId = 1,
                ZoneId = zoneId,
                Date = DateTime.UtcNow.Date,
                TargetDurationMinutes = 60,
                Status = routeStatus,
                ApprovalStatus = approval,
                CreatedAt = DateTime.UtcNow
            };
            db.Routes.Add(route);
            db.SaveChanges();
            var stop = new RouteStop { RouteId = route.Id, VehicleId = vehicle.Id, Order = 1, Status = stopStatus };
            db.RouteStops.Add(stop);
            db.SaveChanges();
            return (route.Id, stop.Id, registration);
        }
    }
}
