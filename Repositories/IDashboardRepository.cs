using web_lab_4.Models.Dashboard;

namespace web_lab_4.Repositories
{
    public interface IDashboardRepository
    {
        Task<int> GetTotalProductsAsync();
        Task<int> GetTotalOrdersAsync();
        Task<int> GetPendingOrdersAsync();
        Task<decimal> GetMonthlyRevenueAsync();
        Task<decimal> GetTotalRevenueAsync();
        Task<int> GetLowStockCountAsync();
        Task<int> GetExpiredProductsCountAsync();
        Task<List<RecentOrderDto>> GetRecentOrdersAsync(int count);
        Task<List<TopProductDto>> GetTopProductsAsync(int count);
        Task<List<LowStockProductDto>> GetLowStockProductsAsync();
        Task<OrderStatusSummary> GetOrderStatusSummaryAsync();
        Task<List<MonthlyRevenueDto>> GetMonthlyRevenueDataAsync(int months);
    }
}