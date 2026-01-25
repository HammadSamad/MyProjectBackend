using Backend_Api.Data;
using Backend_Api.Models;
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
        // When a user views a product
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> AddProductView(int productId, int? userId = null)
        {
            if (productId <= 0)
                return BadRequest(new { error = "Invalid product ID" });

            try
            {
                // Check if product exists
                var productExists = await _context.Products.AnyAsync(p => p.ProductId == productId);
                if (!productExists)
                    return NotFound(new { error = "Product not found" });

                var view = new ProductView
                {
                    ProductId = productId,
                    UserId = userId,
                    ViewedAt = DateTime.UtcNow
                };

                _context.ProductViews.Add(view);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Product view logged successfully" });
            }
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { error = "Database error occurred while logging product view", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }

        // =====================================================
        // GET: api/ProductView
        // Admin / Analytics purpose
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetAllViews()
        {
            try
            {
                var views = await _context.ProductViews
                    .Include(v => v.Product)
                    .Include(v => v.User)
                    .Select(v => new
                    {
                        v.ViewId,
                        v.ProductId,
                        ProductName = v.Product.ProductName,
                        v.UserId,
                        Username = v.User != null ? v.User.Username : "Guest",
                        v.ViewedAt
                    })
                    .OrderByDescending(v => v.ViewedAt)
                    .ToListAsync();

                return Ok(views);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch product views", details = ex.Message });
            }
        }

        // =====================================================
        // DELETE: api/ProductView/cleanup/7
        // Backup cleanup if trigger fails or DB idle
        // =====================================================
        [HttpDelete("cleanup/{days}")]
        public async Task<IActionResult> CleanupOldViews(int days)
        {
            if (days <= 0)
                return BadRequest(new { error = "Days parameter must be greater than zero" });

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
            catch (DbUpdateException dbEx)
            {
                return StatusCode(500, new { error = "Database error occurred while deleting old product views", details = dbEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An unexpected error occurred", details = ex.Message });
            }
        }
    }
}
