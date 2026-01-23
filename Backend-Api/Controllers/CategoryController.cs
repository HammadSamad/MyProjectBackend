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
        private readonly IConfiguration _config;

        public CategoryController(LaptopHarbourDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // =========================================================
        // POST: api/Category → Create new category (No duplicates)
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CreateCategory([FromForm] CreateCategory model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.CategoryName))
                    return BadRequest(new { message = "Category name is required." });

                // Duplicate check (same parent, same name)
                bool exists = await _context.Categories.AnyAsync(c =>
                    c.ParentCategoryId == model.ParentCategoryId &&
                    string.Equals(c.CategoryName, model.CategoryName, StringComparison.OrdinalIgnoreCase)
                );

                if (exists)
                    return BadRequest(new { message = "Category with the same name already exists in this level." });

                string? imageName = null;

                if (model.CategoryImage != null && model.CategoryImage.Length > 0)
                {
                    var uploadRoot = _config["StoredFilesPath"] ?? Path.Combine("wwwroot", "uploads");
                    var categoryFolder = Path.Combine(uploadRoot, "Categories");
                    Directory.CreateDirectory(categoryFolder);

                    var ext = Path.GetExtension(model.CategoryImage.FileName);
                    imageName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(categoryFolder, imageName);

                    using var stream = System.IO.File.Create(filePath);
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
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error creating category.", error = ex.Message });
            }
        }

        // =========================================================
        // GET: api/Category → Get all categories as TREE
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .AsNoTracking()
                    .ToListAsync();

                List<CategoryDTO> BuildTree(int? parentId)
                {
                    return categories
                        .Where(c => c.ParentCategoryId == parentId)
                        .Select(c => new CategoryDTO
                        {
                            CategoryId = c.CategoryId,
                            CategoryName = c.CategoryName,
                            CategoryImage = c.CategoryImage,
                            ParentCategoryId = c.ParentCategoryId,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt,
                            Subcategories = BuildTree(c.CategoryId)
                        })
                        .ToList();
                }

                var tree = BuildTree(null);
                return Ok(tree);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching categories.", error = ex.Message });
            }
        }

        // =========================================================
        // GET: api/Category/{id} → Get single category
        // =========================================================
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            try
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
                    return NotFound(new { message = "Category not found." });

                return Ok(category);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching category.", error = ex.Message });
            }
        }

        // =========================================================
        // PUT: api/Category/{id} → Update category (No duplicates)
        // =========================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateCategory(int id, [FromForm] CreateCategory model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.CategoryName))
                    return BadRequest(new { message = "Category name is required." });

                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                    return NotFound(new { message = "Category not found." });

                // Duplicate check (exclude current category)
                bool exists = await _context.Categories.AnyAsync(c =>
                    c.CategoryId != id &&
                    c.ParentCategoryId == model.ParentCategoryId &&
                    string.Equals(c.CategoryName, model.CategoryName, StringComparison.OrdinalIgnoreCase)
                );

                if (exists)
                    return BadRequest(new { message = "Another category with the same name already exists in this level." });

                category.CategoryName = model.CategoryName.Trim();
                category.ParentCategoryId = model.ParentCategoryId;

                if (model.CategoryImage != null && model.CategoryImage.Length > 0)
                {
                    var uploadRoot = _config["StoredFilesPath"] ?? Path.Combine("wwwroot", "uploads");
                    var categoryFolder = Path.Combine(uploadRoot, "Categories");
                    Directory.CreateDirectory(categoryFolder);

                    // Delete old image
                    if (!string.IsNullOrEmpty(category.CategoryImage))
                    {
                        var oldPath = Path.Combine(categoryFolder, category.CategoryImage);
                        if (System.IO.File.Exists(oldPath))
                            System.IO.File.Delete(oldPath);
                    }

                    var ext = Path.GetExtension(model.CategoryImage.FileName);
                    var newImageName = $"{Guid.NewGuid()}{ext}";
                    var filePath = Path.Combine(categoryFolder, newImageName);

                    using var stream = System.IO.File.Create(filePath);
                    await model.CategoryImage.CopyToAsync(stream);

                    category.CategoryImage = newImageName;
                }

                category.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Category updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating category.", error = ex.Message });
            }
        }

        // =========================================================
        // DELETE: api/Category/{id} → Delete category + image
        // =========================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.InverseParentCategory)
                    .FirstOrDefaultAsync(c => c.CategoryId == id);

                if (category == null)
                    return NotFound(new { message = "Category not found." });

                if (category.InverseParentCategory.Count > 0)
                    return BadRequest(new { message = "Cannot delete category because it has subcategories." });

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

                return Ok(new { message = "Category and its image deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting category.", error = ex.Message });
            }
        }
    }
}
