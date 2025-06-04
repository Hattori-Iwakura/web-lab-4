using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_lab_4.Data;

namespace web_lab_4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
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
                var totalProducts = await _context.Products.CountAsync();
                var totalOrders = await _context.Orders.CountAsync();
                var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
                var monthlyRevenue = await _context.Orders
                    .Where(o => o.OrderDate.Month == DateTime.Now.Month && o.OrderDate.Year == DateTime.Now.Year)
                    .SumAsync(o => o.TotalPrice);

                var recentOrders = await _context.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new {
                        id = o.Id,
                        customerName = o.UserId, // You might want to join with Users table for actual name
                        amount = o.TotalPrice,
                        status = o.Status,
                        date = o.OrderDate
                    })
                    .ToListAsync();

                // Top selling products (this requires OrderDetails)
                var topProducts = await _context.OrderDetails
                    .Include(od => od.Product)
                    .GroupBy(od => od.Product.Name)
                    .Select(g => new {
                        name = g.Key,
                        salesCount = g.Sum(od => od.Quantity),
                        revenue = g.Sum(od => od.Price * od.Quantity)
                    })
                    .OrderByDescending(p => p.salesCount)
                    .Take(4)
                    .ToListAsync();

                return Json(new {
                    totalProducts,
                    totalOrders,
                    pendingOrders,
                    monthlyRevenue,
                    recentOrders,
                    topProducts
                });
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message });
            }
        }
    }
}