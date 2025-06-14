using Microsoft.EntityFrameworkCore;
using web_lab_4.Data;
using web_lab_4.Models;

namespace web_lab_4.Repositories
{
    public class EFReviewRepository : IReviewRepository
    {
        private readonly ApplicationDbContext _context;

        public EFReviewRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Review>> GetProductReviewsAsync(int productId)
        {
            return await _context.Reviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<Review?> GetUserReviewAsync(int productId, string userId)
        {
            return await _context.Reviews
                .FirstOrDefaultAsync(r => r.ProductId == productId && r.UserId == userId);
        }

        public async Task<Review> AddReviewAsync(Review review)
        {
            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task<Review> UpdateReviewAsync(Review review)
        {
            review.UpdatedAt = DateTime.Now;
            _context.Reviews.Update(review);
            await _context.SaveChangesAsync();
            return review;
        }

        public async Task DeleteReviewAsync(int reviewId)
        {
            var review = await _context.Reviews.FindAsync(reviewId);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> HasUserReviewedProductAsync(int productId, string userId)
        {
            return await _context.Reviews
                .AnyAsync(r => r.ProductId == productId && r.UserId == userId);
        }

        public async Task<(double averageRating, int reviewCount)> GetProductRatingStatsAsync(int productId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.ProductId == productId && r.IsApproved)
                .ToListAsync();

            if (!reviews.Any())
                return (0, 0);

            var averageRating = reviews.Average(r => r.Rating);
            var reviewCount = reviews.Count;

            return (averageRating, reviewCount);
        }
    }
}