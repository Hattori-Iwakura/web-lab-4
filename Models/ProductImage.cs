using System.ComponentModel.DataAnnotations;

namespace web_lab_4.Models
{
    public class ProductImage
    {
        public int Id { get; set; }
        [Required, StringLength(50)]
        public string Url { get; set; }
        public int ProductId { get; set; }
        public Product? Product { get; set; }
    }
}
