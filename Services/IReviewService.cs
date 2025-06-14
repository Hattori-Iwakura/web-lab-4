using web_lab_4.Models;

namespace web_lab_4.Services
{
    public interface IReviewService
    {
        Task<IEnumerable<Review>> GetProductReviewsAsync(int productId);
        Task<Review?> GetUserReviewAsync(int productId, string userId);
        Task<Review> AddReviewAsync(int productId, string userId, int rating, string? comment, string reviewerName);
        Task<Review> UpdateReviewAsync(int reviewId, int rating, string? comment);
        Task DeleteReviewAsync(int reviewId, string userId);
        Task<bool> CanUserReviewProductAsync(int productId, string userId);
        Task<(double averageRating, int reviewCount)> GetProductRatingStatsAsync(int productId);
    }
}