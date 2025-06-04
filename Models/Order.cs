using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace web_lab_4.Models
{
    public class Order
    {
        public int Id { get; set; }
        
        [Required]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        public DateTime OrderDate { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalPrice { get; set; }
        
        [Required]
        [StringLength(500)]
        public string ShippingAddress { get; set; } = string.Empty;
        
        [StringLength(1000)]
        public string? Notes { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Add this property
        
        // Navigation properties
        public ICollection<OrderDetail>? OrderDetails { get; set; } = new List<OrderDetail>();
    }
}