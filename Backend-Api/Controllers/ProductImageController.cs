using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductImagesController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IConfiguration _config;

        public ProductImagesController(LaptopHarbourDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // -------------------------------------------------------------
        // POST: api/ProductImages → Upload cover + gallery images
        // -------------------------------------------------------------
        [HttpPost]
        public async Task<IActionResult> UploadImages([FromForm] CreateProductImage model)
        {
            if (model == null || model.ProductId <= 0)
                return BadRequest("Invalid product ID.");

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == model.ProductId);

            if (product == null)
                return NotFound("Product not found.");

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var uploadRoot = _config["StoredFilesPath"] ?? Path.Combine("wwwroot", "uploads");
                var productFolder = Path.Combine(uploadRoot, "Products");
                Directory.CreateDirectory(productFolder);

                // --- Cover Image ---
                if (model.CoverImageUrl != null)
                {
                    var oldCover = product.ProductImages.FirstOrDefault(img => img.IsCover == true);
                    if (oldCover != null)
                    {
                        var oldPath = Path.Combine(productFolder, oldCover.ImageUrl ?? "");
                        if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
                        _context.ProductImages.Remove(oldCover);
                    }

                    var coverFileName = Guid.NewGuid() + Path.GetExtension(model.CoverImageUrl.FileName);
                    var coverPath = Path.Combine(productFolder, coverFileName);
                    using var stream = System.IO.File.Create(coverPath);
                    await model.CoverImageUrl.CopyToAsync(stream);

                    product.ProductImages.Add(new ProductImage
                    {
                        ImageUrl = coverFileName,
                        IsCover = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // --- Gallery Images ---
                if (model.ImageUrl != null && model.ImageUrl.Length > 0)
                {
                    foreach (var file in model.ImageUrl)
                    {
                        var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
                        var filePath = Path.Combine(productFolder, fileName);
                        using var stream = System.IO.File.Create(filePath);
                        await file.CopyToAsync(stream);

                        product.ProductImages.Add(new ProductImage
                        {
                            ImageUrl = fileName,
                            IsCover = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new { message = "Images uploaded successfully." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(ex.Message);
            }
        }

        // -------------------------------------------------------------
        // GET: api/ProductImages/product/5 → Get images of a product
        // -------------------------------------------------------------
        [HttpGet("product/{productId}")]
        public async Task<ActionResult<ProductImageDTO>> GetProductImages(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return NotFound("Product not found.");

            var dto = new ProductImageDTO
            {
                CoverImage = product.ProductImages?.FirstOrDefault(img => img.IsCover == true)?.ImageUrl,
                GalleryImages = product.ProductImages?
                                    .Where(img => img.IsCover == false)
                                    .Select(img => img.ImageUrl!)
                                    .ToList() ?? new List<string>()
            };

            return Ok(dto);
        }

        // -------------------------------------------------------------
        // PUT: api/ProductImages/{imageId} → Update single image
        // -------------------------------------------------------------
        [HttpPut("{imageId}")]
        public async Task<IActionResult> UpdateImage(int imageId, [FromForm] IFormFile file)
        {
            if (file == null)
                return BadRequest("File is required.");

            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null) return NotFound("Image not found.");

            var uploadRoot = _config["StoredFilesPath"] ?? Path.Combine("wwwroot", "uploads");
            var productFolder = Path.Combine(uploadRoot, "Products");
            Directory.CreateDirectory(productFolder);

            // Upload new file
            var fileName = Guid.NewGuid() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(productFolder, fileName);
            using var stream = System.IO.File.Create(filePath);
            await file.CopyToAsync(stream);

            // Delete old file
            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var oldFile = Path.Combine(productFolder, image.ImageUrl);
                if (System.IO.File.Exists(oldFile)) System.IO.File.Delete(oldFile);
            }

            image.ImageUrl = fileName;
            image.CreatedAt = DateTime.UtcNow;

            _context.ProductImages.Update(image);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Image updated successfully." });
        }

        // -------------------------------------------------------------
        // DELETE: api/ProductImages/{imageId} → Delete single image
        // -------------------------------------------------------------
        [HttpDelete("{imageId}")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null) return NotFound("Image not found.");

            var uploadRoot = _config["StoredFilesPath"] ?? Path.Combine("wwwroot", "uploads");
            var productFolder = Path.Combine(uploadRoot, "Products");
            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var path = Path.Combine(productFolder, image.ImageUrl);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Image deleted successfully." });
        }

        // -------------------------------------------------------------
        // DELETE: api/ProductImages/product/5 → Delete all images of a product
        // -------------------------------------------------------------
        [HttpDelete("product/{productId}")]
        public async Task<IActionResult> DeleteAllImages(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null) return NotFound("Product not found.");

            var uploadRoot = _config["StoredFilesPath"] ?? Path.Combine("wwwroot", "uploads");
            var productFolder = Path.Combine(uploadRoot, "Products");

            foreach (var image in product.ProductImages)
            {
                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var path = Path.Combine(productFolder, image.ImageUrl);
                    if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
                }
            }

            _context.ProductImages.RemoveRange(product.ProductImages);
            await _context.SaveChangesAsync();

            return Ok(new { message = "All images deleted successfully." });
        }
    }
}
