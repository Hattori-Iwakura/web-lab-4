using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_lab_4.Services;

namespace web_lab_4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class HomeController : Controller
    {
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;

        public HomeController(IProductService productService, IOrderService orderService)
        {
            _productService = productService;
            _orderService = orderService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var products = await _productService.GetAllProductsAsync();
                var orders = await _orderService.GetAllOrdersAsync();
                
                var totalProducts = products.Count();
                var totalOrders = await _orderService.GetTotalOrdersCountAsync();
                var pendingOrders = orders.Count(o => o.Status == "Pending");
                var monthlyRevenue = orders
                    .Where(o => o.OrderDate.Month == DateTime.Now.Month && o.OrderDate.Year == DateTime.Now.Year)
                    .Sum(o => o.TotalPrice);

                var recentOrders = orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new {
                        id = o.Id,
                        customerName = o.UserId,
                        amount = o.TotalPrice,
                        status = o.Status,
                        date = o.OrderDate
                    })
                    .ToList();

                // Top selling products
                var topProducts = orders
                    .SelectMany(o => o.OrderDetails)
                    .GroupBy(od => od.Product.Name)
                    .Select(g => new {
                        name = g.Key,
                        salesCount = g.Sum(od => od.Quantity),
                        revenue = g.Sum(od => od.Price * od.Quantity)
                    })
                    .OrderByDescending(p => p.salesCount)
                    .Take(4)
                    .ToList();

                // Low stock products
                var lowStockProducts = products
                    .Where(p => p.StockQuantity <= 10)
                    .Select(p => new {
                        name = p.Name,
                        stock = p.StockQuantity,
                        isExpired = p.IsExpired
                    })
                    .ToList();

                return Json(new {
                    totalProducts,
                    totalOrders,
                    pendingOrders,
                    monthlyRevenue,
                    recentOrders,
                    topProducts,
                    lowStockProducts
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}