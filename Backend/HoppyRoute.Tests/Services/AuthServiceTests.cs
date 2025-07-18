using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using HoppyRoute.Application.Services;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;
using Xunit;

namespace HoppyRoute.Tests.Services
{
    public class AuthServiceTests : IDisposable
    {
        private readonly HoppyDbContext _context;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            var options = new DbContextOptionsBuilder<HoppyDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new HoppyDbContext(options);
            _authService = new AuthService(_context);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsUser()
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

            // Act
            var result = await _authService.LoginAsync("testuser", "password123");

            // Assert
            result.Should().NotBeNull();
            result!.Username.Should().Be("testuser");
            result.Role.Should().Be(UserRole.Admin);
            result.FirstName.Should().Be("Test");
            result.LastName.Should().Be("User");
        }

        [Fact]
        public async Task LoginAsync_InvalidUsername_ReturnsNull()
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

            // Act
            var result = await _authService.LoginAsync("wronguser", "password123");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task LoginAsync_InvalidPassword_ReturnsNull()
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

            // Act
            var result = await _authService.LoginAsync("testuser", "wrongpassword");

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task CreateUserAsync_ValidData_CreatesUser()
        {
            // Arrange
            var createUserDto = new HoppyRoute.Application.DTOs.CreateUserDto
            {
                Username = "newuser",
                Password = "password123",
                Role = UserRole.Swapper,
                FirstName = "New",
                LastName = "User",
                Email = "newuser@example.com"
            };

            // Act
            var result = await _authService.CreateUserAsync(createUserDto);

            // Assert
            result.Should().NotBeNull();
            result.Username.Should().Be("newuser");
            result.Role.Should().Be(UserRole.Swapper);
            result.FirstName.Should().Be("New");
            result.LastName.Should().Be("User");
            result.Email.Should().Be("newuser@example.com");

            // Verify user was saved to database
            var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == "newuser");
            savedUser.Should().NotBeNull();
            BCrypt.Net.BCrypt.Verify("password123", savedUser!.PasswordHash).Should().BeTrue();
        }

        [Fact]
        public async Task CreateUserAsync_DuplicateUsername_ThrowsException()
        {
            // Arrange
            var existingUser = new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                Role = UserRole.Admin,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com"
            };

            _context.Users.Add(existingUser);
            await _context.SaveChangesAsync();

            var createUserDto = new HoppyRoute.Application.DTOs.CreateUserDto
            {
                Username = "testuser", // Same username
                Password = "password123",
                Role = UserRole.Swapper,
                FirstName = "New",
                LastName = "User",
                Email = "newuser@example.com"
            };

            // Act & Assert
            await FluentActions.Invoking(() => _authService.CreateUserAsync(createUserDto))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*gebruikersnaam*");
        }

        [Fact]
        public async Task GetUsersAsync_ReturnsAllUsers()
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
                    Role = UserRole.Swapper,
                    FirstName = "User",
                    LastName = "Two",
                    Email = "user2@example.com"
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            // Act
            var result = await _authService.GetUsersAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().Contain(u => u.Username == "user1");
            result.Should().Contain(u => u.Username == "user2");
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
                    Role = UserRole.Swapper,
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
        public async Task DeleteUserAsync_ExistingUser_DeletesUser()
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

            // Act
            var result = await _authService.DeleteUserAsync(user.Id);

            // Assert
            result.Should().BeTrue();

            // Verify user was deleted from database
            var deletedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
            deletedUser.Should().BeNull();
        }

        [Fact]
        public async Task DeleteUserAsync_NonExistingUser_ReturnsFalse()
        {
            // Act
            var result = await _authService.DeleteUserAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
