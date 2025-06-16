namespace web_lab_4.Models.Dashboard
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public decimal MonthlyRevenue { get; set; }
        public decimal TotalRevenue { get; set; }
        public int LowStockCount { get; set; }
        public int ExpiredProductsCount { get; set; }
        public int NewCustomersThisMonth { get; set; }
        
        public List<RecentOrderDto> RecentOrders { get; set; } = new List<RecentOrderDto>();
        public List<TopProductDto> TopProducts { get; set; } = new List<TopProductDto>();
        public List<LowStockProductDto> LowStockProducts { get; set; } = new List<LowStockProductDto>();
        public List<MonthlyRevenueDto> MonthlyRevenueData { get; set; } = new List<MonthlyRevenueDto>();
        public OrderStatusSummary OrderStatusSummary { get; set; } = new OrderStatusSummary();
    }

    public class RecentOrderDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; }
        public DateTime Date { get; set; }
        public int ItemCount { get; set; }
    }

    public class TopProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public int SalesCount { get; set; }
        public decimal Revenue { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
    }

    public class LowStockProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Stock { get; set; }
        public bool IsExpired { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Category { get; set; }
        public decimal Price { get; set; }
    }

    public class MonthlyRevenueDto
    {
        public string Month { get; set; }
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
    }

    public class OrderStatusSummary
    {
        public int Completed { get; set; }
        public int Processing { get; set; }
        public int Pending { get; set; }
        public int Cancelled { get; set; }
        public int Shipped { get; set; }
        public int Delivered { get; set; }
    }

    public class SalesChartData
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Data { get; set; } = new List<decimal>();
    }
}