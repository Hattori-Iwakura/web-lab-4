using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace web_lab_4.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment cannot exceed 1000 characters")]
        public string? Comment { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        [StringLength(100)]
        public string ReviewerName { get; set; } = string.Empty;

        [EmailAddress]
        public string? ReviewerEmail { get; set; }

        public bool IsVerifiedPurchase { get; set; } = false;
        public bool IsApproved { get; set; } = true; // Auto-approve or manual moderation

        // Navigation properties
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual IdentityUser User { get; set; } = null!;
    }
}