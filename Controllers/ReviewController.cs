using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web_lab_4.Services;
using System.Security.Claims;

namespace web_lab_4.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly UserManager<IdentityUser> _userManager;

        public ReviewController(IReviewService reviewService, UserManager<IdentityUser> userManager)
        {
            _reviewService = reviewService;
            _userManager = userManager;
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string? comment)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _userManager.GetUserAsync(User);
                
                if (userId == null || user == null)
                {
                    return Json(new { success = false, message = "Please login to leave a review." });
                }

                // Check if user can review this product
                var canReview = await _reviewService.CanUserReviewProductAsync(productId, userId);
                if (!canReview)
                {
                    return Json(new { success = false, message = "You have already reviewed this product." });
                }

                var reviewerName = user.UserName ?? "Anonymous";
                var review = await _reviewService.AddReviewAsync(productId, userId, rating, comment, reviewerName);

                return Json(new { 
                    success = true, 
                    message = "Your review has been added successfully!",
                    reviewId = review.Id
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReview(int reviewId, int rating, string? comment)
        {
            try
            {
                var review = await _reviewService.UpdateReviewAsync(reviewId, rating, comment);
                return Json(new { 
                    success = true, 
                    message = "Your review has been updated successfully!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int reviewId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                await _reviewService.DeleteReviewAsync(reviewId, userId);
                
                return Json(new { 
                    success = true, 
                    message = "Your review has been deleted successfully!" 
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetProductReviews(int productId)
        {
            try
            {
                var reviews = await _reviewService.GetProductReviewsAsync(productId);
                var stats = await _reviewService.GetProductRatingStatsAsync(productId);

                return Json(new {
                    success = true,
                    reviews = reviews.Select(r => new {
                        id = r.Id,
                        rating = r.Rating,
                        comment = r.Comment,
                        reviewerName = r.ReviewerName,
                        createdAt = r.CreatedAt.ToString("MMM dd, yyyy"),
                        isVerifiedPurchase = r.IsVerifiedPurchase
                    }),
                    averageRating = stats.averageRating,
                    reviewCount = stats.reviewCount
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}