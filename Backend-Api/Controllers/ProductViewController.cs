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
        // Insert OR Update product view (NO DUPLICATES)
        // If same user views same product again → only ViewedAt is updated
        // =====================================================
        [HttpPost]
        public async Task<IActionResult> AddProductView([FromBody] CreateProductView model)
        {
            if (model == null || model.ProductId <= 0)
                return BadRequest(new { error = "Invalid product ID" });

            try
            {
                // 🔍 Check if already exists for same user + product
                var existingView = await _context.ProductViews
                    .FirstOrDefaultAsync(v =>
                        v.ProductId == model.ProductId &&
                        v.UserId == model.UserId);

                if (existingView != null)
                {
                    // 🔁 Update ViewedAt only
                    existingView.ViewedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Product view updated successfully",
                        data = new
                        {
                            existingView.ViewId,
                            existingView.ProductId,
                            existingView.UserId,
                            existingView.ViewedAt
                        }
                    });
                }
                else
                {
                    // ➕ Insert new row
                    var view = new ProductView
                    {
                        ProductId = model.ProductId,
                        UserId = model.UserId,
                        ViewedAt = DateTime.UtcNow
                    };

                    _context.ProductViews.Add(view);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        message = "Product view logged successfully",
                        data = new
                        {
                            view.ViewId,
                            view.ProductId,
                            view.UserId,
                            view.ViewedAt
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An unexpected error occurred",
                    details = ex.Message
                });
            }
        }

        // =====================================================
        // GET: api/ProductView
        // Fetch all product views with discount price logic
        // =====================================================
        [HttpGet]
        public async Task<IActionResult> GetAllViews()
        {
            try
            {
                var views = await _context.ProductViews
                    .Include(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                    .Include(v => v.Product)
                        .ThenInclude(p => p.ProductVariants)   // 🔥 needed for discount
                    .Include(v => v.User)
                    .OrderByDescending(v => v.ViewedAt)
                    .ToListAsync();

                var dtoList = views.Select(v =>
                {
                    string? productImage = null;

                    if (v.Product?.ProductImages != null && v.Product.ProductImages.Any())
                    {
                        var coverImage = v.Product.ProductImages
                            .FirstOrDefault(i => i.IsCover.HasValue && i.IsCover.Value);

                        productImage = coverImage?.ImageUrl
                                       ?? v.Product.ProductImages.FirstOrDefault()?.ImageUrl;
                    }

                    // 🔥 Cheapest variant logic (same as ProductsController)
                    var cheapestVariant = v.Product?.ProductVariants
                        .OrderBy(x => CalculateFinalPrice(x))
                        .FirstOrDefault();

                    decimal originalPrice = cheapestVariant?.Price ?? 0;
                    decimal discountPrice = cheapestVariant != null
                        ? CalculateFinalPrice(cheapestVariant)
                        : 0;

                    bool isDiscounted = discountPrice < originalPrice;

                    decimal discountPercentage = 0;
                    if (originalPrice > 0)
                    {
                        discountPercentage = ((originalPrice - discountPrice) / originalPrice) * 100;
                        discountPercentage = Math.Round(discountPercentage, 2);
                    }

                    return new ProductViewDTO
                    {
                        ViewId = v.ViewId,
                        ProductId = v.ProductId,
                        ProductName = v.Product?.ProductName ?? "Unknown",
                        ProductImage = productImage,

                        // 🔥 Discounted pricing info
                        OriginalPrice = originalPrice,
                        DiscountPrice = discountPrice,
                        DiscountPercentage = discountPercentage,
                        IsDiscounted = isDiscounted,

                        UserId = v.UserId,
                        Username = v.User?.Username ?? "Guest",
                        ViewedAt = v.ViewedAt
                    };
                }).ToList();

                return Ok(dtoList);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "Failed to fetch product views",
                    details = ex.Message
                });
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
                return StatusCode(500, new
                {
                    error = "Failed to delete old product views",
                    details = ex.Message
                });
            }
        }

        // =====================================================
        // FINAL PRICE CALCULATOR (Same as ProductsController)
        // =====================================================
        private decimal CalculateFinalPrice(ProductVariant v)
        {
            decimal price = v.Price ?? 0;
            var now = DateTime.UtcNow;

            if (v.DiscountStart.HasValue && now < v.DiscountStart) return price;
            if (v.DiscountEnd.HasValue && now > v.DiscountEnd) return price;

            if (v.DiscountPercentage.HasValue && v.DiscountPercentage > 0)
                price -= price * (v.DiscountPercentage.Value / 100);

            if (v.DiscountAmount.HasValue && v.DiscountAmount > 0)
                price -= v.DiscountAmount.Value;

            return price < 0 ? 0 : price;
        }
    }
}
