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
    [Authorize] // Require authentication for creating/updating/deleting
    public class ProductReviewController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ProductReviewController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------
        // GET: api/ProductReview/product/5
        // List all reviews for a product
        // -------------------------------------------------------------
        [HttpGet("product/{productId}")]
        [AllowAnonymous] // Anyone can view reviews
        public async Task<ActionResult<IEnumerable<ProductReviewDTO>>> GetReviewsByProduct(int productId)
        {
            var reviews = await _context.ProductReviews
                .Where(r => r.ProductId == productId)
                .Include(r => r.User)
                .Select(r => new ProductReviewDTO
                {
                    ReviewId = r.ReviewId,
                    Rating = r.Rating ?? 0,
                    ReviewText = r.ReviewText,
                    UserName = r.User != null ? r.User.Username : "Unknown",
                    CreatedAt = r.CreatedAt
                })
                .ToListAsync();

            return Ok(reviews);
        }

        // -------------------------------------------------------------
        // POST: api/ProductReview
        // Create a review for a product
        // -------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> CreateReview([FromBody] CreateProductReview model)
        {
            if (model == null || model.ProductId <= 0)
                return BadRequest("Invalid review data.");

            // Get logged-in user ID from JWT token
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");
            var userId = int.Parse(userIdClaim);

            // Optional: Check if user has already reviewed this product
            var existingReview = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.ProductId == model.ProductId && r.UserId == userId);

            if (existingReview != null)
                return BadRequest("You have already reviewed this product.");

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

            // Return DTO with username
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

            return Ok(dto);
        }

        // -------------------------------------------------------------
        // PUT: api/ProductReview/5
        // Update review (only owner can update)
        // -------------------------------------------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateReview(int id, [FromBody] CreateProductReview model)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");
            var userId = int.Parse(userIdClaim);

            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == userId);

            if (review == null)
                return NotFound("Review not found or you are not authorized.");

            review.Rating = model.Rating;
            review.ReviewText = model.ReviewText;
            review.UpdatedAt = DateTime.UtcNow; // Only update timestamp

            _context.ProductReviews.Update(review);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // -------------------------------------------------------------
        // DELETE: api/ProductReview/5
        // Delete review (only owner can delete)
        // -------------------------------------------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");
            var userId = int.Parse(userIdClaim);

            var review = await _context.ProductReviews
                .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == userId);

            if (review == null)
                return NotFound("Review not found or you are not authorized.");

            _context.ProductReviews.Remove(review);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
