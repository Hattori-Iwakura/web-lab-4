using web_lab_4.Models;
using web_lab_4.Repositories;

namespace web_lab_4.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductService(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IEnumerable<Product>> GetAllProductsAsync()
        {
            return await _productRepository.GetAllAsync();
        }

        public async Task<Product> GetProductByIdAsync(int id)
        {
            return await _productRepository.GetByIdAsync(id);
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId)
        {
            return await _productRepository.GetByCategoryAsync(categoryId);
        }

        public async Task<Product> GetProductWithRelatedAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product != null)
            {
                // Get related products from the same category
                var relatedProducts = await _productRepository.GetByCategoryAsync(product.CategoryId);
                // You might want to add a RelatedProducts property to Product model or handle this differently
            }
            return product;
        }

        public async Task AddProductAsync(Product product, IFormFile imageFile)
        {
            if (imageFile != null)
            {
                product.ImageUrl = await SaveImageAsync(imageFile);
            }
            
            // Validate business rules
            await ValidateProductAsync(product);
            
            await _productRepository.AddAsync(product);
        }

        public async Task UpdateProductAsync(int id, Product product, IFormFile imageFile)
        {
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null) 
                throw new ArgumentException($"Product with ID {id} not found");

            if (imageFile != null)
            {
                product.ImageUrl = await SaveImageAsync(imageFile);
            }
            else
            {
                product.ImageUrl = existingProduct.ImageUrl;
            }

            // Update properties
            existingProduct.Name = product.Name;
            existingProduct.Price = product.Price;
            existingProduct.Description = product.Description;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.ImageUrl = product.ImageUrl;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Weight = product.Weight;
            existingProduct.WeightUnit = product.WeightUnit;
            existingProduct.Flavor = product.Flavor;
            existingProduct.Brand = product.Brand;
            existingProduct.ExpiryDate = product.ExpiryDate;
            existingProduct.Ingredients = product.Ingredients;
            existingProduct.NutritionalInfo = product.NutritionalInfo;
            existingProduct.UsageInstructions = product.UsageInstructions;
            existingProduct.IsAvailable = product.IsAvailable;

            // Validate business rules
            await ValidateProductAsync(existingProduct);

            await _productRepository.UpdateAsync(existingProduct);
        }

        public async Task DeleteProductAsync(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new ArgumentException($"Product with ID {id} not found");

            await _productRepository.DeleteAsync(id);
        }

        public async Task<string> SaveImageAsync(IFormFile image)
        {
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(image.FileName);
            var savePath = Path.Combine("wwwroot/images", fileName);
            
            // Ensure directory exists
            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            
            using (var fileStream = new FileStream(savePath, FileMode.Create))
            {
                await image.CopyToAsync(fileStream);
            }
            return "/images/" + fileName;
        }

        public async Task<bool> CheckStockAvailabilityAsync(int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            return product != null && product.IsAvailable && product.StockQuantity >= quantity;
        }

        public async Task UpdateStockAsync(int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
                throw new ArgumentException($"Product with ID {productId} not found");

            product.StockQuantity -= quantity;
            if (product.StockQuantity < 0)
                throw new InvalidOperationException("Insufficient stock");

            await _productRepository.UpdateAsync(product);
        }

        private async Task ValidateProductAsync(Product product)
        {
            // Check if category exists
            var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
            if (category == null)
                throw new ArgumentException("Invalid category");

            // Check expiry date
            if (product.ExpiryDate.HasValue && product.ExpiryDate.Value <= DateTime.Now)
                throw new ArgumentException("Expiry date must be in the future");

            // Validate weight
            if (product.Weight <= 0)
                throw new ArgumentException("Weight must be greater than 0");

            // Validate price
            if (product.Price <= 0)
                throw new ArgumentException("Price must be greater than 0");
        }

        // Add these new methods to ProductService class

        public async Task<IEnumerable<Product>> GetAllProductsWithReviewsAsync()
        {
            // This would ideally load products with their reviews
            // For now, just return all products - the review loading will be handled separately
            return await _productRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Product>> GetFeaturedProductsAsync(int count = 8)
        {
            var allProducts = await _productRepository.GetAllAsync();
            
            // Return featured products based on some criteria
            // For example: available products, ordered by some priority
            return allProducts
                .Where(p => p.IsAvailable && p.IsInStock)
                .OrderByDescending(p => p.Price) // or any other criteria
                .Take(count)
                .ToList();
        }

        public async Task<IEnumerable<Product>> GetProductsByCategoryAsync(int categoryId, int count)
        {
            var categoryProducts = await _productRepository.GetByCategoryAsync(categoryId);
            return categoryProducts
                .Where(p => p.IsAvailable)
                .Take(count)
                .ToList();
        }
    }
}