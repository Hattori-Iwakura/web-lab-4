using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using web_lab_4.Models;
using web_lab_4.Services;

namespace web_lab_4.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Policy = "AdminOnly")]
    public class CategoryController : Controller
    {
        private readonly ICategoryService _categoryService;

        public CategoryController(ICategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        // GET: Category/Index
        public async Task<IActionResult> Index()
        {
            try
            {
                var categories = await _categoryService.GetAllCategoriesAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading categories: " + ex.Message;
                return View(new List<Category>());
            }
        }

        // GET: Category/Add
        public IActionResult Add()
        {
            return View();
        }

        // POST: Category/Add
        [HttpPost]
        public async Task<IActionResult> Add(Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _categoryService.AddCategoryAsync(category);
                    TempData["SuccessMessage"] = "Category added successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error adding category: " + ex.Message;
            }
            return View(category);
        }

        // GET: Category/Update/5
        public async Task<IActionResult> Update(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null) return NotFound();
                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading category: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Category/Update/5
        [HttpPost]
        public async Task<IActionResult> Update(Category category)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    await _categoryService.UpdateCategoryAsync(category);
                    TempData["SuccessMessage"] = "Category updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating category: " + ex.Message;
            }
            return View(category);
        }

        // GET: Category/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var category = await _categoryService.GetCategoryByIdAsync(id);
                if (category == null) return NotFound();

                ViewBag.CanDelete = await _categoryService.CanDeleteCategoryAsync(id);
                return View(category);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error loading category: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Category/Delete/5
        [HttpPost]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                await _categoryService.DeleteCategoryAsync(id);
                TempData["SuccessMessage"] = "Category deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error deleting category: " + ex.Message;
            }
            return RedirectToAction(nameof(Index));
        }
    }
}