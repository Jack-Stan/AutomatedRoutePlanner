using System.ComponentModel.DataAnnotations;

namespace HoppyRoute.Domain.Entities
{
    public class Swapper
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string? Phone { get; set; }
        
        // Navigation properties
        public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
    }
}
