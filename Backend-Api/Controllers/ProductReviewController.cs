using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // User must be logged in for create/update/delete
    public class ProductReviewController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ProductReviewController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // =========================================================
        // GET REVIEWS BY PRODUCT (Pagination + Average Rating)
        // GET: api/ProductReview/product/{productId}
        // =========================================================
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsByProduct(
            int productId,
            int pageNumber = 1,
            int pageSize = 10)
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0 || pageSize > 50) pageSize = 10;

                var query = _context.ProductReviews
                    .Where(r => r.ProductId == productId)
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt);

                var totalReviews = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalReviews / (double)pageSize);

                var reviews = await query
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new ProductReviewDTO
                    {
                        ReviewId = r.ReviewId,
                        Rating = r.Rating ?? 0,
                        ReviewText = r.ReviewText,
                        UserName = r.User != null ? r.User.Username : "Unknown",
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                // Safe Average Rating calculation
                var averageRating = totalReviews == 0 ? 0 :
                    await _context.ProductReviews
                        .Where(r => r.ProductId == productId)
                        .AverageAsync(r => r.Rating ?? 0);

                return Ok(new
                {
                    TotalReviews = totalReviews,
                    TotalPages = totalPages,
                    CurrentPage = pageNumber,
                    PageSize = pageSize,
                    AverageRating = Math.Round(averageRating, 1),
                    Reviews = reviews
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to fetch reviews.",
                    details = ex.Message
                });
            }
        }

        // =========================================================
        // CREATE REVIEW
        // User must be logged in
        // User must have purchased the product
        // Only one review per product per user
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateProductReview model)
        {
            try
            {
                if (model == null || model.ProductId <= 0)
                    return BadRequest(new { error = "Invalid review data." });

                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { error = "You must be logged in to submit a review." });

                int userId = int.Parse(userIdClaim);

                // Check if user purchased the product
                bool purchased = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .AnyAsync(oi =>
                        oi.ProductId == model.ProductId &&
                        oi.Order.UserId == userId);

                if (!purchased)
                    return BadRequest(new { error = "You can only review products you have purchased." });

                // Prevent duplicate review
                bool alreadyReviewed = await _context.ProductReviews
                    .AnyAsync(r => r.ProductId == model.ProductId && r.UserId == userId);

                if (alreadyReviewed)
                    return BadRequest(new { error = "You have already reviewed this product." });

                var review = new ProductReview
                {
                    ProductId = model.ProductId,
                    UserId = userId,
                    Rating = model.Rating,
                    ReviewText = model.ReviewText,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ProductReviews.Add(review);
                await _context.SaveChangesAsync();

                var dto = await _context.ProductReviews
                    .Include(r => r.User)
                    .Where(r => r.ReviewId == review.ReviewId)
                    .Select(r => new ProductReviewDTO
                    {
                        ReviewId = r.ReviewId,
                        Rating = r.Rating ?? 0,
                        ReviewText = r.ReviewText,
                        UserName = r.User != null ? r.User.Username : "Unknown",
                        CreatedAt = r.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                return Ok(new
                {
                    message = "Review created successfully.",
                    data = dto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to create review.",
                    details = ex.Message
                });
            }
        }

        // =========================================================
        // UPDATE REVIEW
        // Only owner can update
        // No duplicate review text for same product
        // =========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] CreateProductReview model)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { error = "You must be logged in to update a review." });

                int userId = int.Parse(userIdClaim);

                var review = await _context.ProductReviews
                    .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == userId);

                if (review == null)
                    return NotFound(new { error = "Review not found or you are not authorized." });

                // Prevent duplicate review text for same product
                bool duplicate = await _context.ProductReviews
                    .AnyAsync(r =>
                        r.ProductId == review.ProductId &&
                        r.UserId == userId &&
                        r.ReviewId != review.ReviewId &&
                        r.ReviewText == model.ReviewText);

                if (duplicate)
                    return BadRequest(new
                    {
                        error = "You have already submitted a review with the same text for this product."
                    });

                review.Rating = model.Rating;
                review.ReviewText = model.ReviewText;
                review.UpdatedAt = DateTime.UtcNow;

                _context.ProductReviews.Update(review);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Review updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to update review.",
                    details = ex.Message
                });
            }
        }

        // =========================================================
        // DELETE REVIEW
        // Only owner can delete
        // =========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            try
            {
                var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new { error = "You must be logged in to delete a review." });

                int userId = int.Parse(userIdClaim);

                var review = await _context.ProductReviews
                    .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == userId);

                if (review == null)
                    return NotFound(new { error = "Review not found or you are not authorized." });

                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Review deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to delete review.",
                    details = ex.Message
                });
            }
        }
    }
}
