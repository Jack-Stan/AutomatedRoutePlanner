using HoppyRoute.Domain.Enums;

namespace HoppyRoute.Application.DTOs
{
    public class LoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public UserDto User { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public int? AssignedZoneId { get; set; }
        public string? AssignedZoneName { get; set; }
        public int? AssignedRegionId { get; set; }
        public string? AssignedRegionName { get; set; }
        public string RoleName => Role.ToString();
        public bool IsTemporaryPassword { get; set; }
        public bool HasCompletedFirstLogin { get; set; }
    }

    public class CreateUserRequestDto
    {
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public int? AssignedZoneId { get; set; }
    }

    public class UpdateUserRequestDto
    {
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public bool? IsActive { get; set; }
        public int? AssignedZoneId { get; set; }
        public int? AssignedRegionId { get; set; }
        public UserRole? Role { get; set; }
    }

    public class ChangePasswordRequestDto
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class FirstLoginPasswordChangeDto
    {
        public string TemporaryPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequestDto
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequestDto
    {
        public string Token { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class PasswordResetResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
