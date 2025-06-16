using web_lab_4.Models;
using web_lab_4.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace web_lab_4.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService? _emailService;
        private readonly UserManager<IdentityUser>? _userManager;

        // Primary constructor with all dependencies
        public OrderService(
            ApplicationDbContext context, 
            IEmailService emailService,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _emailService = emailService;
            _userManager = userManager;
        }

        // Backward compatibility constructor - TẠM THỜI
        public OrderService(ApplicationDbContext context)
        {
            _context = context;
            _emailService = null;
            _userManager = null;
        }

        public async Task<Order> GetOrderByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<IEnumerable<Order>> GetOrdersByUserIdAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> GetOrderWithDetailsAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<bool> ValidateOrderOwnershipAsync(int orderId, string userId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            return order?.UserId == userId;
        }

        public async Task UpdateOrderStatusAsync(int orderId, string status)
        {
            // Validate status
            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Completed", "Delivered", "Cancelled" };
            
            if (!validStatuses.Contains(status))
            {
                throw new ArgumentException($"Invalid order status: {status}. Valid statuses are: {string.Join(", ", validStatuses)}");
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                throw new ArgumentException($"Order with ID {orderId} not found");
            }

            var oldStatus = order.Status; // Capture the old status before updating

            order.Status = status;
            await _context.SaveChangesAsync();

            // Send status update email - CHỈ KHI CÓ EMAIL SERVICE
            if (_emailService != null && _userManager != null)
            {
                try
                {
                    var user = await _userManager.FindByIdAsync(order.UserId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        await _emailService.SendOrderStatusUpdateEmailAsync(
                            user.Email,
                            user.UserName ?? "Customer",
                            order,
                            oldStatus);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send status update email: {ex.Message}");
                }
            }
        }

        public async Task<IEnumerable<Order>> GetAllOrdersAsync()
        {
            return await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalRevenueAsync()
        {
            return await _context.Orders
                .Where(o => o.Status != "Cancelled")
                .SumAsync(o => o.TotalPrice);
        }

        public async Task<int> GetTotalOrdersCountAsync()
        {
            return await _context.Orders.CountAsync();
        }
    }
}