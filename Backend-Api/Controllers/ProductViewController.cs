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

        // =====================================================
        // GET: api/ProductView
        // Admin / Analytics purpose
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetAllViews()
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

        // =====================================================
        // DELETE: api/ProductView/cleanup/7
        // Backup cleanup if trigger fails or DB idle
        // =====================================================
        [HttpDelete("cleanup/{days}")]
        public async Task<IActionResult> CleanupOldViews(int days)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(7-days);

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
    }
}
