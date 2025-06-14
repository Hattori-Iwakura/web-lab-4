using Microsoft.AspNetCore.Mvc;
using web_lab_4.Services;
using web_lab_4.Models;
using System.Diagnostics;

namespace web_lab_4.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductService _productService;

        public HomeController(ILogger<HomeController> logger, IProductService productService)
        {
            _logger = logger;
            _productService = productService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                // Get featured products (available products with good stock)
                var allProducts = await _productService.GetAllProductsAsync();
                var featuredProducts = allProducts
                    .Where(p => p.IsAvailable && p.IsInStock && !p.IsExpired)
                    .OrderByDescending(p => p.Id) // or any other criteria for "featured"
                    .Take(8) // Show up to 8 products
                    .ToList();

                return View(featuredProducts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading featured products");
                // Return empty list if error occurs
                return View(new List<Product>());
            }
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}