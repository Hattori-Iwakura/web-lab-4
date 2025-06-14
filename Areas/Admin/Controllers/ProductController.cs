using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using web_lab_4.Models;
using web_lab_4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using web_lab_4.Data;
using web_lab_4.Models.ViewModels;

namespace web_lab_4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IProductService productService, ICategoryService categoryService, ApplicationDbContext context, ILogger<ProductController> logger)
        {
            _productService = productService;
            _categoryService = categoryService;
            _context = context;
            _logger = logger;
        }

        // Hiển thị danh sách sản phẩm
        [HttpGet]
        public async Task<IActionResult> Index(ProductFilterModel filter, int page = 1, int pageSize = 25)
        {
            try
            {
                var viewModel = new ProductFilterViewModel
                {
                    Filter = filter ?? new ProductFilterModel(),
                    CurrentPage = page,
                    PageSize = pageSize
                };

                var query = _context.Products
                    .Include(p => p.Category)
                    .AsQueryable();

                // Apply filters
                if (!string.IsNullOrWhiteSpace(filter?.SearchTerm))
                {
                    var searchTerm = filter.SearchTerm.ToLower();
                    query = query.Where(p => 
                        p.Name.ToLower().Contains(searchTerm) || 
                        (p.Brand != null && p.Brand.ToLower().Contains(searchTerm)) ||
                        (p.Description != null && p.Description.ToLower().Contains(searchTerm)));
                }

                if (filter?.CategoryId.HasValue == true)
                {
                    query = query.Where(p => p.CategoryId == filter.CategoryId.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter?.Brand))
                {
                    query = query.Where(p => p.Brand == filter.Brand);
                }

                if (filter?.MinPrice.HasValue == true)
                {
                    query = query.Where(p => p.Price >= filter.MinPrice.Value);
                }

                if (filter?.MaxPrice.HasValue == true)
                {
                    query = query.Where(p => p.Price <= filter.MaxPrice.Value);
                }

                if (filter?.MinStock.HasValue == true)
                {
                    query = query.Where(p => p.StockQuantity >= filter.MinStock.Value);
                }

                if (filter?.MaxStock.HasValue == true)
                {
                    query = query.Where(p => p.StockQuantity <= filter.MaxStock.Value);
                }

                // Thay đổi logic cho bool thay vì bool?
                if (filter?.IsAvailable == true)
                {
                    query = query.Where(p => p.IsAvailable == true);
                }

                if (filter?.IsExpired == true)
                {
                    query = query.Where(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value < DateTime.Now);
                }

                if (filter?.IsLowStock == true)
                {
                    query = query.Where(p => p.StockQuantity <= 10);
                }

                if (filter?.ExpiryFromDate.HasValue == true)
                {
                    query = query.Where(p => p.ExpiryDate >= filter.ExpiryFromDate.Value);
                }

                if (filter?.ExpiryToDate.HasValue == true)
                {
                    query = query.Where(p => p.ExpiryDate <= filter.ExpiryToDate.Value);
                }

                // Apply sorting
                var sortBy = filter?.SortBy?.ToLower() ?? "name";
                var sortDirection = filter?.SortDirection ?? "asc";
                
                query = sortBy switch
                {
                    "name" => sortDirection == "desc" ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                    "price" => sortDirection == "desc" ? query.OrderByDescending(p => p.Price) : query.OrderBy(p => p.Price),
                    "stock" => sortDirection == "desc" ? query.OrderByDescending(p => p.StockQuantity) : query.OrderBy(p => p.StockQuantity),
                    "category" => sortDirection == "desc" ? query.OrderByDescending(p => p.Category.Name) : query.OrderBy(p => p.Category.Name),
                    _ => query.OrderBy(p => p.Name)
                };

                viewModel.TotalItems = await query.CountAsync();

                viewModel.Products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                viewModel.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
                viewModel.Brands = await _context.Products
                    .Where(p => !string.IsNullOrWhiteSpace(p.Brand))
                    .Select(p => p.Brand)
                    .Distinct()
                    .OrderBy(b => b)
                    .ToListAsync();

                var allProducts = await _context.Products.ToListAsync();
                viewModel.TotalProducts = allProducts.Count;
                viewModel.LowStockCount = allProducts.Count(p => p.StockQuantity <= 10);
                viewModel.ExpiredCount = allProducts.Count(p => p.ExpiryDate.HasValue && p.ExpiryDate.Value < DateTime.Now);
                viewModel.AvailableCount = allProducts.Count(p => p.IsAvailable && p.StockQuantity > 0);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading products");
                TempData["ErrorMessage"] = "Error loading products: " + ex.Message;
                return View(new ProductFilterViewModel());
            }
        }

        [HttpPost]
        public IActionResult ResetFilter()
        {
            return RedirectToAction("Index");
        }

        // Hiển thị form thêm sản phẩm mới
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.WeightUnits = new SelectList(new[] { "g", "kg", "ml", "l" });
            return View();
        }

        // Xử lý thêm sản phẩm mới
        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
        {
            try
            {
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

            var categories = await _categoryService.GetAllCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.WeightUnits = new SelectList(new[] { "g", "kg", "ml", "l" });
            return View(product);
        }

        // Hiển thị thông tin chi tiết sản phẩm
        public async Task<IActionResult> Display(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null) return NotFound();

                ViewBag.Category = await _categoryService.GetCategoryByIdAsync(product.CategoryId);
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading product: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Hiển thị form cập nhật sản phẩm
        public async Task<IActionResult> Update(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null) return NotFound();

                var categories = await _categoryService.GetAllCategoriesAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
                ViewBag.WeightUnits = new SelectList(new[] { "g", "kg", "ml", "l" }, product.WeightUnit);
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading product: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Xử lý cập nhật sản phẩm
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
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.WeightUnits = new SelectList(new[] { "g", "kg", "ml", "l" });
            return View(product);
        }

        // Hiển thị form xác nhận xóa sản phẩm
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null) return NotFound();
                return View(product);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading product: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // Xử lý xóa sản phẩm
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

        // Update stock quantity
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int id, int quantity)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null) return NotFound();

                product.StockQuantity = quantity;
                await _productService.UpdateProductAsync(id, product, null);
                
                TempData["SuccessMessage"] = "Stock updated successfully!";
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Toggle product availability
        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(int id)
        {
            try
            {
                var product = await _productService.GetProductByIdAsync(id);
                if (product == null) return NotFound();

                product.IsAvailable = !product.IsAvailable;
                await _productService.UpdateProductAsync(id, product, null);
                
                TempData["SuccessMessage"] = $"Product {(product.IsAvailable ? "enabled" : "disabled")} successfully!";
                return Json(new { success = true, isAvailable = product.IsAvailable });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}