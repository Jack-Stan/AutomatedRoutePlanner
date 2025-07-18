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
        [Authorize]
        public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequestDto request)
        {
            try
            {
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
            catch (Exception)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden" });
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
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] UserRole? role)
        {
            try
            {
                var currentUserId = GetCurrentUserId();
                var currentUser = await _authService.GetUserByIdAsync(currentUserId);

                IEnumerable<UserDto> users;

                if (role.HasValue)
                {
                    // If requesting specific role, apply hierarchy rules
                    users = currentUser.Role switch
                    {
                        UserRole.Admin => await _authService.GetUsersByRoleAsync(role.Value),
                        UserRole.FleetManager when role.Value == UserRole.BatterySwapper 
                            => await _authService.GetUsersByRoleAsync(role.Value, currentUserId),
                        _ => throw new UnauthorizedAccessException("Geen toestemming om deze gebruikers te bekijken")
                    };
                }
                else
                {
                    // Return users based on current user's role
                    users = currentUser.Role switch
                    {
                        UserRole.Admin => await _authService.GetUsersByRoleAsync(UserRole.FleetManager),
                        UserRole.FleetManager => await _authService.GetUsersByRoleAsync(UserRole.BatterySwapper, currentUserId),
                        _ => new List<UserDto>()
                    };
                }

                return Ok(users);
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

        [HttpPut("users/{id}")]
        [Authorize]
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
        [Authorize]
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
            catch (Exception)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden" });
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
        [Authorize]
        public async Task<IActionResult> GetUsersCount()
        {
            try
            {
                // Only admin users can access this endpoint
                var currentUserId = GetCurrentUserId();
                var currentUser = await _authService.GetUserByIdAsync(currentUserId);
                
                if (currentUser?.Role != UserRole.Admin)
                {
                    return Forbid("Alleen admins kunnen het aantal gebruikers opvragen");
                }

                var count = await _authService.GetUsersCountAsync();
                return Ok(new { count });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Niet geautoriseerd" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Er is een fout opgetreden" });
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
