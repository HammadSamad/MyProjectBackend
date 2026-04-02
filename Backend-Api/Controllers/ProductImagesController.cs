using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
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

        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

        public ProductImagesController(LaptopHarbourDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ================= HELPERS =================

        private bool IsValidImage(IFormFile file)
        {
            if (file == null) return false;
            var ext = Path.GetExtension(file.FileName).ToLower();
            return _allowedExtensions.Contains(ext) &&
                   file.Length > 0 &&
                   file.Length <= _maxFileSize;
        }

        private string GetProductImagePath()
        {
            var root = _config["StoredFilesPath"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

            var path = Path.Combine(root, "Products");
            Directory.CreateDirectory(path);
            return path;
        }

        // =========================================================
        // POST: Upload / Replace Cover Image
        // =========================================================
        [HttpPost("cover")]
        public async Task<IActionResult> UploadCoverImage(
            [FromForm] int productId,
            [FromForm] IFormFile coverImage)
        {
            if (productId <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            if (!IsValidImage(coverImage))
                return BadRequest(new { message = "Invalid cover image." });

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return NotFound(new { message = "Product not found." });

            var folder = GetProductImagePath();

            var oldCover = product.ProductImages
                .FirstOrDefault(x => x.IsCover.GetValueOrDefault());

            if (oldCover != null)
            {
                var oldPath = Path.Combine(folder, oldCover.ImageUrl!);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);

                _context.ProductImages.Remove(oldCover);
            }

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(coverImage.FileName)}";
            var path = Path.Combine(folder, fileName);

            using (var stream = new FileStream(path, FileMode.Create))
                await coverImage.CopyToAsync(stream);

            product.ProductImages.Add(new ProductImage
            {
                ProductId = productId,
                ImageUrl = fileName,
                IsCover = true,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            return Ok(new { message = "Cover image uploaded successfully." });
        }

        // =========================================================
        // POST: Upload Gallery Images (Multiple)
        // =========================================================
        [HttpPost("gallery")]
        public async Task<IActionResult> UploadGalleryImages(
            [FromForm] int productId,
            [FromForm] IFormFile[] images)
        {
            if (productId <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            if (images == null || images.Length == 0)
                return BadRequest(new { message = "At least one image is required." });

            var product = await _context.Products.FindAsync(productId);
            if (product == null)
                return NotFound(new { message = "Product not found." });

            var folder = GetProductImagePath();

            foreach (var file in images)
            {
                if (!IsValidImage(file))
                    return BadRequest(new { message = "One or more images are invalid." });

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var path = Path.Combine(folder, fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                    await file.CopyToAsync(stream);

                _context.ProductImages.Add(new ProductImage
                {
                    ProductId = productId,
                    ImageUrl = fileName,
                    IsCover = false,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Gallery images uploaded successfully." });
        }

        // =========================================================
        // GET: Product Images
        // =========================================================
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductImages(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return NotFound(new { message = "Product not found." });

            var dto = new ProductImageDTO
            {
                CoverImage = product.ProductImages
                    .FirstOrDefault(x => x.IsCover.GetValueOrDefault())?.ImageUrl,

                GalleryImages = product.ProductImages
                    .Where(x => !x.IsCover.GetValueOrDefault())
                    .Select(x => x.ImageUrl!)
                    .ToList()
            };

            return Ok(dto);
        }

        // =========================================================
        // PUT: Update Single Image
        // =========================================================
        [HttpPut("{imageId}")]
        public async Task<IActionResult> UpdateImage(
            int imageId,
            [FromForm] IFormFile file)
        {
            if (!IsValidImage(file))
                return BadRequest(new { message = "Invalid image." });

            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
                return NotFound(new { message = "Image not found." });

            var folder = GetProductImagePath();

            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var oldPath = Path.Combine(folder, image.ImageUrl);
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var newPath = Path.Combine(folder, newFileName);

            using (var stream = new FileStream(newPath, FileMode.Create))
                await file.CopyToAsync(stream);

            image.ImageUrl = newFileName;
            image.CreatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Image updated successfully." });
        }

        // =========================================================
        // DELETE: Single Image
        // =========================================================
        [HttpDelete("{imageId}")]
        public async Task<IActionResult> DeleteImage(int imageId)
        {
            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
                return NotFound(new { message = "Image not found." });

            var folder = GetProductImagePath();

            if (!string.IsNullOrEmpty(image.ImageUrl))
            {
                var path = Path.Combine(folder, image.ImageUrl);
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            _context.ProductImages.Remove(image);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Image deleted successfully." });
        }

        // =========================================================
        // DELETE: All Images of Product
        // =========================================================
        [HttpDelete("product/{productId}")]
        public async Task<IActionResult> DeleteAllImages(int productId)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return NotFound(new { message = "Product not found." });

            var folder = GetProductImagePath();

            foreach (var img in product.ProductImages)
            {
                if (!string.IsNullOrEmpty(img.ImageUrl))
                {
                    var path = Path.Combine(folder, img.ImageUrl);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }
            }

            _context.ProductImages.RemoveRange(product.ProductImages);
            await _context.SaveChangesAsync();

            return Ok(new { message = "All product images deleted successfully." });
        }
    }
}
