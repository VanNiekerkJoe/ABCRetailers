using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Order
    {
        [Key]
        public string Id { get; set; } = string.Empty;

        [Required]
        public string OrderNumber { get; set; } = string.Empty;

        [Required]
        public int CustomerId { get; set; }

        [Required]
        public string OrderStatus { get; set; } = "Pending";

        [Required]
        public string PaymentStatus { get; set; } = "Pending";

        [Required]
        public string PaymentMethod { get; set; } = string.Empty;

        public string Currency { get; set; } = "ZAR";

        [Range(0, double.MaxValue)]
        public decimal Subtotal { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TaxAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal ShippingAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DiscountAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Required]
        public string ShippingAddress { get; set; } = string.Empty;

        [Required]
        public string BillingAddress { get; set; } = string.Empty;

        public string? ShippingMethod { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CustomerNotes { get; set; }
        public string? AdminNotes { get; set; }

        public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
        public DateTime? PaidAt { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CancelledAt { get; set; }

        // Navigation properties - FIXED: Remove nullable from collection
        public virtual Customer? Customer { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}