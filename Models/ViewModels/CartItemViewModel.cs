using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    public class CartItemViewModel
    {
        public int CartId { get; set; }

        [Required]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public string ProductDescription { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 10000)]
        public decimal Price { get; set; }

        [Required]
        [Range(1, 100)]
        public int Quantity { get; set; } = 1;

        public string? ImageUrl { get; set; }

        [Range(1, 100)]
        public int MaxQuantity { get; set; } = 10;

        public decimal TotalPrice => Price * Quantity;
    }
}