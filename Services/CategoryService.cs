using web_lab_4.Models;
using web_lab_4.Repositories;

namespace web_lab_4.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;

        public CategoryService(ICategoryRepository categoryRepository, IProductRepository productRepository)
        {
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<Category>> GetAllCategoriesAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<Category> GetCategoryByIdAsync(int id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task AddCategoryAsync(Category category)
        {
            // Validate business rules
            await ValidateCategoryAsync(category);
            
            await _categoryRepository.AddAsync(category);
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            var existingCategory = await _categoryRepository.GetByIdAsync(category.Id);
            if (existingCategory == null)
                throw new ArgumentException($"Category with ID {category.Id} not found");

            // Validate business rules
            await ValidateCategoryAsync(category);

            await _categoryRepository.UpdateAsync(category);
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new ArgumentException($"Category with ID {id} not found");

            // Check if category can be deleted
            if (!await CanDeleteCategoryAsync(id))
                throw new InvalidOperationException("Cannot delete category that contains products");

            await _categoryRepository.DeleteAsync(id);
        }

        public async Task<bool> CanDeleteCategoryAsync(int id)
        {
            var products = await _productRepository.GetByCategoryAsync(id);
            return !products.Any();
        }

        private async Task ValidateCategoryAsync(Category category)
        {
            // Check for duplicate names
            var allCategories = await _categoryRepository.GetAllAsync();
            var duplicate = allCategories.FirstOrDefault(c => 
                c.Name.ToLower() == category.Name.ToLower() && c.Id != category.Id);
            
            if (duplicate != null)
                throw new ArgumentException("Category name already exists");

            // Validate name
            if (string.IsNullOrWhiteSpace(category.Name))
                throw new ArgumentException("Category name is required");
        }
    }
}