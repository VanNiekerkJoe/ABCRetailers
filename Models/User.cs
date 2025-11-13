using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required]
        public string Password { get; set; } = string.Empty;  // Hash in production
        [Required]
        [StringLength(20)]
        public string Role { get; set; } = string.Empty;  // 'Admin' or 'Customer'
    }
}