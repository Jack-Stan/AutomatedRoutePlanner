using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;
using Xunit;

namespace HoppyRoute.Tests.Controllers
{
    public class AuthControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public AuthControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the real DbContext
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<HoppyDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add InMemory database for testing
                    services.AddDbContext<HoppyDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("TestDb");
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsToken()
        {
            // Arrange
            await SeedTestUser();

            var loginRequest = new LoginRequestDto
            {
                Username = "testuser",
                Password = "Test123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            loginResponse.Should().NotBeNull();
            loginResponse!.Token.Should().NotBeNullOrEmpty();
            loginResponse.User.Username.Should().Be("testuser");
            loginResponse.User.Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorized()
        {
            // Arrange
            await SeedTestUser();

            var loginRequest = new LoginRequestDto
            {
                Username = "testuser",
                Password = "wrongpassword"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_NonExistentUser_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new LoginRequestDto
            {
                Username = "nonexistent",
                Password = "Test123!"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUsers_WithoutAuthentication_ReturnsUnauthorized()
        {
            // Act
            var response = await _client.GetAsync("/api/auth/users");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetUsers_WithAuthentication_ReturnsUsers()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/auth/users");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var users = JsonSerializer.Deserialize<List<UserDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            users.Should().NotBeNull();
            users.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task CreateUser_WithValidData_CreatesUser()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var createUserRequest = new CreateUserRequestDto
            {
                Role = UserRole.BatterySwapper,
                FirstName = "New",
                LastName = "User",
                Email = "newuser@example.com"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/users", createUserRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var createdUser = JsonSerializer.Deserialize<UserDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            createdUser.Should().NotBeNull();
            createdUser!.Username.Should().Be("new.user"); // Auto-generated username
            createdUser.Role.Should().Be(UserRole.BatterySwapper);
            createdUser.FirstName.Should().Be("New");
            createdUser.LastName.Should().Be("User");
            createdUser.Email.Should().Be("newuser@example.com");
        }

        [Fact]
        public async Task CreateUser_WithDuplicateEmail_ReturnsBadRequest()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var createUserRequest = new CreateUserRequestDto
            {
                Role = UserRole.BatterySwapper,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com" // Same as seeded user
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/users", createUserRequest);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task GetUsersCount_WithAuthentication_ReturnsCount()
        {
            // Arrange
            var token = await GetAuthTokenAsync();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.GetAsync("/api/auth/users/count");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var countResponse = JsonSerializer.Deserialize<JsonElement>(content);
            
            countResponse.GetProperty("count").GetInt32().Should().BeGreaterThan(0);
        }

        private async Task SeedTestUser()
        {
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<HoppyDbContext>();

            // Clear existing data
            context.Users.RemoveRange(context.Users);
            await context.SaveChangesAsync();

            // Add test user
            var testUser = new User
            {
                Username = "testuser",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Test123!"),
                Role = UserRole.Admin,
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com"
            };

            context.Users.Add(testUser);
            await context.SaveChangesAsync();
        }

        private async Task<string> GetAuthTokenAsync()
        {
            await SeedTestUser();

            var loginRequest = new LoginRequestDto
            {
                Username = "testuser",
                Password = "Test123!"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync();
            var loginResponse = JsonSerializer.Deserialize<LoginResponseDto>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return loginResponse!.Token;
        }
    }
}
