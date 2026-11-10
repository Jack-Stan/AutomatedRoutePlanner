using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using HoppyRoute.Application.DTOs;
using HoppyRoute.Application.Interfaces;
using HoppyRoute.Domain.Enums;
using System.Security.Claims;

namespace HoppyRouteApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // Role names match the "role" claim AuthService puts in the JWT (UserRole.ToString()).
        private const string AdminRole = nameof(UserRole.Admin);

        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var result = await _authService.LoginAsync(request);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("users")]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequestDto request)
        {
            try
            {
                // The creator is whoever the token says it is, never a fixed id.
                var currentUserId = GetCurrentUserId();
                var result = await _authService.CreateUserAsync(request, currentUserId);
                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden", error = ex.Message });
            }
        }

        [HttpGet("users/me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            try
            {
                var userId = GetCurrentUserId();
                var user = await _authService.GetUserByIdAsync(userId);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("users/{id}")]
        [Authorize]
        public async Task<ActionResult<UserDto>> GetUser(int id)
        {
            // Admins may read anyone; everyone else only their own record.
            if (!User.IsInRole(AdminRole))
            {
                int callerId;
                try { callerId = GetCurrentUserId(); }
                catch (UnauthorizedAccessException ex) { return Unauthorized(new { message = ex.Message }); }

                if (callerId != id)
                {
                    return Forbid();
                }
            }

            try
            {
                var user = await _authService.GetUserByIdAsync(id);
                return Ok(user);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden" });
            }
        }

        [HttpGet("users")]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] UserRole? role, [FromQuery] int? zoneId)
        {
            try
            {
                IEnumerable<UserDto> users;

                if (role.HasValue)
                {
                    users = await _authService.GetUsersByRoleAsync(role.Value);
                    
                    // Filter by zone if specified
                    if (zoneId.HasValue)
                    {
                        users = users.Where(u => u.AssignedZoneId == zoneId.Value);
                    }
                }
                else
                {
                    // No role filter: return users of every role
                    var allUsers = new List<UserDto>();
                    foreach (UserRole userRole in Enum.GetValues<UserRole>())
                    {
                        var roleUsers = await _authService.GetUsersByRoleAsync(userRole);
                        allUsers.AddRange(roleUsers);
                    }
                    users = allUsers;
                    
                    // Filter by zone if specified
                    if (zoneId.HasValue)
                    {
                        users = users.Where(u => u.AssignedZoneId == zoneId.Value);
                    }
                }

                return Ok(users);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden", error = ex.Message });
            }
        }

        [HttpPut("users/{id}")]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequestDto request)
        {
            try
            {
                var result = await _authService.UpdateUserAsync(id, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden" });
            }
        }

        [HttpPost("users/change-password")]
        [Authorize]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequestDto request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest(new { message = "Request data is null" });
                }
                
                if (string.IsNullOrEmpty(request.CurrentPassword))
                {
                    return BadRequest(new { message = "Current password is required" });
                }
                
                if (string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new { message = "New password is required" });
                }
                
                // Always the authenticated user; a token without a usable id is a 401 (no fallback user).
                var userId = GetCurrentUserId();

                var result = await _authService.ChangePasswordAsync(userId, request);
                
                if (result)
                {
                    return Ok(new { message = "Wachtwoord succesvol gewijzigd" });
                }
                
                return BadRequest(new { message = "Wachtwoord wijzigen mislukt" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden" });
            }
        }

        [HttpDelete("users/{id}")]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult> DeactivateUser(int id)
        {
            try
            {
                var result = await _authService.DeactivateUserAsync(id);
                
                if (result)
                {
                    return Ok(new { message = "Gebruiker gedeactiveerd" });
                }
                
                return NotFound(new { message = "Gebruiker niet gevonden" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden", error = ex.Message });
            }
        }

        [HttpDelete("users/{id}/permanent")]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult> DeleteUser(int id)
        {
            try
            {
                var result = await _authService.DeleteUserAsync(id);
                
                if (result)
                {
                    return Ok(new { message = "Gebruiker permanent verwijderd" });
                }
                
                return NotFound(new { message = "Gebruiker niet gevonden" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden", error = ex.Message });
            }
        }

        [HttpPost("validate-token")]
        public async Task<ActionResult> ValidateToken([FromBody] string token)
        {
            try
            {
                var isValid = await _authService.ValidateTokenAsync(token);
                return Ok(new { isValid });
            }
            catch (Exception)
            {
                return BadRequest(new { message = "Token validatie mislukt" });
            }
        }

        [HttpGet("users/count")]
        [Authorize(Roles = AdminRole)]
        public async Task<IActionResult> GetUsersCount()
        {
            try
            {
                var count = await _authService.GetUsersCountAsync();
                return Ok(new { count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden", error = ex.Message });
            }
        }

        [HttpPost("users/{id}/reset-password")]
        [Authorize(Roles = AdminRole)]
        public async Task<ActionResult> ResetUserPassword(int id)
        {
            try
            {
                var result = await _authService.ResetUserPasswordAsync(id);
                
                if (result.Success)
                {
                    return Ok(new { 
                        message = "Nieuw tijdelijk wachtwoord verzonden", 
                        temporaryPassword = result.TemporaryPassword 
                    });
                }
                
                return NotFound(new { message = "Gebruiker niet gevonden" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden", error = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request?.Email))
                {
                    return BadRequest(new { message = "Email is verplicht" });
                }

                var result = await _authService.InitiatePasswordResetAsync(request.Email);
                
                // Always return success to prevent email enumeration attacks
                return Ok(new { 
                    message = "Als het emailadres bestaat, is er een wachtwoord reset link verzonden." 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden", error = ex.Message });
            }
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
        {
            try
            {
                if (request == null || string.IsNullOrEmpty(request.Token) || string.IsNullOrEmpty(request.NewPassword))
                {
                    return BadRequest(new { message = "Token en nieuw wachtwoord zijn verplicht" });
                }

                var result = await _authService.ResetPasswordWithTokenAsync(request.Token, request.NewPassword);
                
                if (result.Success)
                {
                    return Ok(new { message = "Wachtwoord succesvol gewijzigd" });
                }
                
                return BadRequest(new { message = result.ErrorMessage ?? "Ongeldige of verlopen token" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden", error = ex.Message });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Gebruiker niet geauthenticeerd");
            }

            return userId;
        }
    }
}
