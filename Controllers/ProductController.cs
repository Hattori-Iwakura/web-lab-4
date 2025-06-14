using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using web_lab_4.Models;
using web_lab_4.Services;

namespace web_lab_4.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IReviewService _reviewService; // Thêm này

        public ProductController(
            IProductService productService, 
            ICategoryService categoryService,
            IReviewService reviewService) // Thêm này
        {
            _productService = productService;
            _categoryService = categoryService;
            _reviewService = reviewService; // Thêm này
        }

        public async Task<IActionResult> Index(
            string searchTerm = "",
            int? categoryId = null,
            string brand = "",
            string flavor = "",
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string sortBy = "",
            int page = 1)
        {
            const int pageSize = 12;

            try
            {
                // Get all products WITH REVIEWS
                var allProducts = await _productService.GetAllProductsWithReviewsAsync(); // Cần method này
                var products = allProducts.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    products = products.Where(p => 
                        p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        (p.Description != null && p.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Brand != null && p.Brand.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)) ||
                        (p.Ingredients != null && p.Ingredients.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                    );
                }

                if (categoryId.HasValue)
                {
                    products = products.Where(p => p.CategoryId == categoryId.Value);
                }

                if (!string.IsNullOrEmpty(brand))
                {
                    products = products.Where(p => p.Brand == brand);
                }

                if (!string.IsNullOrEmpty(flavor))
                {
                    products = products.Where(p => p.Flavor == flavor);
                }

                if (minPrice.HasValue)
                {
                    products = products.Where(p => p.Price >= minPrice.Value);
                }

                if (maxPrice.HasValue)
                {
                    products = products.Where(p => p.Price <= maxPrice.Value);
                }

                // Apply sorting
                products = sortBy switch
                {
                    "name_asc" => products.OrderBy(p => p.Name),
                    "name_desc" => products.OrderByDescending(p => p.Name),
                    "price_asc" => products.OrderBy(p => p.Price),
                    "price_desc" => products.OrderByDescending(p => p.Price),
                    "newest" => products.OrderByDescending(p => p.Id),
                    "stock_asc" => products.OrderBy(p => p.StockQuantity),
                    "brand_asc" => products.OrderBy(p => p.Brand),
                    "expiry_asc" => products.OrderBy(p => p.ExpiryDate ?? DateTime.MaxValue),
                    _ => products.OrderBy(p => p.Name)
                };

                var totalProducts = products.Count();
                var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

                // Ensure page is within valid range
                if (page < 1) page = 1;
                if (page > totalPages && totalPages > 0) page = totalPages;

                // Apply pagination
                var pagedProducts = products
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                // Load review stats for each product
                foreach (var product in pagedProducts)
                {
                    if (_reviewService != null)
                    {
                        try
                        {
                            var stats = await _reviewService.GetProductRatingStatsAsync(product.Id);
                            // Store stats in ViewData for this specific product
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

                // Get filter data from all products (not filtered)
                var categories = await _categoryService.GetAllCategoriesAsync();
                var brands = allProducts.Where(p => !string.IsNullOrEmpty(p.Brand))
                                      .Select(p => p.Brand)
                                      .Distinct()
                                      .OrderBy(b => b)
                                      .ToList();
                var flavors = allProducts.Where(p => !string.IsNullOrEmpty(p.Flavor))
                                       .Select(p => p.Flavor)
                                       .Distinct()
                                       .OrderBy(f => f)
                                       .ToList();

                // Set ViewBag data
                ViewBag.Categories = categories;
                ViewBag.Brands = brands;
                ViewBag.Flavors = flavors;
                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.SearchTerm = searchTerm;
                ViewBag.SelectedCategory = categoryId?.ToString();
                ViewBag.Brand = brand;
                ViewBag.Flavor = flavor;
                ViewBag.MinPrice = minPrice?.ToString();
                ViewBag.MaxPrice = maxPrice?.ToString();
                ViewBag.SortBy = sortBy;

                return View(pagedProducts);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading products: " + ex.Message;
                return View(new List<Product>());
            }
        }

        public async Task<IActionResult> Add()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                
                // Create ViewBag data for form
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                ViewBag.WeightUnits = new SelectList(new[] { "g", "kg", "oz", "lb", "ml", "l" });
                
                return View();
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading form data: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
        {
            try
            {
                // Remove ImageUrl from ModelState since it's handled by file upload
                ModelState.Remove("ImageUrl");
                
                if (ModelState.IsValid)
                {
                    await _productService.AddProductAsync(product, imageUrl);
                    TempData["SuccessMessage"] = "Product added successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding product: " + ex.Message;
            }

            // Reload form data on error
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            ViewBag.WeightUnits = new SelectList(new[] { "g", "kg", "oz", "lb", "ml", "l" }, product.WeightUnit);
            
            return View(product);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Load category
            product.Category = await _categoryService.GetCategoryByIdAsync(product.CategoryId);

            // Load review statistics
            if (_reviewService != null)
            {
                try
                {
                    var reviewStats = await _reviewService.GetProductRatingStatsAsync(id);
                    ViewBag.ReviewCount = reviewStats.reviewCount;
                    ViewBag.AverageRating = reviewStats.averageRating;
                    ViewBag.DisplayRating = reviewStats.averageRating > 0 
                        ? reviewStats.averageRating.ToString("F1") 
                        : "No rating";
                }
                catch (Exception ex)
                {
                    // Log error and set defaults
                    Console.WriteLine($"Error loading review stats: {ex.Message}");
                    ViewBag.ReviewCount = 0;
                    ViewBag.AverageRating = 0;
                    ViewBag.DisplayRating = "No rating";
                }
            }
            else
            {
                // Default values if ReviewService is not available
                ViewBag.ReviewCount = 0;
                ViewBag.AverageRating = 0;
                ViewBag.DisplayRating = "No rating";
            }

            // Load related products (optional)
            try
            {
                var relatedProducts = await _productService.GetProductsByCategoryAsync(product.CategoryId);
                ViewBag.RelatedProducts = relatedProducts?.Where(p => p.Id != id).Take(4).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading related products: {ex.Message}");
                ViewBag.RelatedProducts = new List<Product>();
            }

            return View(product);
        }

        public async Task<IActionResult> Update(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null) return NotFound();

                var categories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
                ViewBag.WeightUnits = new SelectList(new[] { "g", "kg", "oz", "lb", "ml", "l" }, product.WeightUnit);
                
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading product: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl)
        {
            try
            {
                ModelState.Remove("ImageUrl");
                if (id != product.Id) return NotFound();

                if (ModelState.IsValid)
                {
                    await _productService.UpdateProductAsync(id, product, imageUrl);
                    TempData["SuccessMessage"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating product: " + ex.Message;
            }

            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            ViewBag.WeightUnits = new SelectList(new[] { "g", "kg", "oz", "lb", "ml", "l" }, product.WeightUnit);
            
            return View(product);
        }

        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null) return NotFound();
                
                // Get category for display
                ViewBag.Category = await _categoryService.GetCategoryByIdAsync(product.CategoryId);
                
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading product: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _productService.DeleteProductAsync(id);
                TempData["SuccessMessage"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting product: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }

        // API endpoints for AJAX calls
        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                product.IsAvailable = !product.IsAvailable;
                await _productService.UpdateProductAsync(id, product, null);

                return Json(new { success = true, isAvailable = product.IsAvailable });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStock(int id, int quantity)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Product not found" });
                }

                product.StockQuantity = quantity;
                await _productService.UpdateProductAsync(id, product, null);

                return Json(new { success = true, stockQuantity = product.StockQuantity });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Get products by category for filtering
        [HttpGet]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            try
            {
                var products = await _productService.GetProductsByCategoryAsync(categoryId);
                var productList = products.Select(p => new {
                    id = p.Id,
                    name = p.Name,
                    price = p.Price,
                    imageUrl = p.ImageUrl,
                    isAvailable = p.IsAvailable,
                    isInStock = p.IsInStock
                }).ToList();

                return Json(new { success = true, products = productList });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Search products with autocomplete
        [HttpGet]
        public async Task<IActionResult> SearchProducts(string term)
        {
            try
            {
                if (string.IsNullOrEmpty(term) || term.Length < 2)
                {
                    return Json(new List<object>());
                }

                var products = await _productService.GetAllProductsAsync();
                var results = products
                    .Where(p => p.Name.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                               (p.Brand != null && p.Brand.Contains(term, StringComparison.OrdinalIgnoreCase)))
                    .Take(10)
                    .Select(p => new {
                        id = p.Id,
                        name = p.Name,
                        brand = p.Brand,
                        price = p.Price.ToString("C"),
                        imageUrl = p.ImageUrl
                    })
                    .ToList();

                return Json(results);
            }
            catch (Exception ex)
            {
                return Json(new List<object>());
            }
        }
    }
}