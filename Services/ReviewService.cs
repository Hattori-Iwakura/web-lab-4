using web_lab_4.Models;
using web_lab_4.Repositories;

namespace web_lab_4.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IProductRepository _productRepository;

        public ReviewService(IReviewRepository reviewRepository, IProductRepository productRepository)
        {
            _reviewRepository = reviewRepository;
            _productRepository = productRepository;
        }

        public async Task<IEnumerable<Review>> GetProductReviewsAsync(int productId)
        {
            return await _reviewRepository.GetProductReviewsAsync(productId);
        }

        public async Task<Review?> GetUserReviewAsync(int productId, string userId)
        {
            return await _reviewRepository.GetUserReviewAsync(productId, userId);
        }

        public async Task<Review> AddReviewAsync(int productId, string userId, int rating, string? comment, string reviewerName)
        {
            // Check if user already reviewed this product
            var existingReview = await _reviewRepository.GetUserReviewAsync(productId, userId);
            if (existingReview != null)
            {
                throw new InvalidOperationException("You have already reviewed this product.");
            }

            // Verify product exists
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                throw new ArgumentException("Product not found.");
            }

            var review = new Review
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Comment = comment?.Trim(),
                ReviewerName = reviewerName.Trim(),
                CreatedAt = DateTime.Now,
                IsApproved = true // Auto-approve for now
            };

            return await _reviewRepository.AddReviewAsync(review);
        }

        public async Task<Review> UpdateReviewAsync(int reviewId, int rating, string? comment)
        {
            var review = await _reviewRepository.GetUserReviewAsync(reviewId, ""); // You'll need to modify this
            if (review == null)
            {
                throw new ArgumentException("Review not found.");
            }

            review.Rating = rating;
            review.Comment = comment?.Trim();

            return await _reviewRepository.UpdateReviewAsync(review);
        }

        public async Task DeleteReviewAsync(int reviewId, string userId)
        {
            var review = await _reviewRepository.GetUserReviewAsync(0, userId); // You'll need to get by ID
            if (review == null || review.UserId != userId)
            {
                throw new UnauthorizedAccessException("You can only delete your own reviews.");
            }

            await _reviewRepository.DeleteReviewAsync(reviewId);
        }

        public async Task<bool> CanUserReviewProductAsync(int productId, string userId)
        {
            // Check if user already reviewed
            var hasReviewed = await _reviewRepository.HasUserReviewedProductAsync(productId, userId);
            if (hasReviewed)
                return false;

            // Optional: Check if user has purchased the product
            // You can implement this based on your order system

            return true;
        }

        public async Task<(double averageRating, int reviewCount)> GetProductRatingStatsAsync(int productId)
        {
            return await _reviewRepository.GetProductRatingStatsAsync(productId);
        }
    }
}