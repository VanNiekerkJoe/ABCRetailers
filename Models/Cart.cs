using System.ComponentModel.DataAnnotations;

namespace ABCRetailers.Models
{
    public class Cart
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public string ProductId { get; set; } = string.Empty;
        [Required]
        public int Quantity { get; set; } = 1;
    }
}