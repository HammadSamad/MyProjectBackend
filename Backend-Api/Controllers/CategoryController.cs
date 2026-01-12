using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IConfiguration _config;

        public CategoryController(LaptopHarbourDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // =========================================================
        // POST: api/Category → Create new category
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromForm] CreateCategory model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            string? imageName = null;

            // Image upload
            if (model.CategoryImage != null && model.CategoryImage.Length > 0)
            {
                var uploadRoot = _config["StoredFilesPath"] ?? Path.Combine("wwwroot", "uploads");
                var categoryFolder = Path.Combine(uploadRoot, "Categories");
                Directory.CreateDirectory(categoryFolder);

                var ext = Path.GetExtension(model.CategoryImage.FileName);
                imageName = Guid.NewGuid() + ext;
                var filePath = Path.Combine(categoryFolder, imageName);

                using var stream = System.IO.File.Create(filePath);
                await model.CategoryImage.CopyToAsync(stream);
            }

            var category = new Category
            {
                CategoryName = model.CategoryName,
                ParentCategoryId = model.ParentCategoryId,
                CategoryImage = imageName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category created successfully." });
        }

        // =========================================================
        // GET: api/Category → Get all categories
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories
                .Select(c => new CategoryDTO
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    CategoryImage = c.CategoryImage,
                    ParentCategoryId = c.ParentCategoryId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(categories);
        }

        // =========================================================
        // GET: api/Category/{id} → Get single category
        // =========================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _context.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new CategoryDTO
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    CategoryImage = c.CategoryImage,
                    ParentCategoryId = c.ParentCategoryId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound("Category not found.");

            return Ok(category);
        }

        // =========================================================
        // PUT: api/Category/{id} → Update category
        // =========================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] CreateCategory model)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound("Category not found.");

            category.CategoryName = model.CategoryName;
            category.ParentCategoryId = model.ParentCategoryId;

            // Update image if new image provided
            if (model.CategoryImage != null && model.CategoryImage.Length > 0)
            {
                var uploadRoot = _config["StoredFilesPath"] ?? Path.Combine("wwwroot", "uploads");
                var categoryFolder = Path.Combine(uploadRoot, "Categories");
                Directory.CreateDirectory(categoryFolder);

                // Delete old image
                if (!string.IsNullOrEmpty(category.CategoryImage))
                {
                    var oldFile = Path.Combine(categoryFolder, category.CategoryImage);
                    if (System.IO.File.Exists(oldFile))
                        System.IO.File.Delete(oldFile);
                }

                var ext = Path.GetExtension(model.CategoryImage.FileName);
                var newImageName = Guid.NewGuid() + ext;
                var filePath = Path.Combine(categoryFolder, newImageName);

                using var stream = System.IO.File.Create(filePath);
                await model.CategoryImage.CopyToAsync(stream);

                category.CategoryImage = newImageName;
            }

            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Category updated successfully." });
        }

        // =========================================================
        // DELETE: api/Category/{id} → Delete category
        // =========================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.InverseParentCategory)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound("Category not found.");

            // Optional: prevent deleting if subcategories exist
            if (category.InverseParentCategory.Any())
                return BadRequest("Cannot delete category because it has subcategories.");

            // Delete image
            if (!string.IsNullOrEmpty(category.CategoryImage))
            {
                var uploadRoot = _config["StoredFilesPath"] ?? Path.Combine("wwwroot", "uploads");
                var categoryFolder = Path.Combine(uploadRoot, "Categories");
                var path = Path.Combine(categoryFolder, category.CategoryImage);

                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted successfully." });
        }
    }
}
