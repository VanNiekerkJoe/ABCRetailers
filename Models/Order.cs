using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Order
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public int CustomerId { get; set; }
        [Required]
        public string ProductId { get; set; } = string.Empty;
        [Required]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        [Required]
        public int Quantity { get; set; }
        [Required]
        public decimal UnitPrice { get; set; }
        [Required]
        public decimal TotalPrice { get; set; }
        [Required]
        public string Status { get; set; } = "Placed";  // 'Placed', 'Processed'
    }
}