using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models.ViewModels
{
    public class ProductViewModel
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 10000)]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        [Required]
        [Range(0, 1000)]
        [Display(Name = "Stock Available")]
        public int StockAvailable { get; set; }

        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }

        [Display(Name = "Category")]
        public string Category { get; set; } = "General";

        [Display(Name = "Brand")]
        public string Brand { get; set; } = "Unknown";

        [Display(Name = "Weight (kg)")]
        public decimal Weight { get; set; } = 0.1m;

        [Display(Name = "Dimensions")]
        public string Dimensions { get; set; } = "N/A";

        [Display(Name = "SKU")]
        public string SKU { get; set; } = string.Empty;

        [Display(Name = "Is Featured")]
        public bool IsFeatured { get; set; } = false;
    }

    public class ProductSearchViewModel
    {
        public string Search { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string SortBy { get; set; } = "name";
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<Product> Products { get; set; } = new List<Product>();
        public List<string> Categories { get; set; } = new List<string>();
    }
}