using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_lab_4.Models
{
    public class Product
    {
        public int Id { get; set; }
        
        [Required, StringLength(100)]
        public string Name { get; set; }
        
        [Range(0.01, 1000000000)]
        public decimal Price { get; set; }
        
        public string Description { get; set; }
        
        public string? ImageUrl { get; set; }
        
        // Thuộc tính mới cho thực phẩm chức năng
        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; } = 0; // Số lượng tồn kho
        
        [Range(0.01, 10000)]
        public decimal Weight { get; set; } = 0; // Trọng lượng (gram)
        
        [StringLength(50)]
        public string? Flavor { get; set; } // Vị (ví dụ: Chocolate, Vanilla, Strawberry)
        
        [StringLength(20)]
        public string WeightUnit { get; set; } = "g"; // Đơn vị trọng lượng (g, kg, ml, l)
        
        // Thuộc tính bổ sung cho thực phẩm chức năng
        [StringLength(100)]
        public string? Brand { get; set; } // Thương hiệu
        
        public DateTime? ExpiryDate { get; set; } // Ngày hết hạn
        
        [StringLength(500)]
        public string? Ingredients { get; set; } // Thành phần
        
        [StringLength(1000)]
        public string? NutritionalInfo { get; set; } // Thông tin dinh dưỡng
        
        [StringLength(200)]
        public string? UsageInstructions { get; set; } // Hướng dẫn sử dụng
        
        public bool IsAvailable { get; set; } = true; // Có sẵn hay không
        
        // Navigation properties
        public ICollection<ProductImage>? Images { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
        
        // Navigation property for reviews
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        // Computed properties
        public bool IsInStock => StockQuantity > 0;
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Now;
        public string DisplayWeight => $"{Weight} {WeightUnit}";

        // Calculated properties for reviews
        [NotMapped]
        public double AverageRating
        {
            get
            {
                if (Reviews == null || !Reviews.Any(r => r.IsApproved))
                    return 0;

                return Reviews.Where(r => r.IsApproved).Average(r => r.Rating);
            }
        }

        [NotMapped]
        public int ReviewCount
        {
            get
            {
                return Reviews?.Where(r => r.IsApproved).Count() ?? 0;
            }
        }

        [NotMapped]
        public string DisplayRating
        {
            get
            {
                return AverageRating > 0 ? AverageRating.ToString("F1") : "No rating";
            }
        }
    }
}
