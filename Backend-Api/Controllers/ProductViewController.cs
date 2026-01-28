using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductViewController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ProductViewController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // =====================================================
        // POST: api/ProductView
        // Log a product view (minimal)
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> AddProductView([FromBody] CreateProductView model)
        {
            if (model == null || model.ProductId <= 0)
                return BadRequest(new { error = "Invalid product ID" });

            try
            {
                var view = new ProductView
                {
                    ProductId = model.ProductId,
                    UserId = model.UserId,
                    ViewedAt = DateTime.UtcNow
                };

                _context.ProductViews.Add(view);
                await _context.SaveChangesAsync();

                var dto = new
                {
                    view.ViewId,
                    view.ProductId,
                    view.UserId,
                    view.ViewedAt
                };

                return Ok(new { message = "Product view logged successfully", data = dto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        // =====================================================
        // GET: api/ProductView
        // Fetch all product views with product data
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetAllViews()
        {
            try
            {
                var views = await _context.ProductViews
                    .Include(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                    .Include(v => v.User)
                    .OrderByDescending(v => v.ViewedAt)
                    .ToListAsync();

                var dtoList = views.Select(v =>
                {
                    string? productImage = null;

                    if (v.Product?.ProductImages != null && v.Product.ProductImages.Any())
                    {
                        var coverImage = v.Product.ProductImages.FirstOrDefault(i => i.IsCover.HasValue && i.IsCover.Value);
                        productImage = coverImage?.ImageUrl ?? v.Product.ProductImages.FirstOrDefault()?.ImageUrl;
                    }

                    return new ProductViewDTO
                    {
                        ViewId = v.ViewId,
                        ProductId = v.ProductId,
                        ProductName = v.Product?.ProductName ?? "Unknown",
                        ProductImage = productImage,
                        UserId = v.UserId,
                        Username = v.User?.Username ?? "Guest",
                        ViewedAt = v.ViewedAt
                    };
                }).ToList();

                return Ok(dtoList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch product views", details = ex.Message });
            }
        }

        // =====================================================
        // DELETE: api/ProductView/cleanup/{days}
        // Delete old product views older than X days
        // =====================================================
        [HttpDelete("cleanup/{days}")]
        public async Task<IActionResult> CleanupOldViews(int days)
        {
            if (days < 7)
                return BadRequest(new { error = "Days parameter must be at least 7." });

            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);

                var oldViews = await _context.ProductViews
                    .Where(v => v.ViewedAt < cutoffDate)
                    .ToListAsync();

                if (!oldViews.Any())
                    return Ok(new { message = "No old product views to delete" });

                _context.ProductViews.RemoveRange(oldViews);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Old product views deleted successfully",
                    deletedCount = oldViews.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete old product views", details = ex.Message });
            }
        }

    }
}
