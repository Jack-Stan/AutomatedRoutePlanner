using FluentAssertions;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Exceptions;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Application.Services;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;
using HoppyRoute.Tests.TestSupport;
using Microsoft.Extensions.Configuration;
using Moq;

namespace HoppyRoute.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly HoppyDbContext _db = TestDb.Create();
        private readonly Mock<IEmailService> _email = new();
        private readonly AuthService _auth;
        private string? _lastEmailedPassword;

        public AuthServiceTests()
        {
            _email.Setup(e => e.SendTemporaryPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                  .Callback<string, string, string, string, string>((_, _, _, _, pw) => _lastEmailedPassword = pw)
                  .ReturnsAsync(true);
            _auth = new AuthService(_db, Config(TestKeys.JwtSecret), _email.Object);
        }

        public void Dispose() => _db.Dispose();

        private static IConfiguration Config(string? secret) =>
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret,
                ["Jwt:Issuer"] = "HoppyApp",
                ["Jwt:ExpirationHours"] = "24",
            }).Build();

        private async Task<User> AddUserAsync(string username, string password, UserRole role = UserRole.BatterySwapper,
            bool active = true, bool temporary = false, DateTime? tempExpires = null)
        {
            var user = new User
            {
                Username = username,
                Email = $"{username}@test.invalid",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password, 4),
                Role = role,
                FirstName = "Test",
                LastName = username,
                IsActive = active,
                IsTemporaryPassword = temporary,
                TemporaryPasswordExpiresAt = tempExpires,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            return user;
        }

        // ---------- configuration ----------

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Constructor_WithoutJwtSecret_Throws(string? secret)
        {
            var act = () => new AuthService(_db, Config(secret), _email.Object);
            act.Should().Throw<InvalidOperationException>().WithMessage("*Jwt:Secret*");
        }

        // ---------- login ----------

        [Fact]
        public async Task Login_ValidCredentials_ReturnsTokenThatValidates_AndStampsLastLogin()
        {
            var user = await AddUserAsync("swapper", "correct-horse");

            var result = await _auth.LoginAsync(new LoginRequestDto { Username = "swapper", Password = "correct-horse" });

            result.Token.Should().NotBeNullOrWhiteSpace();
            result.User.Id.Should().Be(user.Id);
            (await _auth.ValidateTokenAsync(result.Token)).Should().BeTrue();
            (await _auth.GetUserFromTokenAsync(result.Token))!.Username.Should().Be("swapper");
            (await _db.Users.FindAsync(user.Id))!.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public async Task ValidateToken_SignedWithDifferentKey_IsRejected()
        {
            await AddUserAsync("swapper", "correct-horse");
            var other = new AuthService(TestDb.Create(), Config("test-only-a-different-signing-key-0123456789"), _email.Object);
            var token = (await _auth.LoginAsync(new LoginRequestDto { Username = "swapper", Password = "correct-horse" })).Token;

            (await other.ValidateTokenAsync(token)).Should().BeFalse();
            (await _auth.ValidateTokenAsync(token + "x")).Should().BeFalse();
        }

        [Theory]
        [InlineData("swapper", "wrong-password")]
        [InlineData("nobody", "correct-horse")]
        public async Task Login_BadCredentials_ThrowsUnauthorized(string username, string password)
        {
            await AddUserAsync("swapper", "correct-horse");

            var act = () => _auth.LoginAsync(new LoginRequestDto { Username = username, Password = password });

            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Ongeldige inloggegevens");
        }

        [Fact]
        public async Task Login_DeactivatedUser_ThrowsUnauthorized()
        {
            await AddUserAsync("gone", "correct-horse", active: false);

            var act = () => _auth.LoginAsync(new LoginRequestDto { Username = "gone", Password = "correct-horse" });

            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        // ---------- temporary-password flow ----------

        [Fact]
        public async Task TemporaryPasswordFlow_CreateLoginChange_ClearsTemporaryFlags()
        {
            var admin = await AddUserAsync("admin", "admin-pass", UserRole.Admin);

            // 1. Admin creates a swapper: a 12-char temporary password is generated and emailed.
            var created = await _auth.CreateUserAsync(new CreateUserRequestDto
            {
                FirstName = "Jan", LastName = "Peeters", Email = "jan@test.invalid", Role = UserRole.BatterySwapper
            }, admin.Id);

            created.Username.Should().Be("jan.peeters");
            created.IsTemporaryPassword.Should().BeTrue();
            _lastEmailedPassword.Should().NotBeNull().And.HaveLength(12);
            var stored = (await _db.Users.FindAsync(created.Id))!;
            stored.PasswordHash.Should().NotBe(_lastEmailedPassword, "only the hash is stored");
            stored.TemporaryPasswordExpiresAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromMinutes(1));

            // 2. The emailed temporary password works for login.
            var login = await _auth.LoginAsync(new LoginRequestDto { Username = "jan.peeters", Password = _lastEmailedPassword! });
            login.User.IsTemporaryPassword.Should().BeTrue();

            // 3. Changing it clears the temporary state.
            var temp = _lastEmailedPassword!;
            (await _auth.ChangePasswordAsync(created.Id, new ChangePasswordRequestDto { CurrentPassword = temp, NewPassword = "my-own-password" }))
                .Should().BeTrue();
            stored = (await _db.Users.FindAsync(created.Id))!;
            stored.IsTemporaryPassword.Should().BeFalse();
            stored.HasCompletedFirstLogin.Should().BeTrue();
            stored.TemporaryPasswordExpiresAt.Should().BeNull();

            // 4. Old temporary password no longer works, new one does.
            await FluentActions.Awaiting(() => _auth.LoginAsync(new LoginRequestDto { Username = "jan.peeters", Password = temp }))
                .Should().ThrowAsync<UnauthorizedAccessException>();
            (await _auth.LoginAsync(new LoginRequestDto { Username = "jan.peeters", Password = "my-own-password" })).Token.Should().NotBeEmpty();
        }

        [Fact]
        public async Task Login_ExpiredTemporaryPassword_ThrowsUnauthorized()
        {
            await AddUserAsync("late", "temp-pass", temporary: true, tempExpires: DateTime.UtcNow.AddMinutes(-1));

            var act = () => _auth.LoginAsync(new LoginRequestDto { Username = "late", Password = "temp-pass" });

            await act.Should().ThrowAsync<UnauthorizedAccessException>().WithMessage("Tijdelijk wachtwoord is verlopen*");
        }

        [Fact]
        public async Task ChangePassword_WrongCurrentPassword_Throws_AndKeepsOldPassword()
        {
            var user = await AddUserAsync("swapper", "correct-horse");

            var act = () => _auth.ChangePasswordAsync(user.Id, new ChangePasswordRequestDto { CurrentPassword = "nope", NewPassword = "x" });

            await act.Should().ThrowAsync<InvalidCurrentPasswordException>();
            BCrypt.Net.BCrypt.Verify("correct-horse", (await _db.Users.FindAsync(user.Id))!.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task ResetUserPassword_IssuesNewTemporaryPassword_ThatLogsIn()
        {
            var user = await AddUserAsync("swapper", "correct-horse");

            var (success, temp) = await _auth.ResetUserPasswordAsync(user.Id);

            success.Should().BeTrue();
            temp.Should().HaveLength(12).And.Be(_lastEmailedPassword);
            (await _auth.LoginAsync(new LoginRequestDto { Username = "swapper", Password = temp })).User.IsTemporaryPassword.Should().BeTrue();
            await FluentActions.Awaiting(() => _auth.LoginAsync(new LoginRequestDto { Username = "swapper", Password = "correct-horse" }))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task ResetUserPassword_Admin_IsRefused()
        {
            var admin = await AddUserAsync("admin", "admin-pass", UserRole.Admin);
            await FluentActions.Awaiting(() => _auth.ResetUserPasswordAsync(admin.Id)).Should().ThrowAsync<InvalidOperationException>();
        }

        [Fact]
        public void GenerateTemporaryPassword_AlwaysHas12CharsWithUpperLowerDigitAndSpecial()
        {
            // The generator promises one of each class. Sample enough to catch a broken guarantee.
            for (var i = 0; i < 2000; i++)
            {
                var pw = AuthService.GenerateTemporaryPassword();
                pw.Should().HaveLength(12);
                pw.Should().Match(p => p.Any(char.IsUpper), $"'{pw}' needs an uppercase letter");
                pw.Should().Match(p => p.Any(char.IsLower), $"'{pw}' needs a lowercase letter");
                pw.Should().Match(p => p.Any(char.IsDigit), $"'{pw}' needs a digit");
                pw.Should().Match(p => p.Any(c => "!@#$%^&*".Contains(c)), $"'{pw}' needs a special character");
            }
        }

        // ---------- user management rules ----------

        [Fact]
        public async Task CreateUser_ByNonAdmin_ThrowsUnauthorized()
        {
            var planner = await AddUserAsync("fleetmanager", "pw", UserRole.FleetManager);
            var act = () => _auth.CreateUserAsync(new CreateUserRequestDto { FirstName = "A", LastName = "B", Email = "ab@test.invalid", Role = UserRole.BatterySwapper }, planner.Id);
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task CreateUser_AdminCannotCreateAnotherAdmin()
        {
            var admin = await AddUserAsync("admin2", "pw", UserRole.Admin);
            var act = () => _auth.CreateUserAsync(new CreateUserRequestDto { FirstName = "A", LastName = "B", Email = "new-admin@test.invalid", Role = UserRole.Admin }, admin.Id);
            await act.Should().ThrowAsync<ForbiddenOperationException>();
            _db.Users.Should().NotContain(u => u.Email == "new-admin@test.invalid");
        }

        [Theory]
        [InlineData(UserRole.FleetManager)]
        [InlineData(UserRole.BatterySwapper)]
        public async Task CreateUser_AdminCanCreateFleetManagersAndSwappers(UserRole role)
        {
            var admin = await AddUserAsync($"admin-{role}", "pw", UserRole.Admin);
            var created = await _auth.CreateUserAsync(new CreateUserRequestDto { FirstName = "A", LastName = "B", Email = $"{role}@test.invalid", Role = role }, admin.Id);
            created.Role.Should().Be(role);
        }

        [Fact]
        public async Task CreateUser_DuplicateEmail_ThrowsArgumentException()
        {
            var admin = await AddUserAsync("admin", "pw", UserRole.Admin);
            var act = () => _auth.CreateUserAsync(new CreateUserRequestDto { FirstName = "A", LastName = "B", Email = admin.Email, Role = UserRole.BatterySwapper }, admin.Id);
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("E-mailadres bestaat al");
        }

        [Fact]
        public async Task CreateUser_SameName_GetsUniqueUsername()
        {
            var admin = await AddUserAsync("admin", "pw", UserRole.Admin);
            var first = await _auth.CreateUserAsync(new CreateUserRequestDto { FirstName = "Jan", LastName = "Peeters", Email = "j1@test.invalid", Role = UserRole.BatterySwapper }, admin.Id);
            var second = await _auth.CreateUserAsync(new CreateUserRequestDto { FirstName = "Jan", LastName = "Peeters", Email = "j2@test.invalid", Role = UserRole.BatterySwapper }, admin.Id);

            first.Username.Should().Be("jan.peeters");
            second.Username.Should().NotBe(first.Username).And.StartWith("jan.peeters");
        }

        [Fact]
        public async Task DeactivateUser_Swapper_IsDeactivated_AndCanNoLongerLogIn()
        {
            var user = await AddUserAsync("swapper", "correct-horse");

            (await _auth.DeactivateUserAsync(user.Id)).Should().BeTrue();

            (await _db.Users.FindAsync(user.Id))!.IsActive.Should().BeFalse();
            await FluentActions.Awaiting(() => _auth.LoginAsync(new LoginRequestDto { Username = "swapper", Password = "correct-horse" }))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task DeactivateUser_Admin_IsRefused()
        {
            var admin = await AddUserAsync("admin", "pw", UserRole.Admin);
            await FluentActions.Awaiting(() => _auth.DeactivateUserAsync(admin.Id))
                .Should().ThrowAsync<InvalidOperationException>().WithMessage("Admin gebruikers kunnen niet worden gedeactiveerd");
        }

        [Fact]
        public async Task DeactivateUser_Unknown_ReturnsFalse()
        {
            (await _auth.DeactivateUserAsync(9999)).Should().BeFalse();
        }
    }
}
