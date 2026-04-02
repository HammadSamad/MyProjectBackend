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
    [Authorize]
    public class ProductReviewController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductReviewController(LaptopHarbourDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ================= GET REVIEWS BY PRODUCT WITH AVERAGE RATING =================
        [HttpGet("product/{productId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsByProduct(int productId, int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                if (pageNumber <= 0) pageNumber = 1;
                if (pageSize <= 0 || pageSize > 50) pageSize = 10;

                var query = _context.ProductReviews
                    .Where(r => r.ProductId == productId)
                    .Include(r => r.User)
                    .Include(r => r.ReviewImages)
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
                        UserName = r.User.Username,
                        CreatedAt = r.CreatedAt,
                        UpdatedAt = r.UpdatedAt,
                        ImageUrl = r.ReviewImages
                                    .OrderBy(i => i.ReviewImageId)
                                    .Select(i => i.ImageUrl)
                                    .FirstOrDefault()
                    })
                    .ToListAsync();

                double averageRating = totalReviews == 0 ? 0 :
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
                return StatusCode(500, new { error = "Failed to fetch reviews.", details = ex.Message });
            }
        }

        // ================= CREATE REVIEW WITH IMAGE =================
        [HttpPost]
        [DisableRequestSizeLimit]
        [Authorize] // Make sure only logged-in users can post reviews
        public async Task<IActionResult> CreateReview([FromForm] CreateProductReviewWithImage model)
        {
            if (model == null || model.ProductId <= 0)
                return BadRequest(new { error = "Invalid review data." });

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { error = "You must be logged in to submit a review." });

            int userId = int.Parse(userIdClaim);

            // Validate rating
            if (model.Rating < 1 || model.Rating > 5)
                return BadRequest(new { error = "Rating must be between 1 and 5." });

            // Check if user purchased the product
            bool purchased = await _context.OrderItems
                .AnyAsync(oi => oi.Variant.ProductId == model.ProductId
                                && oi.Order.UserId == userId
                                && oi.Order.OrderStatus == "Completed");

            if (!purchased)
                return BadRequest(new { error = "You can only review products you have purchased." });

            // Check if user already reviewed
            bool alreadyReviewed = await _context.ProductReviews
                .AnyAsync(r => r.ProductId == model.ProductId && r.UserId == userId);

            if (alreadyReviewed)
                return BadRequest(new { error = "You have already reviewed this product." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Create review
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

                string? imageUrl = null;

                // Handle image upload
                if (model.Image != null)
                {
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                    var ext = Path.GetExtension(model.Image.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                        return BadRequest(new { error = "Only JPG, JPEG, PNG files are allowed." });

                    if (model.Image.Length > 5 * 1024 * 1024)
                        return BadRequest(new { error = "Image size cannot exceed 5 MB." });

                    var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "reviews");
                    Directory.CreateDirectory(uploadPath);

                    string fileName = $"{Guid.NewGuid()}{ext}";
                    string fullPath = Path.Combine(uploadPath, fileName);

                    using var stream = System.IO.File.Create(fullPath);
                    await model.Image.CopyToAsync(stream);

                    var reviewImage = new ReviewImage
                    {
                        ReviewId = review.ReviewId,
                        ImageUrl = $"/uploads/reviews/{fileName}"
                    };

                    _context.ReviewImages.Add(reviewImage);
                    await _context.SaveChangesAsync();

                    imageUrl = reviewImage.ImageUrl;
                }

                await transaction.CommitAsync();

                var dto = new ProductReviewDTO
                {
                    ReviewId = review.ReviewId,
                    Rating = review.Rating ?? 0,
                    ReviewText = review.ReviewText,
                    UserName = User.Identity?.Name ?? "Unknown",
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt,
                    ImageUrl = imageUrl
                };

                return Ok(new { message = "Review created successfully.", data = dto });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to create review.", details = ex.Message });
            }
        }


        // ================= UPDATE REVIEW + IMAGE =================
        [HttpPut("{id}")]
        [DisableRequestSizeLimit]
        [Authorize] // Only logged-in users can update their review
        public async Task<IActionResult> UpdateReview(int id, [FromForm] CreateProductReviewWithImage model)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { error = "You must be logged in to update a review." });

            int userId = int.Parse(userIdClaim);

            // Validate rating
            if (model.Rating < 1 || model.Rating > 5)
                return BadRequest(new { error = "Rating must be between 1 and 5." });

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var review = await _context.ProductReviews
                    .Include(r => r.ReviewImages)
                    .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == userId);

                if (review == null)
                    return NotFound(new { error = "Review not found or you are not authorized." });

                // Update review
                review.Rating = model.Rating;
                review.ReviewText = model.ReviewText;
                review.UpdatedAt = DateTime.UtcNow;

                string? imageUrl = review.ReviewImages.Select(i => i.ImageUrl).FirstOrDefault();

                // Handle image update
                if (model.Image != null)
                {
                    string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
                    var ext = Path.GetExtension(model.Image.FileName).ToLower();

                    if (!allowedExtensions.Contains(ext))
                        return BadRequest(new { error = "Only JPG, JPEG, PNG files are allowed." });

                    if (model.Image.Length > 5 * 1024 * 1024)
                        return BadRequest(new { error = "Image size cannot exceed 5 MB." });

                    // Delete old image if exists
                    var oldImage = review.ReviewImages.FirstOrDefault();
                    if (oldImage != null)
                    {
                        var oldPath = Path.Combine(_env.WebRootPath ?? "wwwroot", oldImage.ImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);

                        _context.ReviewImages.Remove(oldImage);
                    }

                    // Save new image
                    var uploadPath = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "reviews");
                    Directory.CreateDirectory(uploadPath);

                    string fileName = $"{Guid.NewGuid()}{ext}";
                    string fullPath = Path.Combine(uploadPath, fileName);

                    using var stream = System.IO.File.Create(fullPath);
                    await model.Image.CopyToAsync(stream);

                    var reviewImage = new ReviewImage
                    {
                        ReviewId = review.ReviewId,
                        ImageUrl = $"/uploads/reviews/{fileName}"
                    };

                    _context.ReviewImages.Add(reviewImage);
                    imageUrl = reviewImage.ImageUrl;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var dto = new ProductReviewDTO
                {
                    ReviewId = review.ReviewId,
                    Rating = review.Rating ?? 0,
                    ReviewText = review.ReviewText,
                    UserName = User.Identity?.Name ?? "Unknown",
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt,
                    ImageUrl = imageUrl
                };

                return Ok(new { message = "Review updated successfully.", data = dto });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to update review.", details = ex.Message });
            }
        }


        // ================= DELETE REVIEW + IMAGE =================
        [HttpDelete("{id}")]
        [Authorize] // Only logged-in users can delete their review
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized(new { error = "You must be logged in to delete a review." });

            int userId = int.Parse(userIdClaim);

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var review = await _context.ProductReviews
                    .Include(r => r.ReviewImages)
                    .FirstOrDefaultAsync(r => r.ReviewId == id && r.UserId == userId);

                if (review == null)
                    return NotFound(new { error = "Review not found or you are not authorized." });

                // Delete image file if exists
                var image = review.ReviewImages.FirstOrDefault();
                if (image != null)
                {
                    var filePath = Path.Combine(_env.WebRootPath ?? "wwwroot", image.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);

                    _context.ReviewImages.Remove(image);
                }

                // Delete review
                _context.ProductReviews.Remove(review);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Review deleted successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { error = "Failed to delete review.", details = ex.Message });
            }
        }

    }
}
