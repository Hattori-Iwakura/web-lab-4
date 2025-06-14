using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_lab_4.Services;
using web_lab_4.Models.Dashboard;

namespace web_lab_4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class HomeController : Controller
    {
        private readonly IDashboardService _dashboardService;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IDashboardService dashboardService, ILogger<HomeController> logger)
        {
            _dashboardService = dashboardService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("\n\n\n\n\n\n\nLoading Dashboard Index\n\n\n\n\n\n\n");
            try
            {
                var dashboardData = await _dashboardService.GetDashboardDataAsync();
                return View(dashboardData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["ErrorMessage"] = "Error loading dashboard data.";
                return View(new DashboardViewModel());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var data = await _dashboardService.GetDashboardDataAsync();
                
                return Json(new {
                    success = true,
                    totalProducts = data.TotalProducts,
                    totalOrders = data.TotalOrders,
                    pendingOrders = data.PendingOrders,
                    monthlyRevenue = data.MonthlyRevenue,
                    totalRevenue = data.TotalRevenue,
                    lowStockCount = data.LowStockCount,
                    expiredProductsCount = data.ExpiredProductsCount,
                    newCustomersThisMonth = data.NewCustomersThisMonth,
                    recentOrders = data.RecentOrders.Select(o => new {
                        id = o.Id,
                        customerName = o.CustomerName,
                        amount = o.Amount,
                        status = o.Status,
                        date = o.Date.ToString("yyyy-MM-dd"),
                        itemCount = o.ItemCount
                    }),
                    topProducts = data.TopProducts.Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        salesCount = p.SalesCount,
                        revenue = p.Revenue,
                        category = p.Category
                    }),
                    lowStockProducts = data.LowStockProducts.Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        stock = p.Stock,
                        isExpired = p.IsExpired,
                        category = p.Category
                    }),
                    orderStatusSummary = data.OrderStatusSummary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard stats");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetSalesChart(string period = "year")
        {
            try
            {
                var chartData = await _dashboardService.GetSalesChartDataAsync(period);
                return Json(new { 
                    success = true, 
                    labels = chartData.Labels,
                    data = chartData.Data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sales chart data");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetRecentOrders(int count = 10)
        {
            try
            {
                var orders = await _dashboardService.GetRecentOrdersAsync(count);
                return Json(new { success = true, data = orders });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetTopProducts(int count = 8)
        {
            try
            {
                var products = await _dashboardService.GetTopProductsAsync(count);
                return Json(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting top products");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLowStockProducts()
        {
            try
            {
                var products = await _dashboardService.GetLowStockProductsAsync();
                return Json(new { success = true, data = products });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting low stock products");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOrderStatusSummary()
        {
            try
            {
                var summary = await _dashboardService.GetOrderStatusSummaryAsync();
                return Json(new { success = true, data = summary });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status summary");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMonthlyRevenue(int months = 12)
        {
            try
            {
                var data = await _dashboardService.GetMonthlyRevenueDataAsync(months);
                return Json(new { success = true, data = data });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting monthly revenue data");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RefreshDashboard()
        {
            try
            {
                await _dashboardService.RefreshDashboardCacheAsync();
                return Json(new { success = true, message = "Dashboard refreshed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing dashboard");
                return Json(new { success = false, error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ExportReport(string type = "monthly", string format = "json")
        {
            try
            {
                var data = await _dashboardService.GetDashboardDataAsync();
                
                if (format.ToLower() == "csv")
                {
                    var csv = GenerateCSVReport(data, type);
                    return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"report-{type}-{DateTime.Now:yyyy-MM-dd}.csv");
                }
                
                return Json(new { success = true, data = data, type = type, generatedAt = DateTime.Now });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report");
                return Json(new { success = false, error = ex.Message });
            }
        }

        private string GenerateCSVReport(DashboardViewModel data, string type)
        {
            var csv = new System.Text.StringBuilder();
            
            switch (type.ToLower())
            {
                case "products":
                    csv.AppendLine("Product Name,Sales Count,Revenue,Category");
                    foreach (var product in data.TopProducts)
                    {
                        csv.AppendLine($"{product.Name},{product.SalesCount},{product.Revenue:C},{product.Category}");
                    }
                    break;
                    
                case "orders":
                    csv.AppendLine("Order ID,Customer,Amount,Status,Date");
                    foreach (var order in data.RecentOrders)
                    {
                        csv.AppendLine($"{order.Id},{order.CustomerName},{order.Amount:C},{order.Status},{order.Date:yyyy-MM-dd}");
                    }
                    break;
                    
                case "revenue":
                default:
                    csv.AppendLine("Month,Revenue,Order Count,Average Order Value");
                    foreach (var revenue in data.MonthlyRevenueData)
                    {
                        csv.AppendLine($"{revenue.Month},{revenue.Revenue:C},{revenue.OrderCount},{revenue.AverageOrderValue:C}");
                    }
                    break;
            }
            
            return csv.ToString();
        }
    }
}