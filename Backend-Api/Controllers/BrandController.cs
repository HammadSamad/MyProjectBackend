using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Route("api/[controller]")]
[ApiController]
public class BrandController : ControllerBase
{
    private readonly LaptopHarbourDbContext _context;

    public BrandController(LaptopHarbourDbContext context)
    {
        _context = context;
    }

    // -------------------------------------------------------------
    // GET: api/Brand → List all brands with products
    // -------------------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> GetBrands()
    {
        try
        {
            var brands = await _context.Brands
                .Include(b => b.Products)
                    .ThenInclude(p => p.ProductImages)
                .Include(b => b.Products)
                    .ThenInclude(p => p.ProductVariants)
                        .ThenInclude(v => v.VariantSpecificationOptions)
                            .ThenInclude(vso => vso.Option)
                .Include(b => b.Products)
                    .ThenInclude(p => p.ProductSpecificationValues)
                        .ThenInclude(psv => psv.Option)
                .Include(b => b.Products)
                    .ThenInclude(p => p.Category)
                .ToListAsync();

            if (brands.Count == 0)
                return Ok(new { message = "No brands found.", data = new List<BrandDTO>() });

            var dto = brands.Select(MapBrandToDTO).ToList();
            return Ok(dto);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while fetching brands.",
                error = ex.Message
            });
        }
    }

    // -------------------------------------------------------------
    // GET: api/Brand/5 → Single brand
    // -------------------------------------------------------------
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetBrand(int id)
    {
        try
        {
            var brand = await _context.Brands
                .Include(b => b.Products)
                    .ThenInclude(p => p.ProductImages)
                .Include(b => b.Products)
                    .ThenInclude(p => p.ProductVariants)
                        .ThenInclude(v => v.VariantSpecificationOptions)
                            .ThenInclude(vso => vso.Option)
                .Include(b => b.Products)
                    .ThenInclude(p => p.ProductSpecificationValues)
                        .ThenInclude(psv => psv.Option)
                .Include(b => b.Products)
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(b => b.BrandId == id);

            if (brand == null)
                return NotFound(new { message = "Brand not found." });

            return Ok(MapBrandToDTO(brand));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while fetching the brand.",
                error = ex.Message
            });
        }
    }

    // -------------------------------------------------------------
    // POST: api/Brand → Create brand
    // -------------------------------------------------------------
    [HttpPost]
    public async Task<IActionResult> CreateBrand([FromBody] CreateBrand model)
    {
        try
        {
            if (model == null || string.IsNullOrWhiteSpace(model.BrandName))
                return BadRequest(new { message = "BrandName is required." });

            bool exists = await _context.Brands
                .AnyAsync(b => b.BrandName!.ToLower() == model.BrandName.ToLower());

            if (exists)
                return BadRequest(new { message = "Brand with this name already exists." });

            var brand = new Brand
            {
                BrandName = model.BrandName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Brand created successfully.",
                brandId = brand.BrandId
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while creating the brand.",
                error = ex.Message
            });
        }
    }

    // -------------------------------------------------------------
    // PUT: api/Brand/5 → Update brand
    // -------------------------------------------------------------
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBrand(int id, [FromBody] CreateBrand model)
    {
        try
        {
            if (model == null || string.IsNullOrWhiteSpace(model.BrandName))
                return BadRequest(new { message = "BrandName is required." });

            var brand = await _context.Brands.FindAsync(id);
            if (brand == null)
                return NotFound(new { message = "Brand not found." });

            brand.BrandName = model.BrandName;
            brand.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Brand updated successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while updating the brand.",
                error = ex.Message
            });
        }
    }

    // -------------------------------------------------------------
    // DELETE: api/Brand/5 → Delete brand
    // -------------------------------------------------------------
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBrand(int id)
    {
        try
        {
            var brand = await _context.Brands
                .Include(b => b.Products)
                .ThenInclude(p => p.ProductImages)
                .FirstOrDefaultAsync(b => b.BrandId == id);

            if (brand == null)
                return NotFound(new { message = "Brand not found." });

            // Delete related product images + products
            if (brand.Products.Count > 0)
            {
                _context.Products.RemoveRange(brand.Products);
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Brand and all related products deleted successfully." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new
            {
                message = "An error occurred while deleting the brand.",
                error = ex.Message
            });
        }
    }

    // =============================================================
    // Helper method to map Brand → BrandDTO
    // =============================================================
    private BrandDTO MapBrandToDTO(Brand b)
    {
        return new BrandDTO
        {
            BrandId = b.BrandId,
            BrandName = b.BrandName,
            Products = b.Products.Select(p => new ProductDTO
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                WarrantyMonths = p.WarrantyMonths,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                BrandName = b.BrandName,
                CategoryName = p.Category?.CategoryName,
                CoverImage = p.ProductImages
                    .FirstOrDefault(img => img.IsCover == true)?.ImageUrl,

                GalleryImages = p.ProductImages
                    .Where(img => img.IsCover == false)
                    .Select(img => img.ImageUrl!)
                    .ToList(),

                Variants = p.ProductVariants.Select(v => new ProductVariantDTO
                {
                    VariantId = v.VariantId,
                    Sku = v.Sku,
                    Price = v.Price,
                    Stock = v.Stock,
                    Specifications = v.VariantSpecificationOptions.Select(vso => new VariantSpecificationOptionDTO
                    {
                        SpecificationName = vso.Option.Specification.SpecificationName,
                        OptionValue = vso.Option.OptionValue
                    }).ToList()
                }).ToList(),

                Specifications = p.ProductSpecificationValues.Select(psv => new ProductSpecificationDTO
                {
                    SpecificationName = psv.Specification.SpecificationName,
                    DataType = psv.Specification.DataType,
                    ValueText = psv.ValueText,
                    ValueNumber = psv.ValueNumber,
                    ValueBool = psv.ValueBool,
                    OptionValue = psv.Option?.OptionValue
                }).ToList()
            }).ToList()
        };
    }
}
