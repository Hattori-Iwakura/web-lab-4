using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web_lab_4.Data;
using web_lab_4.Models; // Add this line


namespace web_lab_4.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private const string CART_KEY = "Cart";

        public ShoppingCartController(ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Display shopping cart
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_KEY) ?? new ShoppingCart();
            return View(cart);
        }

        // Add product to cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                // Check if product is available and in stock
                if (!product.IsAvailable || !product.IsInStock)
                {
                    return Json(new { success = false, message = "Product is not available" });
                }

                var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_KEY) ?? new ShoppingCart();
                cart.AddItem(product, quantity);
                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);

                // Return JSON for AJAX calls
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { 
                        success = true, 
                        message = $"{product.Name} has been added to your cart!",
                        cartCount = cart.Items.Sum(i => i.Quantity)
                    });
                }

                // Return redirect for normal form submissions
                TempData["SuccessMessage"] = $"{product.Name} has been added to your cart!";
                return RedirectToAction("Index", "Product");
            }
            catch (Exception ex)
            {
                // Log the error
                Console.WriteLine($"Error adding product to cart: {ex.Message}");
                
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "An error occurred while adding the product to cart" });
                }
                
                TempData["ErrorMessage"] = "An error occurred while adding the product to cart";
                return RedirectToAction("Index", "Product");
            }
        }
        // Update item quantity in cart
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_KEY);
            if (cart != null)
            {
                if (quantity <= 0)
                {
                    cart.RemoveItem(productId);
                }
                else
                {
                    cart.UpdateQuantity(productId, quantity);
                }
                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
            }
            return RedirectToAction("Index");
        }

        // Remove item from cart
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_KEY);
            if (cart != null)
            {
                cart.RemoveItem(productId);
                HttpContext.Session.SetObjectAsJson(CART_KEY, cart);
            }
            return RedirectToAction("Index");
        }

        // Clear entire cart
        [HttpPost]
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove(CART_KEY);
            return RedirectToAction("Index");
        }

        // Get cart count for navbar
        public IActionResult GetCartCount()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_KEY);
            var count = cart?.Items.Sum(i => i.Quantity) ?? 0;
            return Json(new { count });
        }

        // Show checkout form
        [Authorize]
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_KEY);
            if (cart == null || !cart.Items.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty. Please add some products before checkout.";
                return RedirectToAction("Index");
            }

            var order = new Order
            {
                OrderDate = DateTime.UtcNow,
                TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity)
            };

            ViewBag.Cart = cart;
            return View(order);
        }

        // Process checkout
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(Order order)
        {
            try
            {
                var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_KEY);
                if (cart == null || !cart.Items.Any())
                {
                    TempData["ErrorMessage"] = "Your cart is empty. Please add some products before checkout.";
                    return RedirectToAction("Index");
                }

                // Get current user
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Please log in to complete your order.";
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                // Set UserId and clear its validation error
                order.UserId = user.Id;
                ModelState.Remove("UserId"); // Clear validation error for UserId
                
                // Set other properties that aren't from the form
                order.OrderDate = DateTime.UtcNow;
                order.Status = "Pending";
                order.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);

                // Debug: Log what we're receiving
                System.Console.WriteLine($"ModelState.IsValid: {ModelState.IsValid}");
                System.Console.WriteLine($"ShippingAddress: '{order.ShippingAddress}'");
                System.Console.WriteLine($"UserId: '{order.UserId}'");

                if (ModelState.IsValid)
                {
                    // Create order details
                    var orderDetails = new List<OrderDetail>();
                    foreach (var item in cart.Items)
                    {
                        orderDetails.Add(new OrderDetail
                        {
                            ProductId = item.ProductId,
                            Quantity = item.Quantity,
                            Price = item.Price
                        });
                    }
                    order.OrderDetails = orderDetails;

                    // Save order to database
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    // Clear cart after successful order
                    HttpContext.Session.Remove(CART_KEY);

                    TempData["SuccessMessage"] = "Your order has been placed successfully!";
                    return View("OrderCompleted", order);
                }
                else
                {
                    // Debug: Show validation errors
                    foreach (var modelError in ModelState.Values.SelectMany(v => v.Errors))
                    {
                        System.Console.WriteLine($"Validation Error: {modelError.ErrorMessage}");
                    }
                    
                    TempData["ErrorMessage"] = "Please correct the errors below.";
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Checkout Error: {ex.Message}");
                TempData["ErrorMessage"] = "An error occurred while processing your order. Please try again.";
            }

            // If we got this far, something failed, redisplay form
            ViewBag.Cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>(CART_KEY);
            return View(order);
        }
        // Order completion confirmation page
        [Authorize]
        public async Task<IActionResult> OrderCompleted(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Verify the order belongs to the current user
            var user = await _userManager.GetUserAsync(User);
            if (order.UserId != user.Id)
            {
                return Forbid();
            }

            return View(order);
        }

        // Show user's order history
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            var user = await _userManager.GetUserAsync(User);
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // View order details
        [Authorize]
        public async Task<IActionResult> OrderDetail(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            // Verify the order belongs to the current user
            var user = await _userManager.GetUserAsync(User);
            if (order.UserId != user?.Id)
            {
                return Forbid();
            }

            return View(order);
        }
    }
}

