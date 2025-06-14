using web_lab_4.Models;
using Microsoft.AspNetCore.Identity;

namespace web_lab_4.Services
{
    public interface IShoppingCartService
    {
        ShoppingCart GetCart(HttpContext httpContext);
        Task AddToCartAsync(HttpContext httpContext, int productId, int quantity);
        void UpdateQuantity(HttpContext httpContext, int productId, int quantity);
        void RemoveFromCart(HttpContext httpContext, int productId);
        void ClearCart(HttpContext httpContext);
        int GetCartCount(HttpContext httpContext);
        Task<Order> ProcessCheckoutAsync(HttpContext httpContext, Order order, IdentityUser user);
        Task<bool> ValidateCartItemsAsync(ShoppingCart cart);
    }
}