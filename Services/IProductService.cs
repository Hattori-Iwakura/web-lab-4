using web_lab_4.Models;

namespace web_lab_4.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllProductsAsync();
        Task<Product> GetProductByIdAsync(int id);
        Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId);
        Task AddProductAsync(Product product, IFormFile imageFile);
        Task UpdateProductAsync(int id, Product product, IFormFile imageFile);
        Task DeleteProductAsync(int id);
        Task<string> SaveImageAsync(IFormFile image);
        Task<Product> GetProductWithRelatedAsync(int id);
        Task<bool> CheckStockAvailabilityAsync(int productId, int quantity);
        Task UpdateStockAsync(int productId, int quantity);
    }
}