using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Tests.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace HoppyRoute.Tests.Controllers
{
    /// <summary>
    /// Unexpected exceptions must never reach the client: a generic 500 body, the details only in the server log.
    /// Services are swapped for mocks that throw an exception carrying a recognisable marker string.
    /// </summary>
    public class ErrorResponseTests : IClassFixture<HoppyApiFactory>
    {
        private const string Marker = "test-only-internal-detail-SqlConnection-42";

        private readonly WebApplicationFactory<Program> _factory;
        private readonly CapturingLoggerProvider _logs = new();

        public ErrorResponseTests(HoppyApiFactory baseFactory)
        {
            var auth = new Mock<IAuthService>();
            auth.Setup(a => a.GetUsersCountAsync()).ThrowsAsync(new InvalidOperationException(Marker));
            auth.Setup(a => a.LoginAsync(It.IsAny<HoppyRoute.Application.DTOs.LoginRequestDto>())).ThrowsAsync(new Exception(Marker));

            var vehicles = new Mock<IVehicleService>();
            vehicles.Setup(v => v.GetAllLowBatteryVehiclesAsync(It.IsAny<int>())).ThrowsAsync(new InvalidOperationException(Marker));

            var zones = new Mock<IZoneService>();
            zones.Setup(z => z.GetAllZonesAsync()).ThrowsAsync(new InvalidOperationException(Marker));

            _factory = baseFactory.WithWebHostBuilder(b =>
            {
                b.ConfigureLogging(l => l.AddProvider(_logs));
                b.ConfigureTestServices(s =>
                {
                    s.RemoveAll<IAuthService>();
                    s.AddSingleton(auth.Object);
                    s.RemoveAll<IVehicleService>();
                    s.AddSingleton(vehicles.Object);
                    s.RemoveAll<IZoneService>();
                    s.AddSingleton(zones.Object);
                });
            });
        }

        private static string AdminToken()
        {
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(TestKeys.JwtSecret));
            var token = new JwtSecurityToken(
                issuer: "HoppyApp",
                claims: new[] { new Claim("sub", "1"), new Claim("role", nameof(UserRole.Admin)) },
                expires: DateTime.UtcNow.AddMinutes(5),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        [Theory]
        [InlineData("GET", "/api/auth/users/count", true)]   // caught in AuthController
        [InlineData("POST", "/api/auth/login", false)]       // caught in AuthController (was a 400 with ex.Message)
        [InlineData("GET", "/api/zones/count", false)]       // caught in ZonesController
        [InlineData("GET", "/api/vehicles/lowbattery/all", false)] // not caught: global exception handler
        public async Task UnexpectedException_Returns500_WithoutExceptionText_AndIsLogged(string method, string url, bool asAdmin)
        {
            var client = _factory.CreateClient();
            var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (asAdmin)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", AdminToken());
            if (method == "POST")
                request.Content = new StringContent("{\"username\":\"a\",\"password\":\"b\"}", Encoding.UTF8, "application/json");

            var response = await client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            body.Should().NotContain(Marker);
            body.Should().NotContain("Exception");
            body.Should().NotContain(" at ");
            body.Should().Contain("Er is een fout opgetreden");
            _logs.Entries.Should().Contain(e => e.Level == LogLevel.Error && e.Exception != null && e.Exception.Message == Marker);
        }

        private sealed class CapturingLoggerProvider : ILoggerProvider
        {
            public System.Collections.Concurrent.ConcurrentQueue<(LogLevel Level, Exception? Exception)> Entries { get; } = new();
            public ILogger CreateLogger(string categoryName) => new Logger(this);
            public void Dispose() { }

            private sealed class Logger(CapturingLoggerProvider owner) : ILogger
            {
                public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
                public bool IsEnabled(LogLevel logLevel) => true;
                public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                    => owner.Entries.Enqueue((logLevel, exception));
            }
        }
    }
}
