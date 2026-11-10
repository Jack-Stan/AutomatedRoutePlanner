using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Exceptions;
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
            _jwtSecret = _configuration["Jwt:Secret"] ?? string.Empty;
            if (string.IsNullOrWhiteSpace(_jwtSecret))
                throw new InvalidOperationException("Jwt:Secret is not configured. Set the Jwt__Secret environment variable.");
            _jwtIssuer = _configuration["Jwt:Issuer"] ?? "HoppyApp";
            _jwtExpirationHours = int.Parse(_configuration["Jwt:ExpirationHours"] ?? "24");
        }

        public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
        {
            Console.WriteLine($"Login attempt for username: {request.Username}");
            
            var user = await _context.Users
                .Include(u => u.AssignedZone)
                .Include(u => u.AssignedRegion)
                .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

            var passwordValid = user != null && VerifyPassword(request.Password, user.PasswordHash);
            Console.WriteLine($"Login {(passwordValid ? "succeeded" : "failed")} for username: {request.Username}");

            if (user == null || !passwordValid)
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

            // Which roles this creator may hand out. Admins are never created through the API.
            if (!CanCreateUser(createdByUser.Role, request.Role))
            {
                throw new ForbiddenOperationException($"Je mag geen gebruiker met rol {request.Role} aanmaken");
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
                .Include(u => u.AssignedRegion)
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
                .Include(u => u.AssignedRegion)
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
                .Include(u => u.AssignedRegion)
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
                .Include(u => u.AssignedRegion)
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

            if (request.AssignedRegionId.HasValue)
            {
                user.AssignedRegionId = request.AssignedRegionId.Value;
            }

            if (request.Role.HasValue)
            {
                user.Role = request.Role.Value;
            }

            await _context.SaveChangesAsync();

            // Reload with updated zone and region info
            user = await _context.Users
                .Include(u => u.AssignedZone)
                .Include(u => u.AssignedRegion)
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
                throw new InvalidCurrentPasswordException("Huidig wachtwoord is onjuist");
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

            // Prevent deactivating admin users (protect system integrity)
            if (user.Role == UserRole.Admin)
            {
                throw new InvalidOperationException("Admin gebruikers kunnen niet worden gedeactiveerd");
            }

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return false;
            }

            // Prevent deleting admin users (protect system integrity)
            if (user.Role == UserRole.Admin)
            {
                throw new InvalidOperationException("Admin gebruikers kunnen niet worden verwijderd");
            }

            // Check if user is referenced in routes
            var hasRoutes = await _context.Routes.AnyAsync(r => r.AssignedSwapperId == userId || r.CreatedByUserId == userId || r.ApprovedBy == userId);
            if (hasRoutes)
            {
                throw new InvalidOperationException("Gebruiker kan niet worden verwijderd omdat deze gekoppeld is aan routes");
            }

            _context.Users.Remove(user);
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
                    new Claim("role", ((UserRole)user.Role).ToString()),
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
            try 
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
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
                UserRole.Admin => targetRole is UserRole.FleetManager or UserRole.BatterySwapper,
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
                AssignedRegionId = user.AssignedRegionId,
                AssignedRegionName = user.AssignedRegion?.Name,
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

        internal static string GenerateTemporaryPassword()
        {
            // Character classes are kept separate so the "one of each" guarantee holds.
            // (Previously one combined string was indexed with fixed ranges 0-26/26-52/52-62,
            // but the string omits 'I' and 'l', so those ranges straddled class boundaries.)
            const string upper = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";
            const string all = upper + lower + digits + special;

            var password = new char[12];
            password[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
            password[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
            password[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
            password[3] = special[RandomNumberGenerator.GetInt32(special.Length)];

            for (int i = 4; i < password.Length; i++)
            {
                password[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
            }

            // Shuffle (Fisher-Yates) so the guaranteed characters are not always in front.
            for (int i = password.Length - 1; i > 0; i--)
            {
                int j = RandomNumberGenerator.GetInt32(i + 1);
                (password[i], password[j]) = (password[j], password[i]);
            }

            return new string(password);
        }

        public async Task<(bool Success, string TemporaryPassword)> ResetUserPasswordAsync(int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return (false, string.Empty);
            }

            // Don't allow resetting admin password (additional protection)
            if (user.Role == UserRole.Admin)
            {
                throw new InvalidOperationException("Admin wachtwoord kan niet gereset worden");
            }

            // Generate new temporary password
            var temporaryPassword = GenerateTemporaryPassword();
            
            // Hash the temporary password
            user.PasswordHash = HashPassword(temporaryPassword);
            user.IsTemporaryPassword = true;
            user.TemporaryPasswordExpiresAt = DateTime.UtcNow.AddDays(7); // 7 days to change password

            await _context.SaveChangesAsync();

            // Send email with new temporary password
            try
            {
                await _emailService.SendTemporaryPasswordEmailAsync(
                    user.Email, 
                    user.FirstName ?? "Gebruiker", 
                    user.LastName ?? "", 
                    user.Username, 
                    temporaryPassword
                );
                
                return (true, temporaryPassword);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send reset password email: {ex.Message}");
                // Even if email fails, the password was reset
                return (true, temporaryPassword);
            }
        }

        public async Task<int> GetUsersCountAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<bool> InitiatePasswordResetAsync(string email)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
            if (user == null)
            {
                // Don't reveal that email doesn't exist for security
                return true;
            }

            // Don't allow password reset for admin users
            if (user.Role == UserRole.Admin)
            {
                return true; // Still return true to not reveal admin emails
            }

            // Generate reset token
            var resetToken = GenerateSecureToken();
            user.PasswordResetToken = resetToken;
            user.PasswordResetTokenExpires = DateTime.UtcNow.AddHours(1); // 1 hour expiry

            await _context.SaveChangesAsync();

            // Send password reset email
            try
            {
                var resetLink = $"https://hoppy.app/reset-password?token={resetToken}";
                await _emailService.SendPasswordResetEmailAsync(user.Email, user.FirstName ?? "Gebruiker", resetLink);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send password reset email: {ex.Message}");
                // Clear the token if email failed
                user.PasswordResetToken = null;
                user.PasswordResetTokenExpires = null;
                await _context.SaveChangesAsync();
                return false;
            }
        }

        public async Task<PasswordResetResult> ResetPasswordWithTokenAsync(string token, string newPassword)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => 
                u.PasswordResetToken == token && 
                u.PasswordResetTokenExpires > DateTime.UtcNow &&
                u.IsActive);

            if (user == null)
            {
                return new PasswordResetResult 
                { 
                    Success = false, 
                    ErrorMessage = "Ongeldige of verlopen reset token" 
                };
            }

            // Validate password strength
            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 8)
            {
                return new PasswordResetResult 
                { 
                    Success = false, 
                    ErrorMessage = "Wachtwoord moet minimaal 8 karakters lang zijn" 
                };
            }

            // Update password
            user.PasswordHash = HashPassword(newPassword);
            user.PasswordResetToken = null;
            user.PasswordResetTokenExpires = null;
            user.IsTemporaryPassword = false;

            await _context.SaveChangesAsync();

            return new PasswordResetResult { Success = true };
        }

        private string GenerateSecureToken()
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bytes = new byte[32];
                rng.GetBytes(bytes);
                return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
            }
        }

        public async Task<bool> AssignUserToRegionAsync(int userId, int regionId)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return false;

            user.AssignedRegionId = regionId;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<UserDto>> GetUsersByRegionAsync(int regionId)
        {
            var users = await _context.Users
                .Where(u => u.AssignedRegionId == regionId && u.IsActive)
                .ToListAsync();

            return users.Select(MapToUserDto).ToList();
        }
    }
}
