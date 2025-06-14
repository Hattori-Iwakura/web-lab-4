using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web_lab_4.Services;

namespace web_lab_4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        // Display order list
        public async Task<IActionResult> Index(string status = "", DateTime? startDate = null, DateTime? endDate = null, string searchTerm = "")
        {
            try
            {
                var orders = await _orderService.GetAllOrdersAsync();

                // Apply filters
                if (!string.IsNullOrEmpty(status))
                {
                    orders = orders.Where(o => o.Status == status);
                }

                if (startDate.HasValue)
                {
                    orders = orders.Where(o => o.OrderDate >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    orders = orders.Where(o => o.OrderDate <= endDate.Value.AddDays(1));
                }

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    orders = orders.Where(o => 
                        o.Id.ToString().Contains(searchTerm) ||
                        o.ShippingAddress.Contains(searchTerm) ||
                        o.UserId.Contains(searchTerm));
                }

                var ordersList = orders.OrderByDescending(o => o.OrderDate).ToList();

                // Statistics for dashboard
                ViewBag.TotalOrders = await _orderService.GetTotalOrdersCountAsync();
                ViewBag.PendingOrders = orders.Count(o => o.Status == "Pending");
                ViewBag.CompletedOrders = orders.Count(o => o.Status == "Completed");
                ViewBag.TotalRevenue = await _orderService.GetTotalRevenueAsync();
                
                ViewBag.CurrentStatus = status;
                ViewBag.StartDate = startDate;
                ViewBag.EndDate = endDate;
                ViewBag.SearchTerm = searchTerm;

                return View(ordersList);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading orders: " + ex.Message;
                return View(new List<web_lab_4.Models.Order>());
            }
        }

        // Display order details
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var order = await _orderService.GetOrderWithDetailsAsync(id);
                if (order == null) return NotFound();

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading order details: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Update order status
        [HttpPost]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            try
            {
                await _orderService.UpdateOrderStatusAsync(id, status);
                TempData["SuccessMessage"] = $"Order #{id} status updated to {status}";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating order status: " + ex.Message;
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // Delete order
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var order = await _orderService.GetOrderWithDetailsAsync(id);
                if (order == null) return NotFound();

                // Note: You might want to add a DeleteOrderAsync method to OrderService
                // For now, we'll just update status to "Cancelled"
                await _orderService.UpdateOrderStatusAsync(id, "Cancelled");
                
                TempData["SuccessMessage"] = $"Order #{id} has been cancelled";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error cancelling order: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}