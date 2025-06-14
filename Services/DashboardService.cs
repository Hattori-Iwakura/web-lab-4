using Microsoft.Extensions.Caching.Memory;
using web_lab_4.Repositories;
using web_lab_4.Models.Dashboard;

namespace web_lab_4.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly IDashboardRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DashboardService> _logger;
        private const string DASHBOARD_CACHE_KEY = "dashboard_data";
        private readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(15);

        public DashboardService(IDashboardRepository repository, IMemoryCache cache, ILogger<DashboardService> logger)
        {
            _repository = repository;
            _cache = cache;
            _logger = logger;
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
                var monthlyData = await _repository.GetMonthlyRevenueDataAsync(12);
                return new SalesChartData
                {
                    Labels = monthlyData.Select(x => x.Month).ToList(),
                    Data = monthlyData.Select(x => x.Revenue).ToList(),
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales chart data");
                return new SalesChartData();
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