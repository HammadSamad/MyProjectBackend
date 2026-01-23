using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Backend_Api.Models.Model_Create;
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

        private bool IsValidImage(IFormFile file)
        {
            var ext = Path.GetExtension(file.FileName).ToLower();
            return _allowedExtensions.Contains(ext) && file.Length > 0 && file.Length <= _maxFileSize;
        }

        private string GetProductImagePath()
        {
            var root = _config["StoredFilesPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
            var path = Path.Combine(root, "Products");
            Directory.CreateDirectory(path);
            return path;
        }

        // =========================================================
        // POST: Upload Cover + Gallery Images
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> UploadImages([FromForm] CreateProductImage model)
        {
            if (model == null)
                return BadRequest(new { message = "Request body is empty." });

            if (model.ProductId <= 0)
                return BadRequest(new { message = "Invalid Product ID." });

            if (model.CoverImageUrl == null && (model.ImageUrl == null || model.ImageUrl.Length == 0))
                return BadRequest(new { message = "At least one image must be uploaded." });

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == model.ProductId);

            if (product == null)
                return NotFound(new { message = "Product not found." });

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var productFolder = GetProductImagePath();

                // -------- Cover Image --------
                if (model.CoverImageUrl != null)
                {
                    if (!IsValidImage(model.CoverImageUrl))
                        return BadRequest(new { message = "Cover image must be JPG, PNG, WEBP and ≤ 5MB." });

                    var oldCover = product.ProductImages.FirstOrDefault(x => x.IsCover.GetValueOrDefault());
                    if (oldCover != null)
                    {
                        var oldPath = Path.Combine(productFolder, oldCover.ImageUrl!);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);

                        _context.ProductImages.Remove(oldCover);
                    }

                    var coverFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.CoverImageUrl.FileName)}";
                    var coverPath = Path.Combine(productFolder, coverFileName);

                    using (var stream = new FileStream(coverPath, FileMode.Create))
                        await model.CoverImageUrl.CopyToAsync(stream);

                    product.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.ProductId,   // <-- explicitly set
                        ImageUrl = coverFileName,
                        IsCover = true,
                        CreatedAt = DateTime.UtcNow
                    });
                }

                // -------- Gallery Images --------
                if (model.ImageUrl != null)
                {
                    foreach (var file in model.ImageUrl)
                    {
                        if (!IsValidImage(file))
                            return BadRequest(new { message = "Gallery images must be JPG, PNG, WEBP and ≤ 5MB." });

                        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                        var filePath = Path.Combine(productFolder, fileName);

                        using (var stream = new FileStream(filePath, FileMode.Create))
                            await file.CopyToAsync(stream);

                        product.ProductImages.Add(new ProductImage
                        {
                            ProductId = product.ProductId,   // <-- explicitly set
                            ImageUrl = fileName,
                            IsCover = false,
                            CreatedAt = DateTime.UtcNow
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,
                    message = "Product images uploaded successfully."
                });
            }
            catch
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while uploading images. Please try again."
                });
            }
        }

        // =========================================================
        // GET: Product Images
        // =========================================================
        [HttpGet("product/{productId}")]
        public async Task<IActionResult> GetProductImages(int productId)
        {
            if (productId <= 0)
                return BadRequest(new { message = "Invalid product ID." });

            var product = await _context.Products
                .Include(p => p.ProductImages)
                .FirstOrDefaultAsync(p => p.ProductId == productId);

            if (product == null)
                return NotFound(new { message = "Product not found." });

            var dto = new ProductImageDTO
            {
                CoverImage = product.ProductImages.FirstOrDefault(x => x.IsCover.GetValueOrDefault())?.ImageUrl,
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
        public async Task<IActionResult> UpdateImage(int imageId, [FromForm] IFormFile file)
        {
            if (file == null)
                return BadRequest(new { message = "Image file is required." });

            if (!IsValidImage(file))
                return BadRequest(new { message = "Only JPG, PNG, WEBP images up to 5MB are allowed." });

            var image = await _context.ProductImages.FindAsync(imageId);
            if (image == null)
                return NotFound(new { message = "Image not found." });

            try
            {
                var productFolder = GetProductImagePath();

                var newFileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var newPath = Path.Combine(productFolder, newFileName);

                using (var stream = new FileStream(newPath, FileMode.Create))
                    await file.CopyToAsync(stream);

                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var oldPath = Path.Combine(productFolder, image.ImageUrl);
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                image.ImageUrl = newFileName;
                image.CreatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Image updated successfully." });
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to update image." });
            }
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

            try
            {
                var productFolder = GetProductImagePath();

                if (!string.IsNullOrEmpty(image.ImageUrl))
                {
                    var path = Path.Combine(productFolder, image.ImageUrl);
                    if (System.IO.File.Exists(path))
                        System.IO.File.Delete(path);
                }

                _context.ProductImages.Remove(image);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Image deleted successfully." });
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to delete image." });
            }
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

            try
            {
                var productFolder = GetProductImagePath();

                foreach (var img in product.ProductImages)
                {
                    if (!string.IsNullOrEmpty(img.ImageUrl))
                    {
                        var path = Path.Combine(productFolder, img.ImageUrl);
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                    }
                }

                _context.ProductImages.RemoveRange(product.ProductImages);
                await _context.SaveChangesAsync();

                return Ok(new { message = "All product images deleted successfully." });
            }
            catch
            {
                return StatusCode(500, new { message = "Failed to delete product images." });
            }
        }
    }
}
