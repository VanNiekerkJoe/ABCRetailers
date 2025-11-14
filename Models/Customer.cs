using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [DataType(DataType.Date)]
        public DateTime? DateOfBirth { get; set; }

        [StringLength(10)]
        public string? Gender { get; set; }

        public string? AvatarUrl { get; set; }

        public string? DefaultShippingAddress { get; set; }

        public string? DefaultBillingAddress { get; set; }

        public int LoyaltyPoints { get; set; } = 0;

        public int TotalOrders { get; set; } = 0;

        public decimal TotalSpent { get; set; } = 0;

        public DateTime? LastOrderDate { get; set; }

        public bool NewsletterSubscribed { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual User? User { get; set; }
    }
}