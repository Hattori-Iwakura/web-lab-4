using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_lab_4.Data;
using web_lab_4.Models;

namespace web_lab_4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Display order list
        public async Task<IActionResult> Index(string status = "", DateTime? startDate = null, DateTime? endDate = null, string searchTerm = "")
        {
            var ordersQuery = _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .AsQueryable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == status);
            }

            // Filter by date range
            if (startDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= endDate.Value.AddDays(1));
            }

            // Search by order ID or customer info
            if (!string.IsNullOrEmpty(searchTerm))
            {
                ordersQuery = ordersQuery.Where(o => 
                    o.Id.ToString().Contains(searchTerm) ||
                    o.ShippingAddress.Contains(searchTerm) ||
                    o.UserId.Contains(searchTerm));
            }

            var orders = await ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            // Statistics for dashboard
            ViewBag.TotalOrders = await _context.Orders.CountAsync();
            ViewBag.PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            ViewBag.CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Completed");
            ViewBag.TotalRevenue = await _context.Orders.Where(o => o.Status == "Completed").SumAsync(o => o.TotalPrice);
            
            ViewBag.CurrentStatus = status;
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            ViewBag.SearchTerm = searchTerm;

            return View(orders);
        }

        // Display order details
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // Update order status
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{id} status updated to {status}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // Delete order
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            _context.OrderDetails.RemoveRange(order.OrderDetails);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Order #{id} has been deleted successfully";
            return RedirectToAction(nameof(Index));
        }

        // Export orders to CSV
        public async Task<IActionResult> ExportOrders()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var csv = new System.Text.StringBuilder();
            csv.AppendLine("Order ID,Order Date,Customer ID,Status,Total Amount,Items Count,Shipping Address");

            foreach (var order in orders)
            {
                csv.AppendLine($"{order.Id},{order.OrderDate:yyyy-MM-dd HH:mm},{order.UserId},{order.Status},{order.TotalPrice:F2},{order.OrderDetails.Count()},\"{order.ShippingAddress}\"");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"orders_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}