using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using FluentAssertions;
using HoppyRoute.Application.Services;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;
using Xunit;
using Moq;

namespace HoppyRoute.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly HoppyDbContext _context;
        private readonly AuthService _authService;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly Mock<IEmailService> _mockEmailService;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<HoppyDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new HoppyDbContext(options);
            
            // Mock IConfiguration
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(c => c["Jwt:Secret"]).Returns("TestSecretKey123!@#$%^&*()_+");
            _mockConfiguration.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfiguration.Setup(c => c["Jwt:ExpirationHours"]).Returns("24");
            
            // Mock IEmailService
            _mockEmailService = new Mock<IEmailService>();
            _mockEmailService.Setup(e => e.SendTemporaryPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            
            _authService = new AuthService(_context, _mockConfiguration.Object, _mockEmailService.Object);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsLoginResponse()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Admin,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginRequest = new LoginRequestDto
            {
                Username = "testuser",
                Password = "password123"
            };

            // Act
            var result = await _authService.LoginAsync(loginRequest);

            // Assert
            result.Should().NotBeNull();
            result.User.Username.Should().Be("testuser");
            result.User.Role.Should().Be(UserRole.Admin);
            result.User.FirstName.Should().Be("Test");
            result.User.LastName.Should().Be("User");
            result.Token.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public async Task LoginAsync_InvalidUsername_ThrowsUnauthorizedException()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Admin,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginRequest = new LoginRequestDto
            {
                Username = "wronguser",
                Password = "password123"
            };

            // Act & Assert
            await FluentActions.Invoking(() => _authService.LoginAsync(loginRequest))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedException()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Admin,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginRequest = new LoginRequestDto
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            // Act & Assert
            await FluentActions.Invoking(() => _authService.LoginAsync(loginRequest))
                .Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task CreateUserAsync_ValidData_CreatesUser()
        {
            // Arrange
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Admin,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com"
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            var createUserDto = new CreateUserRequestDto
            {
                Email = "new.user@example.com",
                Role = UserRole.BatterySwapper,
                FirstName = "New",
                LastName = "User"
            };

            // Act
            var result = await _authService.CreateUserAsync(createUserDto, adminUser.Id);

            // Assert
            result.Should().NotBeNull();
            result.Username.Should().Be("new.user"); // Generated username
            result.Role.Should().Be(UserRole.BatterySwapper);
            result.FirstName.Should().Be("New");
            result.LastName.Should().Be("User");
            result.Email.Should().Be("new.user@example.com");
            result.IsTemporaryPassword.Should().BeTrue();
            result.HasCompletedFirstLogin.Should().BeFalse();

            // Verify user was saved to database
            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "new.user");
            savedUser.Should().NotBeNull();
            savedUser!.IsTemporaryPassword.Should().BeTrue();
            savedUser.HasCompletedFirstLogin.Should().BeFalse();
        }

        [Fact]
        public async Task CreateUserAsync_DuplicateEmail_ThrowsException()
        {
            // Arrange
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Admin,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com"
            };

            var existingUser = new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Admin,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com"
            };

            _context.Users.AddRange(adminUser, existingUser);
            await _context.SaveChangesAsync();

            var createUserDto = new CreateUserRequestDto
            {
                Email = "test@example.com", // Same email
                Role = UserRole.BatterySwapper,
                FirstName = "New",
                LastName = "User"
            };

            // Act & Assert
            await FluentActions.Invoking(() => _authService.CreateUserAsync(createUserDto, adminUser.Id))
                .Should().ThrowAsync<ArgumentException>()
                .WithMessage("*E-mailadres*");
        }

        [Fact]
        public async Task CreateUserAsync_NonAdminUser_ThrowsUnauthorizedException()
        {
            // Arrange
            var fleetManagerUser = new User
            {
                Username = "fleetmanager",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.FleetManager,
                FirstName = "Fleet",
                LastName = "Manager",
                Email = "fleetmanager@example.com"
            };

            _context.Users.Add(fleetManagerUser);
            await _context.SaveChangesAsync();

            var createUserDto = new CreateUserRequestDto
            {
                Email = "new.user@example.com",
                Role = UserRole.BatterySwapper,
                FirstName = "New",
                LastName = "User"
            };

            // Act & Assert
            await FluentActions.Invoking(() => _authService.CreateUserAsync(createUserDto, fleetManagerUser.Id))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("*administrator*");
        }

        [Fact]
        public async Task LoginAsync_TemporaryPasswordExpired_ThrowsUnauthorizedException()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("temppassword123"),
                Role = UserRole.BatterySwapper,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                IsTemporaryPassword = true,
                HasCompletedFirstLogin = false,
                TemporaryPasswordExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired yesterday
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var loginRequest = new LoginRequestDto
            {
                Username = "testuser",
                Password = "temppassword123"
            };

            // Act & Assert
            await FluentActions.Invoking(() => _authService.LoginAsync(loginRequest))
                .Should().ThrowAsync<UnauthorizedAccessException>()
                .WithMessage("*verlopen*");
        }

        [Fact]
        public async Task ChangePasswordAsync_TemporaryPassword_UpdatesFields()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("temppassword123"),
                Role = UserRole.BatterySwapper,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                IsTemporaryPassword = true,
                HasCompletedFirstLogin = false,
                TemporaryPasswordExpiresAt = DateTime.UtcNow.AddDays(7)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var changePasswordRequest = new ChangePasswordRequestDto
            {
                CurrentPassword = "temppassword123",
                NewPassword = "newpassword123"
            };

            // Act
            var result = await _authService.ChangePasswordAsync(user.Id, changePasswordRequest);

            // Assert
            result.Should().BeTrue();

            // Verify password was changed and flags were updated
            var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            updatedUser.Should().NotBeNull();
            updatedUser!.IsTemporaryPassword.Should().BeFalse();
            updatedUser.HasCompletedFirstLogin.Should().BeTrue();
            updatedUser.TemporaryPasswordExpiresAt.Should().BeNull();
            BCrypt.Net.BCrypt.Verify("newpassword123", updatedUser.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task CreateUserAsync_ValidData_CallsEmailService()
        {
            // Arrange
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Admin,
                FirstName = "Admin",
                LastName = "User",
                Email = "admin@example.com"
            };

            _context.Users.Add(adminUser);
            await _context.SaveChangesAsync();

            var createUserDto = new CreateUserRequestDto
            {
                Email = "new.user@example.com",
                Role = UserRole.BatterySwapper,
                FirstName = "New",
                LastName = "User"
            };

            // Act
            var result = await _authService.CreateUserAsync(createUserDto, adminUser.Id);

            // Assert
            result.Should().NotBeNull();
            result.Username.Should().Be("new.user");
            result.Email.Should().Be("new.user@example.com");
            result.Role.Should().Be(UserRole.BatterySwapper);

            // Verify email service was called
            _mockEmailService.Verify(e => e.SendTemporaryPasswordEmailAsync(
                "new.user@example.com",
                "New",
                "User",
                "new.user",
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task GetUsersByRoleAsync_ReturnsUsersWithRole()
        {
            // Arrange
            var users = new[]
            {
                new User
                {
                    Username = "admin1",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = UserRole.Admin,
                    FirstName = "Admin",
                    LastName = "One",
                    Email = "admin1@example.com"
                },
                new User
                {
                    Username = "swapper1",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = UserRole.BatterySwapper,
                    FirstName = "Swapper",
                    LastName = "One",
                    Email = "swapper1@example.com"
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetUsersByRoleAsync(UserRole.Admin);

            // Assert
            result.Should().HaveCount(1);
            result.First().Username.Should().Be("admin1");
            result.First().Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public async Task GetUsersCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var users = new[]
            {
                new User
                {
                    Username = "user1",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = UserRole.Admin,
                    FirstName = "User",
                    LastName = "One",
                    Email = "user1@example.com"
                },
                new User
                {
                    Username = "user2",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = UserRole.BatterySwapper,
                    FirstName = "User",
                    LastName = "Two",
                    Email = "user2@example.com"
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetUsersCountAsync();

            // Assert
            result.Should().Be(2);
        }

        [Fact]
        public async Task DeactivateUserAsync_ExistingUser_DeactivatesUser()
        {
            // Arrange
            var user = new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Admin,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.DeactivateUserAsync(user.Id);

            // Assert
            result.Should().BeTrue();

            // Verify user was deactivated in database
            var deactivatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            deactivatedUser.Should().NotBeNull();
            deactivatedUser!.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task DeactivateUserAsync_NonExistingUser_ReturnsFalse()
        {
            // Act
            var result = await _authService.DeactivateUserAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
