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
    public class BrandController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public BrandController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // -------------------------------------------------------------
        // GET: api/Brands → List all brands with products
        // -------------------------------------------------------------
        [HttpGet]
        public async Task<ActionResult<IEnumerable<BrandDTO>>> GetBrands()
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
                .ToListAsync();

            var dto = brands.Select(b => new BrandDTO
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
                    CategoryName = p.Category.CategoryName,
                    CoverImage = p.ProductImages.FirstOrDefault(img => img.IsCover == true)?.ImageUrl,
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
            }).ToList();

            return Ok(dto);
        }

        // -------------------------------------------------------------
        // GET: api/Brands/5 → Single brand
        // -------------------------------------------------------------
        [HttpGet("{id}")]
        public async Task<ActionResult<BrandDTO>> GetBrand(int id)
        {
            var b = await _context.Brands
                .Include(b => b.Products)
                    .ThenInclude(p => p.ProductImages)
                .Include(b => b.Products)
                    .ThenInclude(p => p.ProductVariants)
                        .ThenInclude(v => v.VariantSpecificationOptions)
                            .ThenInclude(vso => vso.Option)
                .Include(b => b.Products)
                    .ThenInclude(p => p.ProductSpecificationValues)
                        .ThenInclude(psv => psv.Option)
                .FirstOrDefaultAsync(b => b.BrandId == id);

            if (b == null) return NotFound();

            var dto = new BrandDTO
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
                    CategoryName = p.Category.CategoryName,
                    CoverImage = p.ProductImages.FirstOrDefault(img => img.IsCover == true)?.ImageUrl,
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

            return Ok(dto);
        }

        // -------------------------------------------------------------
        // POST: api/Brands → Create brand
        // -------------------------------------------------------------
        [HttpPost]
        public async Task<ActionResult<BrandDTO>> CreateBrand([FromBody] CreateBrand model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.BrandName))
                return BadRequest("BrandName is required.");

            var brand = new Brand
            {
                BrandName = model.BrandName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Brands.Add(brand);
            await _context.SaveChangesAsync();

            return await GetBrand(brand.BrandId);
        }

        // -------------------------------------------------------------
        // PUT: api/Brands/5 → Update brand
        // -------------------------------------------------------------
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBrand(int id, [FromBody] CreateBrand model)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand == null) return NotFound();

            brand.BrandName = model.BrandName ?? brand.BrandName;
            brand.UpdatedAt = DateTime.UtcNow;

            _context.Brands.Update(brand);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // -------------------------------------------------------------
        // DELETE: api/Brands/5 → Delete brand
        // -------------------------------------------------------------
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBrand(int id)
        {
            var brand = await _context.Brands
                .Include(b => b.Products)
                .FirstOrDefaultAsync(b => b.BrandId == id);

            if (brand == null) return NotFound();

            // Optional: Delete all products under this brand
            if (brand.Products.Any())
            {
                _context.Products.RemoveRange(brand.Products);
            }

            _context.Brands.Remove(brand);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
