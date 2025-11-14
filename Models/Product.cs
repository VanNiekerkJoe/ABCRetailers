using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ABCRetailers.Models
{
    public class Product
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required]
        public string SKU { get; set; } = string.Empty;

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? ShortDescription { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Price { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? ComparePrice { get; set; }

        [Range(0, double.MaxValue)]
        public decimal? CostPrice { get; set; }

        public int StockQuantity { get; set; } = 0;

        public int LowStockThreshold { get; set; } = 5;

        [Required]
        public int CategoryId { get; set; }

        [Required]
        public string Brand { get; set; } = string.Empty;

        public decimal Weight { get; set; } = 0.1m;

        public string? Dimensions { get; set; }
        public string? Color { get; set; }
        public string? Size { get; set; }
        public string? Material { get; set; }

        // FIXED: Changed from ImageUrl to MainImageUrl
        public string? MainImageUrl { get; set; }

        public string? ImageUrls { get; set; }
        public string? VideoUrl { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsFeatured { get; set; } = false;
        public bool IsPublished { get; set; } = true;
        public bool HasVariants { get; set; } = false;

        [Range(0, 5)]
        public decimal AverageRating { get; set; } = 0;

        public int ReviewCount { get; set; } = 0;
        public int ViewCount { get; set; } = 0;
        public int SalesCount { get; set; } = 0;

        public string? MetaTitle { get; set; }
        public string? MetaDescription { get; set; }
        public string? Tags { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual Category? Category { get; set; }

        // Computed property for views that need StockAvailable
        [NotMapped]
        public bool StockAvailable => StockQuantity > 0;

        // Helper property to get first image
        [NotMapped]
        public string DisplayImageUrl => !string.IsNullOrEmpty(MainImageUrl) ? MainImageUrl : "/images/default-product.png";
    }
}