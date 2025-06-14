using web_lab_4.Models;

namespace web_lab_4.Repositories
{
    public interface IReviewRepository
    {
        Task<IEnumerable<Review>> GetProductReviewsAsync(int productId);
        Task<Review?> GetUserReviewAsync(int productId, string userId);
        Task<Review> AddReviewAsync(Review review);
        Task<Review> UpdateReviewAsync(Review review);
        Task DeleteReviewAsync(int reviewId);
        Task<bool> HasUserReviewedProductAsync(int productId, string userId);
        Task<(double averageRating, int reviewCount)> GetProductRatingStatsAsync(int productId);
    }
}