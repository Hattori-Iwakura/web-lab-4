using web_lab_4.Models;
using web_lab_4.Repositories;
using web_lab_4.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace web_lab_4.Services
{
    public class ShoppingCartService : IShoppingCartService
    {
        private readonly ApplicationDbContext _context;
        private readonly IProductRepository _productRepository;
        private readonly IProductService _productService;
        private const string CART_KEY = "Cart";

        public ShoppingCartService(
            ApplicationDbContext context, 
            IProductRepository productRepository,
            IProductService productService)
        {
            _context = context;
            _productRepository = productRepository;
            _productService = productService;
        }

        public ShoppingCart GetCart(HttpContext httpContext)
        {
            return httpContext.Session.GetObjectFromJson<ShoppingCart>(CART_KEY) ?? new ShoppingCart();
        }

        public async Task AddToCartAsync(HttpContext httpContext, int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) 
                throw new ArgumentException($"Product with ID {productId} not found");

            // Check stock availability
            if (!await _productService.CheckStockAvailabilityAsync(productId, quantity))
                throw new InvalidOperationException("Insufficient stock or product not available");

            // Check if product is expired
            if (product.IsExpired)
                throw new InvalidOperationException("Product has expired");

            var cart = GetCart(httpContext);
            
            // Check if adding this quantity would exceed available stock
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            var totalQuantity = quantity + (existingItem?.Quantity ?? 0);
            
            if (!await _productService.CheckStockAvailabilityAsync(productId, totalQuantity))
                throw new InvalidOperationException("Insufficient stock for requested quantity");

            cart.AddItem(product, quantity);
            httpContext.Session.SetObjectAsJson(CART_KEY, cart);
        }

        public void UpdateQuantity(HttpContext httpContext, int productId, int quantity)
        {
            var cart = GetCart(httpContext);
            if (quantity <= 0)
            {
                cart.RemoveItem(productId);
            }
            else
            {
                cart.UpdateQuantity(productId, quantity);
            }
            httpContext.Session.SetObjectAsJson(CART_KEY, cart);
        }

        public void RemoveFromCart(HttpContext httpContext, int productId)
        {
            var cart = GetCart(httpContext);
            cart.RemoveItem(productId);
            httpContext.Session.SetObjectAsJson(CART_KEY, cart);
        }

        public void ClearCart(HttpContext httpContext)
        {
            httpContext.Session.Remove(CART_KEY);
        }

        public int GetCartCount(HttpContext httpContext)
        {
            var cart = GetCart(httpContext);
            return cart?.Items.Sum(i => i.Quantity) ?? 0;
        }

        public async Task<bool> ValidateCartItemsAsync(ShoppingCart cart)
        {
            foreach (var item in cart.Items)
            {
                if (!await _productService.CheckStockAvailabilityAsync(item.ProductId, item.Quantity))
                {
                    return false;
                }
            }
            return true;
        }

        public async Task<Order> ProcessCheckoutAsync(HttpContext httpContext, Order order, IdentityUser user)
        {
            var cart = GetCart(httpContext);
            if (cart == null || !cart.Items.Any())
                throw new InvalidOperationException("Cart is empty");

            // Validate all cart items
            if (!await ValidateCartItemsAsync(cart))
                throw new InvalidOperationException("Some items in your cart are no longer available or out of stock");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                order.UserId = user.Id;
                order.OrderDate = DateTime.UtcNow;
                order.Status = "Pending";
                order.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);

                var orderDetails = new List<OrderDetail>();
                foreach (var item in cart.Items)
                {
                    // Double-check stock before processing
                    if (!await _productService.CheckStockAvailabilityAsync(item.ProductId, item.Quantity))
                        throw new InvalidOperationException($"Insufficient stock for {item.ProductName}");

                    orderDetails.Add(new OrderDetail
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Weight = item.Weight,
                        WeightUnit = item.WeightUnit,
                        Flavor = item.Flavor
                    });

                    // Update stock
                    await _productService.UpdateStockAsync(item.ProductId, item.Quantity);
                }
                order.OrderDetails = orderDetails;

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                ClearCart(httpContext);
                return order;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}