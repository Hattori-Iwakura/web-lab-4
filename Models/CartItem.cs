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
        
        // Thuộc tính mới cho thực phẩm chức năng
        public decimal Weight { get; set; } = 0;
        
        public string WeightUnit { get; set; } = "g";
        
        public string? Flavor { get; set; }
        
        public string? Brand { get; set; }
        
        // Computed properties
        public decimal TotalPrice => Price * Quantity;
        public decimal TotalWeight => Weight * Quantity;
        public string DisplayWeight => $"{Weight} {WeightUnit}";
        public string DisplayTotalWeight => $"{TotalWeight} {WeightUnit}";
    }
}