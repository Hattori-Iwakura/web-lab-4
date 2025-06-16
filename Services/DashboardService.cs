using Microsoft.Extensions.Caching.Memory;
using web_lab_4.Repositories;
using web_lab_4.Models.Dashboard;
using web_lab_4.Data;
using Microsoft.EntityFrameworkCore;

namespace web_lab_4.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;
        private readonly ApplicationDbContext _context;
        private const string DASHBOARD_CACHE_KEY = "dashboard_data";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(15);

        public DashboardService(IDashboardRepository repository, IMemoryCache cache, ILogger<DashboardService> logger, ApplicationDbContext context)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
            _context = context;
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync()
        {
            if (_cache.TryGetValue(DASHBOARD_CACHE_KEY, out DashboardViewModel cachedData))
            {
                return cachedData;
            }

            try
            {
                var dashboard = new DashboardViewModel
                {
                    TotalProducts = await _repository.GetTotalProductsAsync(),
                    TotalOrders = await _repository.GetTotalOrdersAsync(),
                    PendingOrders = await _repository.GetPendingOrdersAsync(),
                    MonthlyRevenue = await _repository.GetMonthlyRevenueAsync(),
                    TotalRevenue = await _repository.GetTotalRevenueAsync(),
                    LowStockCount = await _repository.GetLowStockCountAsync(),
                    ExpiredProductsCount = await _repository.GetExpiredProductsCountAsync(),
                    NewCustomersThisMonth = 0 // Implement if needed
                };

                // Load detailed data
                dashboard.RecentOrders = await _repository.GetRecentOrdersAsync(10);
                dashboard.TopProducts = await _repository.GetTopProductsAsync(8);
                dashboard.LowStockProducts = await _repository.GetLowStockProductsAsync();
                //dashboard.MonthlyRevenueData = await _repository.GetMonthlyRevenueDataAsync(12);
                dashboard.OrderStatusSummary = await _repository.GetOrderStatusSummaryAsync();

                // Cache the result
                _cache.Set(DASHBOARD_CACHE_KEY, dashboard, CACHE_DURATION);

                return dashboard;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard data");
                return new DashboardViewModel();
            }
        }

        public async Task<SalesChartData> GetSalesChartDataAsync(string period = "year")
        {
            try
            {
                var now = DateTime.Now;
                var startDate = period.ToLower() switch
                {
                    "week" => now.AddDays(-7),
                    "month" => now.AddMonths(-1),
                    "year" => now.AddYears(-1),
                    _ => now.AddYears(-1)
                };

                var ordersRaw = await _context.Orders
                    .Where(o => o.OrderDate >= startDate && o.Status != "Cancelled")
                    .ToListAsync();

                var orders = ordersRaw
                    .GroupBy(o =>
                        period.ToLower() switch
                        {
                            "week" => o.OrderDate.Date,
                            "month" => new DateTime(o.OrderDate.Year, o.OrderDate.Month, 1),
                            "year" => new DateTime(o.OrderDate.Year, o.OrderDate.Month, 1),
                            _ => new DateTime(o.OrderDate.Year, o.OrderDate.Month, 1)
                        }
                    )
                    .Select(g => new
                    {
                        Date = g.Key,
                        Revenue = g.Sum(o => o.TotalPrice)
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                var labels = new List<string>();
                var data = new List<decimal>();

                if (orders.Any())
                {
                    foreach (var order in orders)
                    {
                        var label = period.ToLower() switch
                        {
                            "week" => order.Date.ToString("MMM dd"),
                            "month" => order.Date.ToString("MMM dd"),
                            "year" => order.Date.ToString("MMM yyyy"),
                            _ => order.Date.ToString("MMM yyyy")
                        };
                        
                        labels.Add(label);
                        data.Add(order.Revenue);
                    }
                }
                else
                {
                    // Return default data if no orders
                    labels.Add("No Data");
                    data.Add(0);
                }

                return new SalesChartData
                {
                    Labels = labels,
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales chart data for period: {Period}", period);
                
                // Return default data on error
                return new SalesChartData
                {
                    Labels = new List<string> { "Error" },
                    Data = new List<decimal> { 0 }
                };
            }
        }

        public async Task<List<RecentOrderDto>> GetRecentOrdersAsync(int count = 10)
        {
            return await _repository.GetRecentOrdersAsync(count);
        }

        public async Task<List<TopProductDto>> GetTopProductsAsync(int count = 8)
        {
            return await _repository.GetTopProductsAsync(count);
        }

        public async Task<List<LowStockProductDto>> GetLowStockProductsAsync()
        {
            return await _repository.GetLowStockProductsAsync();
        }

        public async Task<OrderStatusSummary> GetOrderStatusSummaryAsync()
        {
            return await _repository.GetOrderStatusSummaryAsync();
        }

        public async Task<List<MonthlyRevenueDto>> GetMonthlyRevenueDataAsync(int months = 12)
        {
            return await _repository.GetMonthlyRevenueDataAsync(months);
        }

        public async Task<decimal> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate)
        {
            return await _repository.GetMonthlyRevenueAsync(); // Simplified
        }

        public async Task<int> GetNewCustomersCountAsync(DateTime startDate, DateTime endDate)
        {
            return 0; // Implement if needed
        }

        public async Task RefreshDashboardCacheAsync()
        {
            _cache.Remove(DASHBOARD_CACHE_KEY);
            await GetDashboardDataAsync();
        }
    }
}