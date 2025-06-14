using Microsoft.EntityFrameworkCore;
using web_lab_4.Data;
using web_lab_4.Models.Dashboard;

namespace web_lab_4.Repositories
{
    public class EFDashboardRepository : IDashboardRepository
    {
        private readonly ApplicationDbContext _context;

        public EFDashboardRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetTotalProductsAsync()
        {
            return await _context.Products.CountAsync();
        }

        public async Task<int> GetTotalOrdersAsync()
        {
            return await _context.Orders.CountAsync();
        }

        public async Task<int> GetPendingOrdersAsync()
        {
            return await _context.Orders.CountAsync(o => o.Status == "Pending");
        }

        public async Task<decimal> GetMonthlyRevenueAsync()
        {
            var now = DateTime.Now;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            return await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth && o.OrderDate <= endOfMonth && o.Status != "Cancelled")
                .SumAsync(o => o.TotalPrice);
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => o.TotalPrice);
        }

        public async Task<int> GetLowStockCountAsync()
        {
            return await _context.Products.CountAsync(p => p.StockQuantity <= 10);
        }

        public async Task<int> GetExpiredProductsCountAsync()
        {
            var now = DateTime.Now;
            return await _context.Products.CountAsync(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value < now);
        }

        public async Task<List<RecentOrderDto>> GetRecentOrdersAsync(int count)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .OrderByDescending(o => o.OrderDate)
                .Take(count)
                .Select(o => new RecentOrderDto
                {
                    Id = o.Id,
                    CustomerName = o.ShippingAddress.Contains(",") ? 
                        o.ShippingAddress.Substring(0, o.ShippingAddress.IndexOf(",")) : 
                        "Customer #" + o.UserId,
                    CustomerEmail = o.UserId,
                    Amount = o.TotalPrice,
                    Status = o.Status,
                    Date = o.OrderDate,
                    ItemCount = o.OrderDetails.Sum(od => od.Quantity)
                })
                .ToListAsync();
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(int count)
        {
            var topProducts = await _context.OrderDetails
                .Include(od => od.Product)
                .ThenInclude(p => p.Category)
                .Include(od => od.Order)
                .Where(od => od.Order.Status != "Cancelled")
                .GroupBy(od => od.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    SalesCount = g.Sum(od => od.Quantity),
                    Revenue = g.Sum(od => od.Price * od.Quantity),
                    Product = g.FirstOrDefault().Product
                })
                .OrderByDescending(x => x.SalesCount)
                .Take(count)
                .ToListAsync();

            return topProducts.Select(tp => new TopProductDto
            {
                Id = tp.ProductId,
                Name = tp.Product != null ? tp.Product.Name : "Unknown Product",
                ImageUrl = tp.Product != null ? tp.Product.ImageUrl : "",
                SalesCount = tp.SalesCount,
                Revenue = tp.Revenue,
                Price = tp.Product != null ? tp.Product.Price : 0,
                Category = tp.Product != null && tp.Product.Category != null ? tp.Product.Category.Name : "No Category"
            }).ToList();
        }

        public async Task<List<LowStockProductDto>> GetLowStockProductsAsync()
        {
            var now = DateTime.Now;
            return await _context.Products
                .Include(p => p.Category)
                .Where(p => p.StockQuantity <= 10 || (p.ExpiryDate.HasValue && p.ExpiryDate.Value < now))
                .OrderBy(p => p.StockQuantity)
                .Select(p => new LowStockProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Stock = p.StockQuantity,
                    IsExpired = p.ExpiryDate.HasValue && p.ExpiryDate.Value < now,
                    ExpiryDate = p.ExpiryDate,
                    Category = p.Category != null ? p.Category.Name : "No Category",
                    Price = p.Price
                })
                .ToListAsync();
        }

        public async Task<OrderStatusSummary> GetOrderStatusSummaryAsync()
        {
            var statusCounts = await _context.Orders
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync();

            var summary = new OrderStatusSummary();
            foreach (var status in statusCounts)
            {
                if (status.Status != null)
                {
                    switch (status.Status.ToLower())
                    {
                        case "completed":
                            summary.Completed = status.Count;
                            break;
                        case "processing":
                            summary.Processing = status.Count;
                            break;
                        case "pending":
                            summary.Pending = status.Count;
                            break;
                        case "cancelled":
                            summary.Cancelled = status.Count;
                            break;
                        case "shipped":
                            summary.Shipped = status.Count;
                            break;
                        case "delivered":
                            summary.Delivered = status.Count;
                            break;
                    }
                }
            }

            return summary;
        }

        public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueDataAsync(int months)
        {
            var startDate = DateTime.Now.AddMonths(-months + 1).Date;
            var endDate = DateTime.Now.Date.AddDays(1);

            var monthlyData = await _context.Orders
                .Where(o => o.OrderDate >= startDate && o.OrderDate < endDate && o.Status != "Cancelled")
                .GroupBy(o => new { Year = o.OrderDate.Year, Month = o.OrderDate.Month })
                .Select(g => new MonthlyRevenueDto
                {
                    Month = g.Key.Year.ToString() + "-" + g.Key.Month.ToString("D2"),
                    Revenue = g.Sum(o => o.TotalPrice),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Average(o => o.TotalPrice)
                })
                .OrderBy(x => x.Month)
                .ToListAsync();

            var result = new List<MonthlyRevenueDto>();
            for (int i = 0; i < months; i++)
            {
                var date = DateTime.Now.AddMonths(-months + 1 + i);
                var monthKey = date.Year.ToString() + "-" + date.Month.ToString("D2");
                var existingData = monthlyData.FirstOrDefault(x => x.Month == monthKey);
                
                if (existingData != null)
                {
                    existingData.Month = date.ToString("MMM yyyy");
                    result.Add(existingData);
                }
                else
                {
                    result.Add(new MonthlyRevenueDto
                    {
                        Month = date.ToString("MMM yyyy"),
                        Revenue = 0,
                        OrderCount = 0,
                        AverageOrderValue = 0
                    });
                }
            }

            return result;
        }
    }
}