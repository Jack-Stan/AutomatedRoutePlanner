using System.ComponentModel.DataAnnotations;
using HoppyRoute.Domain.Enums;

namespace HoppyRoute.Domain.Entities
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        [StringLength(100)]
        public string? FirstName { get; set; }

        [StringLength(100)]
        public string? LastName { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LastLoginAt { get; set; }

        // New fields for temporary password functionality
        public bool IsTemporaryPassword { get; set; } = false;
        public bool HasCompletedFirstLogin { get; set; } = false;
        public DateTime? TemporaryPasswordExpiresAt { get; set; }

        // Password reset functionality
        public string? PasswordResetToken { get; set; }
        public DateTime? PasswordResetTokenExpires { get; set; }

        // For hierarchy: Fleet Managers created by Admin, Swappers created by Fleet Manager
        public int? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        // Navigation properties
        public ICollection<User> CreatedUsers { get; set; } = new List<User>();

        // Zone assignment for Fleet Managers and Swappers
        public int? AssignedZoneId { get; set; }
        public Zone? AssignedZone { get; set; }

        // Region assignment for Fleet Managers and Swappers
        public int? AssignedRegionId { get; set; }
        public Region? AssignedRegion { get; set; }

        // Routes created by this user (for Fleet Managers)
        public virtual ICollection<Route> CreatedRoutes { get; set; } = new List<Route>();

        // Routes assigned to this user (for Battery Swappers)
        public virtual ICollection<Route> AssignedRoutes { get; set; } = new List<Route>();
    }
}
