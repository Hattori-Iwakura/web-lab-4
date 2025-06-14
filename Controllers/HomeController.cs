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
        private readonly IReviewService _reviewService; // Assuming you have this service for reviews

        public HomeController(ILogger<HomeController> logger, IProductService productService, IReviewService reviewService)
        {
            _logger = logger;
            _productService = productService;
            _reviewService = reviewService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var featuredProducts = await _productService.GetFeaturedProductsAsync(8);
                
                // Load review stats for each product
                foreach (var product in featuredProducts)
                {
                    if (_reviewService != null)
                    {
                        try
                        {
                            var stats = await _reviewService.GetProductRatingStatsAsync(product.Id);
                            ViewData[$"ReviewCount_{product.Id}"] = stats.reviewCount;
                            ViewData[$"AverageRating_{product.Id}"] = stats.averageRating;
                            ViewData[$"DisplayRating_{product.Id}"] = stats.averageRating > 0 
                                ? stats.averageRating.ToString("F1") 
                                : "No rating";
                        }
                        catch
                        {
                            ViewData[$"ReviewCount_{product.Id}"] = 0;
                            ViewData[$"AverageRating_{product.Id}"] = 0.0;
                            ViewData[$"DisplayRating_{product.Id}"] = "No rating";
                        }
                    }
                }
                
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