using HoppyRoute.Application.Interfaces;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace HoppyRoute.Tests.TestSupport
{
    /// <summary>
    /// Hosts the real API in-process with: a test-only JWT key, an InMemory database
    /// instead of SQL Server, and a mocked email service (nothing ever reaches SMTP).
    /// </summary>
    public class HoppyApiFactory : WebApplicationFactory<Program>
    {
        public const string AdminUsername = "testadmin";
        public const string AdminPassword = "test-only-admin-password";
        public const string AdminEmail = "admin@test.invalid";

        private readonly string _dbName = $"api-tests-{Guid.NewGuid()}";
        private bool _seeded;

        public Mock<IEmailService> EmailService { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // UseSetting (not an environment variable) so this never leaks into other tests.
            builder.UseSetting("Jwt:Secret", TestKeys.JwtSecret);
            builder.UseSetting("Jwt:Issuer", "HoppyApp");

            EmailService
                .Setup(e => e.SendTemporaryPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            builder.ConfigureServices(services =>
            {
                // EF Core 9 keeps provider config in IDbContextOptionsConfiguration<T> as well as
                // DbContextOptions<T>; both must go or SqlServer and InMemory end up registered together.
                services.RemoveAll<DbContextOptions<HoppyDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<HoppyDbContext>>();
                services.AddDbContext<HoppyDbContext>(o => o.UseInMemoryDatabase(_dbName));

                services.RemoveAll<IEmailService>();
                services.AddSingleton(EmailService.Object);
            });
        }

        /// <summary>Creates a client and makes sure the seeded admin exists.</summary>
        public HttpClient CreateSeededClient()
        {
            var client = CreateClient();
            lock (this)
            {
                if (!_seeded)
                {
                    using var scope = Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<HoppyDbContext>();
                    db.Users.Add(new User
                    {
                        Id = 1,
                        Username = AdminUsername,
                        Email = AdminEmail,
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(AdminPassword, 4),
                        Role = UserRole.Admin,
                        FirstName = "Test",
                        LastName = "Admin",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    });
                    db.SaveChanges();
                    _seeded = true;
                }
            }
            return client;
        }

        /// <summary>
        /// Adds an active user with a known (obviously fake) password straight into the test database
        /// and returns its id. Each call makes a fresh, uniquely named user so tests never share state.
        /// </summary>
        public int SeedUser(UserRole role, string username, string password)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HoppyDbContext>();
            var user = new User
            {
                Username = username,
                Email = $"{username}@test.invalid",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 4),
                Role = role,
                FirstName = "Test",
                LastName = role.ToString(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            db.SaveChanges();
            return user.Id;
        }

        public int? CreatedByUserIdOf(string email)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<HoppyDbContext>();
            return db.Users.Single(u => u.Email == email).CreatedByUserId;
        }
    }

    internal static class ServiceCollectionTestExtensions
    {
        public static void RemoveAll<T>(this IServiceCollection services)
        {
            foreach (var d in services.Where(d => d.ServiceType == typeof(T)).ToList())
                services.Remove(d);
        }
    }
}
