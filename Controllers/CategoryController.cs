using web_lab_4.Models;
using web_lab_4.Repositories;
using Microsoft.AspNetCore.Mvc;

public class CategoryController : Controller
{
    private readonly ICategoryRepository _categoryRepository;

    public CategoryController(ICategoryRepository categoryRepository)
    {
        _categoryRepository = categoryRepository;
    }

    // GET: Category/Update/5
    public async Task<IActionResult> Update(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();

        return View(category);
    }

    // POST: Category/Update/5
    [HttpPost]
    public async Task<IActionResult> Update(Category category)
    {
        if (ModelState.IsValid)
        {
            await _categoryRepository.UpdateAsync(category);
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }

    // GET: Category/Delete/5
    public async Task<IActionResult> Delete(int id)
    {
        var category = await _categoryRepository.GetByIdAsync(id);
        if (category == null) return NotFound();

        return View(category);
    }

    // POST: Category/Delete/5
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _categoryRepository.DeleteAsync(id);
        return RedirectToAction(nameof(Index));
    }

    // GET: Category/Index
    public async Task<IActionResult> Index()
    {
        var categories = await _categoryRepository.GetAllAsync();
        return View(categories);
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
        if (ModelState.IsValid)
        {
            await _categoryRepository.AddAsync(category);
            return RedirectToAction(nameof(Index));
        }
        return View(category);
    }
}
