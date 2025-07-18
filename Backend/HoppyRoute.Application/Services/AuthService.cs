using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Domain.Entities;
using HoppyRoute.Domain.Enums;
using HoppyRoute.Infrastructure.Data;

namespace HoppyRoute.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly HoppyDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly int _jwtExpirationHours;

        public AuthService(HoppyDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _jwtSecret = _configuration["Jwt:Secret"] ?? "HoppySecretKey123!@#$%^&*()_+"; // Fallback for development
            _jwtIssuer = _configuration["Jwt:Issuer"] ?? "HoppyApp";
            _jwtExpirationHours = int.Parse(_configuration["Jwt:ExpirationHours"] ?? "24");
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            Console.WriteLine($"Login attempt for username: {request.Username}");
            
            var user = await _context.Users
                .Include(u => u.AssignedZone)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            Console.WriteLine($"User found: {user != null}");
            if (user != null)
            {
                Console.WriteLine($"User ID: {user.Id}, Username: {user.Username}, IsActive: {user.IsActive}");
                Console.WriteLine($"Password hash from DB: {user.PasswordHash}");
                
                var passwordValid = VerifyPassword(request.Password, user.PasswordHash);
                Console.WriteLine($"Password verification result: {passwordValid}");
            }

            if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Ongeldige inloggegevens");
            }

            // Update last login
            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddHours(_jwtExpirationHours);

            return new LoginResponseDto
            {
                Token = token,
                ExpiresAt = expiresAt,
                User = MapToUserDto(user)
            };
        }

        public async Task<UserDto> CreateUserAsync(CreateUserRequestDto request, int createdByUserId)
        {
            var createdByUser = await _context.Users.FindAsync(createdByUserId);
            if (createdByUser == null)
            {
                throw new UnauthorizedAccessException("Gebruiker niet gevonden");
            }

            // Verify permissions
            if (!CanCreateUser(createdByUser.Role, request.Role))
            {
                throw new UnauthorizedAccessException("Geen toestemming om deze gebruikersrol aan te maken");
            }

            // Check if username already exists
            if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            {
                throw new ArgumentException("Gebruikersnaam bestaat al");
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new ArgumentException("E-mailadres bestaat al");
            }

            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = HashPassword(request.Password),
                Role = request.Role,
                FirstName = request.FirstName,
                LastName = request.LastName,
                AssignedZoneId = request.AssignedZoneId,
                CreatedByUserId = createdByUserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Reload with zone info
            user = await _context.Users
                .Include(u => u.AssignedZone)
                .FirstAsync(u => u.Id == user.Id);

            return MapToUserDto(user);
        }

        public async Task<UserDto> GetUserByIdAsync(int userId)
        {
            var user = await _context.Users
                .Include(u => u.AssignedZone)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new ArgumentException("Gebruiker niet gevonden");
            }

            return MapToUserDto(user);
        }

        public async Task<IEnumerable<UserDto>> GetUsersByRoleAsync(UserRole role, int? createdByUserId = null)
        {
            var query = _context.Users
                .Include(u => u.AssignedZone)
                .Where(u => u.Role == role);

            if (createdByUserId.HasValue)
            {
                query = query.Where(u => u.CreatedByUserId == createdByUserId.Value);
            }

            var users = await query.ToListAsync();
            return users.Select(MapToUserDto);
        }

        public async Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequestDto request)
        {
            var user = await _context.Users
                .Include(u => u.AssignedZone)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                throw new ArgumentException("Gebruiker niet gevonden");
            }

            if (!string.IsNullOrEmpty(request.Email))
            {
                user.Email = request.Email;
            }

            if (!string.IsNullOrEmpty(request.FirstName))
            {
                user.FirstName = request.FirstName;
            }

            if (!string.IsNullOrEmpty(request.LastName))
            {
                user.LastName = request.LastName;
            }

            if (request.IsActive.HasValue)
            {
                user.IsActive = request.IsActive.Value;
            }

            if (request.AssignedZoneId.HasValue)
            {
                user.AssignedZoneId = request.AssignedZoneId.Value;
            }

            await _context.SaveChangesAsync();

            // Reload with updated zone info
            user = await _context.Users
                .Include(u => u.AssignedZone)
                .FirstAsync(u => u.Id == userId);

            return MapToUserDto(user);
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordRequestDto request)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            if (!VerifyPassword(request.CurrentPassword, user.PasswordHash))
            {
                throw new UnauthorizedAccessException("Huidig wachtwoord is onjuist");
            }

            user.PasswordHash = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeactivateUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public Task<bool> ValidateTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_jwtSecret);

                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public async Task<UserDto?> GetUserFromTokenAsync(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);

                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == "sub");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return null;
                }

                return await GetUserByIdAsync(userId);
            }
            catch
            {
                return null;
            }
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("sub", user.Id.ToString()),
                    new Claim("username", user.Username),
                    new Claim("role", user.Role.ToString()),
                    new Claim("zoneId", user.AssignedZoneId?.ToString() ?? "")
                }),
                Expires = DateTime.UtcNow.AddHours(_jwtExpirationHours),
                Issuer = _jwtIssuer,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        private bool VerifyPassword(string password, string hash)
        {
            Console.WriteLine($"Verifying password. Input password: '{password}'");
            Console.WriteLine($"Against hash: '{hash}'");
            
            try 
            {
                var result = BCrypt.Net.BCrypt.Verify(password, hash);
                Console.WriteLine($"BCrypt verification result: {result}");
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"BCrypt verification error: {ex.Message}");
                return false;
            }
        }

        private bool CanCreateUser(UserRole creatorRole, UserRole targetRole)
        {
            return creatorRole switch
            {
                UserRole.Admin => targetRole == UserRole.FleetManager,
                UserRole.FleetManager => targetRole == UserRole.BatterySwapper,
                _ => false
            };
        }

        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                FirstName = user.FirstName,
                LastName = user.LastName,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                AssignedZoneId = user.AssignedZoneId,
                AssignedZoneName = user.AssignedZone?.Name
            };
        }

        public async Task<int> GetUsersCountAsync()
        {
            return await _context.Users.CountAsync();
        }
    }
}
