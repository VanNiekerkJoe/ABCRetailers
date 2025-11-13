using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Product
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        [Required]
        public string ProductName { get; set; } = string.Empty;
        [Required]
        public string Description { get; set; } = string.Empty;
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int StockAvailable { get; set; }
        public string? ImageUrl { get; set; }
    }
}