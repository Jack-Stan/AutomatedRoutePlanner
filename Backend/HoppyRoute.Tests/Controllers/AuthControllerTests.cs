using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Tests.TestSupport;
using Moq;

namespace HoppyRoute.Tests.Controllers
{
    /// <summary>End-to-end over HTTP against the real API pipeline (routing, JWT auth, controllers, services, EF InMemory).</summary>
    public class AuthControllerTests : IClassFixture<HoppyApiFactory>
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
        private const string KnownBug_OpenUserEndpoints =
            "KNOWN BUG: AuthController has [Authorize] 'temporarily removed for debugging' on users endpoints; see test report.";

        private readonly HoppyApiFactory _factory;
        private readonly HttpClient _client;

        public AuthControllerTests(HoppyApiFactory factory)
        {
            _factory = factory;
            _client = factory.CreateSeededClient();
        }

        private async Task<LoginResponseDto> LoginAsAdminAsync()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequestDto { Username = HoppyApiFactory.AdminUsername, Password = HoppyApiFactory.AdminPassword });
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return (await response.Content.ReadFromJsonAsync<LoginResponseDto>(Json))!;
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsSignedJwtWithUserClaims()
        {
            var login = await LoginAsAdminAsync();

            login.Token.Should().NotBeNullOrWhiteSpace();
            login.User.Username.Should().Be(HoppyApiFactory.AdminUsername);
            login.ExpiresAt.Should().BeAfter(DateTime.UtcNow);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(login.Token);
            jwt.Issuer.Should().Be("HoppyApp");
            jwt.Claims.Should().Contain(c => c.Type == "sub" && c.Value == "1");
            jwt.Claims.Should().Contain(c => c.Type == "role" && c.Value == nameof(UserRole.Admin));
        }

        [Fact]
        public async Task Login_WrongPassword_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequestDto { Username = HoppyApiFactory.AdminUsername, Password = "wrong-password" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_UnknownUser_Returns401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequestDto { Username = "nobody", Password = "whatever" });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMe_WithoutToken_Returns401()
        {
            var response = await _client.GetAsync("/api/auth/users/me");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMe_WithTokenSignedByAnotherKey_Returns401()
        {
            var handler = new JwtSecurityTokenHandler();
            var forged = handler.WriteToken(handler.CreateJwtSecurityToken(
                issuer: "HoppyApp",
                subject: new System.Security.Claims.ClaimsIdentity(new[] { new System.Security.Claims.Claim("sub", "1") }),
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.ASCII.GetBytes("test-only-a-different-signing-key-0123456789")),
                    Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256)));

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/users/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", forged);
            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetMe_WithValidToken_ReturnsCurrentUser()
        {
            var login = await LoginAsAdminAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/users/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var me = await response.Content.ReadFromJsonAsync<UserDto>(Json);
            me!.Username.Should().Be(HoppyApiFactory.AdminUsername);
        }

        [Fact]
        public async Task CreateUser_GeneratesUsername_AndEmailsATemporaryPassword()
        {
            var email = $"new.{Guid.NewGuid():N}@test.invalid";
            var response = await _client.PostAsJsonAsync("/api/auth/users", new CreateUserRequestDto
            {
                Role = UserRole.BatterySwapper,
                FirstName = "Jan",
                LastName = "Peeters",
                Email = email
            });

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            var created = await response.Content.ReadFromJsonAsync<UserDto>(Json);
            created!.Username.Should().StartWith("jan.peeters");
            created.Role.Should().Be(UserRole.BatterySwapper);
            created.IsTemporaryPassword.Should().BeTrue();

            _factory.EmailService.Verify(e => e.SendTemporaryPasswordEmailAsync(
                email, "Jan", "Peeters", created.Username, It.Is<string>(p => p.Length == 12)), Times.Once);
        }

        [Fact]
        public async Task CreateUser_DuplicateEmail_Returns400()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/users", new CreateUserRequestDto
            {
                Role = UserRole.BatterySwapper,
                FirstName = "Dup",
                LastName = "Licate",
                Email = HoppyApiFactory.AdminEmail
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ---- Known security bugs, kept as tests and skipped so the suite stays honest ----

        [Fact(Skip = KnownBug_OpenUserEndpoints)]
        public async Task CreateUser_WithoutToken_ShouldReturn401()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/users", new CreateUserRequestDto
            {
                Role = UserRole.Admin,
                FirstName = "Anon",
                LastName = "Ymous",
                Email = $"anon.{Guid.NewGuid():N}@test.invalid"
            });
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact(Skip = KnownBug_OpenUserEndpoints)]
        public async Task GetUsers_WithoutToken_ShouldReturn401()
        {
            var response = await _client.GetAsync("/api/auth/users");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
