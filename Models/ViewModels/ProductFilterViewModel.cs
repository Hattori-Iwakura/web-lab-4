using web_lab_4.Models;

namespace web_lab_4.Models.ViewModels
{
    public class ProductFilterViewModel
    {
        public IEnumerable<Product> Products { get; set; } = new List<Product>();
        public ProductFilterModel Filter { get; set; } = new ProductFilterModel();
        public IEnumerable<Category> Categories { get; set; } = new List<Category>();
        public IEnumerable<string> Brands { get; set; } = new List<string>();
        
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        
        public int TotalProducts { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiredCount { get; set; }
        public int AvailableCount { get; set; }
    }

    public class ProductFilterModel
    {
        public string? SearchTerm { get; set; }
        public int? CategoryId { get; set; }
        public string? Brand { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? MinStock { get; set; }
        public int? MaxStock { get; set; }
        
        // Thay đổi từ bool? thành bool
        public bool IsAvailable { get; set; } = false;
        public bool IsExpired { get; set; } = false;
        public bool IsLowStock { get; set; } = false;
        
        public DateTime? ExpiryFromDate { get; set; }
        public DateTime? ExpiryToDate { get; set; }
        public string SortBy { get; set; } = "Name";
        public string SortDirection { get; set; } = "asc";
    }
}