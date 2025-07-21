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
        private readonly IEmailService _emailService;
        private readonly string _jwtSecret;
        private readonly string _jwtIssuer;
        private readonly int _jwtExpirationHours;

        public AuthService(HoppyDbContext context, IConfiguration configuration, IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _emailService = emailService;
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

            // Check if temporary password has expired
            if (user.IsTemporaryPassword && user.TemporaryPasswordExpiresAt.HasValue && 
                user.TemporaryPasswordExpiresAt.Value < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Tijdelijk wachtwoord is verlopen. Neem contact op met een beheerder.");
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

            // Only admins can create users
            if (createdByUser.Role != UserRole.Admin)
            {
                throw new UnauthorizedAccessException("Alleen administrators kunnen gebruikers aanmaken");
            }

            // Check if email already exists
            if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            {
                throw new ArgumentException("E-mailadres bestaat al");
            }

            // Generate username based on first and last name
            var username = await GenerateUsernameAsync(request.FirstName, request.LastName);
            
            // Generate temporary password
            var temporaryPassword = GenerateTemporaryPassword();
            
            var user = new User
            {
                Username = username,
                Email = request.Email,
                PasswordHash = HashPassword(temporaryPassword),
                Role = request.Role,
                FirstName = request.FirstName,
                LastName = request.LastName,
                AssignedZoneId = request.AssignedZoneId,
                CreatedByUserId = createdByUserId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                IsTemporaryPassword = true,
                HasCompletedFirstLogin = false,
                TemporaryPasswordExpiresAt = DateTime.UtcNow.AddDays(7) // Password expires in 7 days
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Reload with zone info
            user = await _context.Users
                .Include(u => u.AssignedZone)
                .FirstAsync(u => u.Id == user.Id);

            // Send email with username and temporary password
            try
            {
                await _emailService.SendTemporaryPasswordEmailAsync(
                    user.Email,
                    user.FirstName ?? "User",
                    user.LastName ?? "User",
                    user.Username,
                    temporaryPassword
                );
            }
            catch (Exception ex)
            {
                // Log email error but don't fail user creation
                Console.WriteLine($"Failed to send email to {user.Email}: {ex.Message}");
            }

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
            
            // If this was a temporary password, mark it as changed
            if (user.IsTemporaryPassword)
            {
                user.IsTemporaryPassword = false;
                user.HasCompletedFirstLogin = true;
                user.TemporaryPasswordExpiresAt = null;
            }
            
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
                AssignedZoneName = user.AssignedZone?.Name,
                IsTemporaryPassword = user.IsTemporaryPassword,
                HasCompletedFirstLogin = user.HasCompletedFirstLogin
            };
        }

        private async Task<string> GenerateUsernameAsync(string firstName, string lastName)
        {
            // Normalize names (remove accents, convert to lowercase)
            var normalizedFirstName = NormalizeName(firstName);
            var normalizedLastName = NormalizeName(lastName);
            
            // Create base username
            var baseUsername = $"{normalizedFirstName}.{normalizedLastName}".ToLower();
            
            // Check if username exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == baseUsername);
            
            if (existingUser == null)
            {
                return baseUsername;
            }
            
            // If username exists, add a number suffix
            var counter = 1;
            string uniqueUsername;
            do
            {
                uniqueUsername = $"{baseUsername}{counter}";
                counter++;
            } while (await _context.Users.AnyAsync(u => u.Username == uniqueUsername));
            
            return uniqueUsername;
        }

        private string NormalizeName(string name)
        {
            // Remove special characters and accents
            var normalized = System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z]", "");
            
            // Convert common accented characters
            normalized = normalized
                .Replace("ä", "a").Replace("ë", "e").Replace("ï", "i").Replace("ö", "o").Replace("ü", "u")
                .Replace("à", "a").Replace("è", "e").Replace("ì", "i").Replace("ò", "o").Replace("ù", "u")
                .Replace("á", "a").Replace("é", "e").Replace("í", "i").Replace("ó", "o").Replace("ú", "u")
                .Replace("â", "a").Replace("ê", "e").Replace("î", "i").Replace("ô", "o").Replace("û", "u")
                .Replace("ç", "c").Replace("ñ", "n");
            
            return normalized;
        }

        private string GenerateTemporaryPassword()
        {
            // Generate a secure random password
            const string chars = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            var password = new char[12];
            
            // Ensure at least one uppercase, one lowercase, one digit, and one special character
            password[0] = chars[random.Next(0, 26)]; // uppercase
            password[1] = chars[random.Next(26, 52)]; // lowercase
            password[2] = chars[random.Next(52, 62)]; // digit
            password[3] = chars[random.Next(62, chars.Length)]; // special character
            
            // Fill the rest randomly
            for (int i = 4; i < password.Length; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }
            
            // Shuffle the password
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }
            
            return new string(password);
        }

        public async Task<int> GetUsersCountAsync()
        {
            return await _context.Users.CountAsync();
        }
    }
}
