namespace web_lab_4.Models
{
    public class OrderDetail
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        
        // Thuộc tính mới cho thực phẩm chức năng
        public decimal Weight { get; set; } = 0;
        public string WeightUnit { get; set; } = "g";
        public string? Flavor { get; set; }
        public string? ProductName { get; set; } // Lưu tên sản phẩm tại thời điểm đặt hàng
        
        // Navigation properties
        public Order? Order { get; set; }
        public Product? Product { get; set; }
        
        // Computed properties
        public decimal TotalPrice => Price * Quantity;
        public decimal TotalWeight => Weight * Quantity;
        public string DisplayWeight => $"{Weight} {WeightUnit}";
        public string DisplayTotalWeight => $"{TotalWeight} {WeightUnit}";
    }
}