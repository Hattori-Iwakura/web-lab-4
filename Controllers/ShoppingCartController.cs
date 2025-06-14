using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web_lab_4.Models;
using web_lab_4.Services;

namespace web_lab_4.Controllers
{
    public class ShoppingCartController : Controller
    {
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IOrderService _orderService;
        private readonly UserManager<IdentityUser> _userManager;

        public ShoppingCartController(
            IShoppingCartService shoppingCartService,
            IOrderService orderService,
            UserManager<IdentityUser> userManager)
        {
            _shoppingCartService = shoppingCartService;
            _orderService = orderService;
            _userManager = userManager;
        }

        // Display shopping cart
        public IActionResult Index()
        {
            var cart = _shoppingCartService.GetCart(HttpContext);
            return View(cart);
        }

        // Add product to cart
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                await _shoppingCartService.AddToCartAsync(HttpContext, productId, quantity);
                TempData["SuccessMessage"] = "Product has been added to your cart!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Index", "Product");
        }

        // Update item quantity in cart
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            try
            {
                _shoppingCartService.UpdateQuantity(HttpContext, productId, quantity);
                TempData["SuccessMessage"] = "Cart updated successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        // Remove item from cart
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            try
            {
                _shoppingCartService.RemoveFromCart(HttpContext, productId);
                TempData["SuccessMessage"] = "Item removed from cart!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToAction("Index");
        }

        // Clear entire cart
        [HttpPost]
        public IActionResult ClearCart()
        {
            _shoppingCartService.ClearCart(HttpContext);
            TempData["SuccessMessage"] = "Cart cleared!";
            return RedirectToAction("Index");
        }

        // Get cart count for navbar
        public IActionResult GetCartCount()
        {
            var count = _shoppingCartService.GetCartCount(HttpContext);
            return Json(new { count });
        }

        // Show checkout form
        [Authorize]
        public IActionResult Checkout()
        {
            var cart = _shoppingCartService.GetCart(HttpContext);
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
                var user = await _userManager.GetUserAsync(User);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Please log in to complete your order.";
                    return RedirectToAction("Login", "Account", new { area = "Identity" });
                }

                ModelState.Remove("UserId");

                if (ModelState.IsValid)
                {
                    var processedOrder = await _shoppingCartService.ProcessCheckoutAsync(HttpContext, order, user);
                    TempData["SuccessMessage"] = "Your order has been placed successfully!";
                    return View("OrderCompleted", processedOrder);
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            ViewBag.Cart = _shoppingCartService.GetCart(HttpContext);
            return View(order);
        }

        // Order completion confirmation page
        [Authorize]
        public async Task<IActionResult> OrderCompleted(int id)
        {
            try
            {
                var order = await _orderService.GetOrderWithDetailsAsync(id);
                if (order == null) return NotFound();

                var user = await _userManager.GetUserAsync(User);
                if (!await _orderService.ValidateOrderOwnershipAsync(id, user.Id))
                    return Forbid();

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        // Show user's order history
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            try
            {
                var user = await _userManager.GetUserAsync(User);
                var orders = await _orderService.GetOrdersByUserIdAsync(user.Id);
                return View(orders);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return View(new List<Order>());
            }
        }

        // View order details
        [Authorize]
        public async Task<IActionResult> OrderDetail(int id)
        {
            try
            {
                var order = await _orderService.GetOrderWithDetailsAsync(id);
                if (order == null) return NotFound();

                var user = await _userManager.GetUserAsync(User);
                if (!await _orderService.ValidateOrderOwnershipAsync(id, user?.Id))
                    return Forbid();

                return View(order);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
                return RedirectToAction("OrderHistory");
            }
        }
    }
}

