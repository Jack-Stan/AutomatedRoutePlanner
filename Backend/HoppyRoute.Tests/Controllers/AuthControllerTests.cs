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

        private async Task<string> LoginAsync(string username, string password)
        {
            var response = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequestDto { Username = username, Password = password });
            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            return (await response.Content.ReadFromJsonAsync<LoginResponseDto>(Json))!.Token;
        }

        private static HttpRequestMessage Request(HttpMethod method, string url, string? token, object? body = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (token != null)
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            if (body != null)
                request.Content = JsonContent.Create(body);
            return request;
        }

        private static string Unique(string prefix) => $"{prefix}.{Guid.NewGuid():N}";

        /// <summary>Seeds a fresh user of the given role and returns a valid token for it.</summary>
        private async Task<(int Id, string Token)> TokenForNewUserAsync(UserRole role)
        {
            var username = Unique(role.ToString().ToLowerInvariant());
            const string password = "test-only-user-password";
            var id = _factory.SeedUser(role, username, password);
            return (id, await LoginAsync(username, password));
        }

        private static CreateUserRequestDto NewSwapper() => new()
        {
            Role = UserRole.BatterySwapper,
            FirstName = "Jan",
            LastName = "Peeters",
            Email = $"{Unique("new")}@test.invalid"
        };

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
            var admin = await LoginAsAdminAsync();
            var dto = NewSwapper();
            var email = dto.Email;
            var response = await _client.SendAsync(Request(HttpMethod.Post, "/api/auth/users", admin.Token, dto));

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
            var admin = await LoginAsAdminAsync();
            var response = await _client.SendAsync(Request(HttpMethod.Post, "/api/auth/users", admin.Token, new CreateUserRequestDto
            {
                Role = UserRole.BatterySwapper,
                FirstName = "Dup",
                LastName = "Licate",
                Email = HoppyApiFactory.AdminEmail
            }));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ---- Authorization on the user-management endpoints ----

        [Fact]
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

        [Fact]
        public async Task GetUsers_WithoutToken_ShouldReturn401()
        {
            var response = await _client.GetAsync("/api/auth/users");
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData(UserRole.FleetManager)]
        [InlineData(UserRole.BatterySwapper)]
        public async Task CreateUser_AsNonAdmin_Returns403_AndCreatesNothing(UserRole role)
        {
            var (_, token) = await TokenForNewUserAsync(role);
            var dto = NewSwapper();
            dto.Role = UserRole.Admin;

            var response = await _client.SendAsync(Request(HttpMethod.Post, "/api/auth/users", token, dto));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var adminToken = (await LoginAsAdminAsync()).Token;
            var all = await (await _client.SendAsync(Request(HttpMethod.Get, "/api/auth/users", adminToken)))
                .Content.ReadFromJsonAsync<List<UserDto>>(Json);
            all!.Should().NotContain(u => u.Email == dto.Email);
        }

        [Fact]
        public async Task CreateUser_RecordsTheTokenUserAsCreator_NotAFixedId()
        {
            var (secondAdminId, token) = await TokenForNewUserAsync(UserRole.Admin);
            secondAdminId.Should().NotBe(1);
            var dto = NewSwapper();

            var response = await _client.SendAsync(Request(HttpMethod.Post, "/api/auth/users", token, dto));

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            _factory.CreatedByUserIdOf(dto.Email).Should().Be(secondAdminId);
        }

        [Fact]
        public async Task CreateUser_AdminRole_AsAdmin_Returns403_AndCreatesNothing()
        {
            var admin = await LoginAsAdminAsync();
            var dto = NewSwapper();
            dto.Role = UserRole.Admin;

            var response = await _client.SendAsync(Request(HttpMethod.Post, "/api/auth/users", admin.Token, dto));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var all = await (await _client.SendAsync(Request(HttpMethod.Get, "/api/auth/users", admin.Token)))
                .Content.ReadFromJsonAsync<List<UserDto>>(Json);
            all!.Should().NotContain(u => u.Email == dto.Email);
            _factory.EmailService.Verify(e => e.SendTemporaryPasswordEmailAsync(
                dto.Email, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateUser_FleetManager_AsAdmin_Succeeds()
        {
            var admin = await LoginAsAdminAsync();
            var dto = NewSwapper();
            dto.Role = UserRole.FleetManager;

            var response = await _client.SendAsync(Request(HttpMethod.Post, "/api/auth/users", admin.Token, dto));

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            (await response.Content.ReadFromJsonAsync<UserDto>(Json))!.Role.Should().Be(UserRole.FleetManager);
        }

        [Fact]
        public async Task GetUsers_AsAdmin_ReturnsUsers()
        {
            var admin = await LoginAsAdminAsync();
            var response = await _client.SendAsync(Request(HttpMethod.Get, "/api/auth/users", admin.Token));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var users = await response.Content.ReadFromJsonAsync<List<UserDto>>(Json);
            users!.Should().Contain(u => u.Username == HoppyApiFactory.AdminUsername);
        }

        [Theory]
        [InlineData("GET", "/api/auth/users")]
        [InlineData("GET", "/api/auth/users?role=2")]
        [InlineData("GET", "/api/auth/users/count")]
        [InlineData("PUT", "/api/auth/users/1")]
        public async Task AdminEndpoints_WithoutToken_Return401(string method, string url)
        {
            var body = method == "PUT" ? new UpdateUserRequestDto { FirstName = "Hacked" } : null;
            var response = await _client.SendAsync(Request(new HttpMethod(method), url, null, body));
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("GET", "/api/auth/users")]
        [InlineData("GET", "/api/auth/users?role=2")]
        [InlineData("GET", "/api/auth/users/count")]
        [InlineData("PUT", "/api/auth/users/1")]
        public async Task AdminEndpoints_AsBatterySwapper_Return403(string method, string url)
        {
            var (_, token) = await TokenForNewUserAsync(UserRole.BatterySwapper);
            var body = method == "PUT" ? new UpdateUserRequestDto { FirstName = "Hacked" } : null;
            var response = await _client.SendAsync(Request(new HttpMethod(method), url, token, body));
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateUser_AsSwapper_CannotPromoteThemselvesToAdmin()
        {
            var (id, token) = await TokenForNewUserAsync(UserRole.BatterySwapper);
            var response = await _client.SendAsync(Request(HttpMethod.Put, $"/api/auth/users/{id}", token,
                new UpdateUserRequestDto { Role = UserRole.Admin }));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            var adminToken = (await LoginAsAdminAsync()).Token;
            var user = await (await _client.SendAsync(Request(HttpMethod.Get, $"/api/auth/users/{id}", adminToken)))
                .Content.ReadFromJsonAsync<UserDto>(Json);
            user!.Role.Should().Be(UserRole.BatterySwapper);
        }

        [Fact]
        public async Task UpdateUser_AsAdmin_UpdatesTheUser()
        {
            var (id, _) = await TokenForNewUserAsync(UserRole.BatterySwapper);
            var admin = await LoginAsAdminAsync();

            var response = await _client.SendAsync(Request(HttpMethod.Put, $"/api/auth/users/{id}", admin.Token,
                new UpdateUserRequestDto { FirstName = "Renamed" }));

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            (await response.Content.ReadFromJsonAsync<UserDto>(Json))!.FirstName.Should().Be("Renamed");
        }

        [Fact]
        public async Task GetUsersCount_AsAdmin_ReturnsCount()
        {
            var admin = await LoginAsAdminAsync();
            var response = await _client.SendAsync(Request(HttpMethod.Get, "/api/auth/users/count", admin.Token));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("count").GetInt32().Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task ChangePassword_WithoutToken_Returns401_AndLeavesTheAdminPasswordAlone()
        {
            var response = await _client.PostAsJsonAsync("/api/auth/users/change-password", new ChangePasswordRequestDto
            {
                CurrentPassword = HoppyApiFactory.AdminPassword,
                NewPassword = "test-only-attacker-password"
            });

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            await LoginAsAdminAsync(); // the old admin password still works
        }

        [Fact]
        public async Task ChangePassword_WithToken_ChangesTheCallersOwnPassword()
        {
            var username = Unique("swapper");
            const string oldPassword = "test-only-old-password";
            const string newPassword = "test-only-new-password";
            _factory.SeedUser(UserRole.BatterySwapper, username, oldPassword);
            var token = await LoginAsync(username, oldPassword);

            var response = await _client.SendAsync(Request(HttpMethod.Post, "/api/auth/users/change-password", token,
                new ChangePasswordRequestDto { CurrentPassword = oldPassword, NewPassword = newPassword }));

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            await LoginAsync(username, newPassword);
            await LoginAsAdminAsync(); // the admin was not touched
        }

        // ---- Per-user endpoints: GET users/{id}, deactivate, delete, reset-password ----

        [Theory]
        [InlineData("GET", "/api/auth/users/{0}")]
        [InlineData("DELETE", "/api/auth/users/{0}")]
        [InlineData("DELETE", "/api/auth/users/{0}/permanent")]
        [InlineData("POST", "/api/auth/users/{0}/reset-password")]
        public async Task PerUserEndpoints_WithoutToken_Return401(string method, string urlFormat)
        {
            var (targetId, _) = await TokenForNewUserAsync(UserRole.BatterySwapper);
            var response = await _client.SendAsync(Request(new HttpMethod(method), string.Format(urlFormat, targetId), null));
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Theory]
        [InlineData("GET", "/api/auth/users/{0}")]
        [InlineData("DELETE", "/api/auth/users/{0}")]
        [InlineData("DELETE", "/api/auth/users/{0}/permanent")]
        [InlineData("POST", "/api/auth/users/{0}/reset-password")]
        public async Task PerUserEndpoints_AsSwapperOnAnotherUser_Return403_AndChangeNothing(string method, string urlFormat)
        {
            var targetUsername = Unique("target");
            const string targetPassword = "test-only-target-password";
            var targetId = _factory.SeedUser(UserRole.BatterySwapper, targetUsername, targetPassword);
            var (_, swapperToken) = await TokenForNewUserAsync(UserRole.BatterySwapper);

            var response = await _client.SendAsync(Request(new HttpMethod(method), string.Format(urlFormat, targetId), swapperToken));

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.Content.Headers.ContentLength.GetValueOrDefault().Should().Be(0); // no user data, no temp password
            // Target still exists, is active, and still logs in with the old password.
            var login = await _client.PostAsJsonAsync("/api/auth/login",
                new LoginRequestDto { Username = targetUsername, Password = targetPassword });
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            (await login.Content.ReadFromJsonAsync<LoginResponseDto>(Json))!.User.IsTemporaryPassword.Should().BeFalse();
        }

        [Theory]
        [InlineData("DELETE", "/api/auth/users/{0}")]
        [InlineData("DELETE", "/api/auth/users/{0}/permanent")]
        [InlineData("POST", "/api/auth/users/{0}/reset-password")]
        public async Task AdminOnlyPerUserEndpoints_AsFleetManager_Return403(string method, string urlFormat)
        {
            var (targetId, _) = await TokenForNewUserAsync(UserRole.BatterySwapper);
            var (_, managerToken) = await TokenForNewUserAsync(UserRole.FleetManager);
            var response = await _client.SendAsync(Request(new HttpMethod(method), string.Format(urlFormat, targetId), managerToken));
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetUser_OwnRecord_AsSwapper_ReturnsIt()
        {
            var (id, token) = await TokenForNewUserAsync(UserRole.BatterySwapper);
            var response = await _client.SendAsync(Request(HttpMethod.Get, $"/api/auth/users/{id}", token));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadFromJsonAsync<UserDto>(Json))!.Id.Should().Be(id);
        }

        [Fact]
        public async Task GetUser_AsAdmin_ReturnsAnyUser()
        {
            var (id, _) = await TokenForNewUserAsync(UserRole.BatterySwapper);
            var admin = await LoginAsAdminAsync();
            var response = await _client.SendAsync(Request(HttpMethod.Get, $"/api/auth/users/{id}", admin.Token));

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadFromJsonAsync<UserDto>(Json))!.Id.Should().Be(id);
        }

        [Fact]
        public async Task DeactivateUser_AsAdmin_DeactivatesTheUser()
        {
            var username = Unique("swapper");
            const string password = "test-only-user-password";
            var id = _factory.SeedUser(UserRole.BatterySwapper, username, password);
            var admin = await LoginAsAdminAsync();

            var response = await _client.SendAsync(Request(HttpMethod.Delete, $"/api/auth/users/{id}", admin.Token));

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            var login = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = username, Password = password });
            login.StatusCode.Should().Be(HttpStatusCode.Unauthorized); // inactive users cannot log in
        }

        [Fact]
        public async Task DeleteUserPermanently_AsAdmin_RemovesTheUser()
        {
            var (id, _) = await TokenForNewUserAsync(UserRole.BatterySwapper);
            var admin = await LoginAsAdminAsync();

            var response = await _client.SendAsync(Request(HttpMethod.Delete, $"/api/auth/users/{id}/permanent", admin.Token));

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            (await _client.SendAsync(Request(HttpMethod.Get, $"/api/auth/users/{id}", admin.Token)))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task ResetUserPassword_AsAdmin_IssuesATemporaryPasswordThatLogsIn()
        {
            var username = Unique("swapper");
            const string oldPassword = "test-only-user-password";
            var id = _factory.SeedUser(UserRole.BatterySwapper, username, oldPassword);
            var admin = await LoginAsAdminAsync();

            var response = await _client.SendAsync(Request(HttpMethod.Post, $"/api/auth/users/{id}/reset-password", admin.Token));

            response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var temp = doc.RootElement.GetProperty("temporaryPassword").GetString()!;
            temp.Should().NotBeNullOrEmpty();
            await LoginAsync(username, temp);
            (await _client.PostAsJsonAsync("/api/auth/login", new LoginRequestDto { Username = username, Password = oldPassword }))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_Returns400_WithMessage_AndKeepsThePassword()
        {
            var username = Unique("swapper");
            const string password = "test-only-user-password";
            _factory.SeedUser(UserRole.BatterySwapper, username, password);
            var token = await LoginAsync(username, password);

            var response = await _client.SendAsync(Request(HttpMethod.Post, "/api/auth/users/change-password", token,
                new ChangePasswordRequestDto { CurrentPassword = "test-only-wrong-password", NewPassword = "test-only-new-password" }));

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            doc.RootElement.GetProperty("message").GetString().Should().Be("Huidig wachtwoord is onjuist");
            await LoginAsync(username, password); // unchanged
            // The token is still good: the user is not "logged out" by this.
            (await _client.SendAsync(Request(HttpMethod.Get, "/api/auth/users/me", token))).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestEmailEndpoint_IsGone(bool asAdmin)
        {
            var token = asAdmin ? (await LoginAsAdminAsync()).Token : null;
            var response = await _client.SendAsync(Request(HttpMethod.Post, "/api/auth/test-email", token, new
            {
                email = "victim@test.invalid",
                firstName = "A",
                lastName = "B",
                username = "x",
                temporaryPassword = "test-only"
            }));

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            _factory.EmailService.Verify(e => e.SendTemporaryPasswordEmailAsync(
                "victim@test.invalid", It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }
    }
}
