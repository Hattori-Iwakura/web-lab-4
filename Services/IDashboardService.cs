using web_lab_4.Models.Dashboard;

namespace web_lab_4.Services
{
    public interface IDashboardService
    {
        Task<DashboardViewModel> GetDashboardDataAsync();
        Task<SalesChartData> GetSalesChartDataAsync(string period = "year");
        Task<List<RecentOrderDto>> GetRecentOrdersAsync(int count = 10);
        Task<List<TopProductDto>> GetTopProductsAsync(int count = 8);
        Task<List<LowStockProductDto>> GetLowStockProductsAsync();
        Task<OrderStatusSummary> GetOrderStatusSummaryAsync();
        Task<List<MonthlyRevenueDto>> GetMonthlyRevenueDataAsync(int months = 12);
        Task<decimal> GetRevenueByPeriodAsync(DateTime startDate, DateTime endDate);
        Task<int> GetNewCustomersCountAsync(DateTime startDate, DateTime endDate);
        Task RefreshDashboardCacheAsync();
    }
}