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
    public class CategoryController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IWebHostEnvironment _env;

        public CategoryController(LaptopHarbourDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private string GetCategoryImagePath()
        {
            var path = Path.Combine(_env.WebRootPath, "upload", "categories");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private string GetImageUrl(string? fileName)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            if (string.IsNullOrEmpty(fileName))
                return $"{baseUrl}/assets/categories/default-category.jpg"; // fallback image

            return $"{baseUrl}/upload/categories/{fileName}";
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromForm] CreateCategory model)
        {
            if (string.IsNullOrWhiteSpace(model.CategoryName))
                return BadRequest(new { message = "Category name is required." });

            bool exists = await _context.Categories.AnyAsync(c =>
                c.ParentCategoryId == model.ParentCategoryId &&
                c.CategoryName!.ToLower() == model.CategoryName.ToLower());

            if (exists)
                return BadRequest(new { message = "Category already exists at this level." });

            string? imageName = null;

            if (model.CategoryImage != null)
            {
                var folder = GetCategoryImagePath();
                imageName = $"{Guid.NewGuid()}{Path.GetExtension(model.CategoryImage.FileName)}";
                var filePath = Path.Combine(folder, imageName);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.CategoryImage.CopyToAsync(stream);
            }

            var category = new Category
            {
                CategoryName = model.CategoryName.Trim(),
                ParentCategoryId = model.ParentCategoryId,
                CategoryImage = imageName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category created successfully." });
        }

        // ================= GET TREE =================
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _context.Categories.AsNoTracking().ToListAsync();

            List<CategoryDTO> BuildTree(int? parentId)
            {
                return categories
                    .Where(c => c.ParentCategoryId == parentId)
                    .Select(c => new CategoryDTO
                    {
                        CategoryId = c.CategoryId,
                        CategoryName = c.CategoryName,
                        CategoryImage = GetImageUrl(c.CategoryImage),
                        ParentCategoryId = c.ParentCategoryId,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt,
                        Subcategories = BuildTree(c.CategoryId)
                    })
                    .ToList();
            }

            return Ok(BuildTree(null));
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var category = await _context.Categories
                .Where(c => c.CategoryId == id)
                .Select(c => new CategoryDTO
                {
                    CategoryId = c.CategoryId,
                    CategoryName = c.CategoryName,
                    CategoryImage = GetImageUrl(c.CategoryImage),
                    ParentCategoryId = c.ParentCategoryId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (category == null)
                return NotFound(new { message = "Category not found." });

            return Ok(category);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] CreateCategory model)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
                return NotFound(new { message = "Category not found." });

            bool exists = await _context.Categories.AnyAsync(c =>
                c.CategoryId != id &&
                c.ParentCategoryId == model.ParentCategoryId &&
                c.CategoryName!.ToLower() == model.CategoryName!.ToLower());

            if (exists)
                return BadRequest(new { message = "Duplicate category at this level." });

            category.CategoryName = model.CategoryName!.Trim();
            category.ParentCategoryId = model.ParentCategoryId;

            if (model.CategoryImage != null)
            {
                var folder = GetCategoryImagePath();

                if (!string.IsNullOrEmpty(category.CategoryImage))
                {
                    var oldFile = Path.Combine(folder, category.CategoryImage);
                    if (System.IO.File.Exists(oldFile))
                        System.IO.File.Delete(oldFile);
                }

                var newImage = $"{Guid.NewGuid()}{Path.GetExtension(model.CategoryImage.FileName)}";
                var filePath = Path.Combine(folder, newImage);

                using var stream = new FileStream(filePath, FileMode.Create);
                await model.CategoryImage.CopyToAsync(stream);

                category.CategoryImage = newImage;
            }

            category.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category updated successfully." });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await _context.Categories
                .Include(c => c.InverseParentCategory)
                .FirstOrDefaultAsync(c => c.CategoryId == id);

            if (category == null)
                return NotFound(new { message = "Category not found." });

            if (category.InverseParentCategory.Any())
                return BadRequest(new { message = "Cannot delete category with subcategories." });

            if (!string.IsNullOrEmpty(category.CategoryImage))
            {
                var folder = GetCategoryImagePath();
                var file = Path.Combine(folder, category.CategoryImage);
                if (System.IO.File.Exists(file))
                    System.IO.File.Delete(file);
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Category deleted successfully." });
        }
    }
}