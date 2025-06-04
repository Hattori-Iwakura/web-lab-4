using System.ComponentModel.DataAnnotations;

namespace web_lab_4.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        [Required, StringLength(100)]
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; }
        
        // Calculate total price for this item
        public decimal TotalPrice => Price * Quantity;
    }
}