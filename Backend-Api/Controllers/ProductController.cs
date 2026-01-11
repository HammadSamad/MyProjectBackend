using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public ProductsController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // GET: api/Products
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetProducts()
        {
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                .Include(p => p.ProductSpecificationValues)
                    .ThenInclude(psv => psv.Option)
                .ToListAsync();

            var productDTOs = products.Select(p => new ProductDTO
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                WarrantyMonths = p.WarrantyMonths,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                BrandName = p.Brand.BrandName,
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
            }).ToList();

            return Ok(productDTOs);
        }

        // GET: api/Products/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDTO>> GetProduct(int id)
        {
            var p = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                .Include(p => p.ProductSpecificationValues)
                    .ThenInclude(psv => psv.Option)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (p == null) return NotFound();

            var dto = new ProductDTO
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                WarrantyMonths = p.WarrantyMonths,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                BrandName = p.Brand.BrandName,
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
            };

            return Ok(dto);
        }

        // POST: api/Products
        [HttpPost]
        public async Task<ActionResult<ProductDTO>> CreateProduct([FromBody] CreateProduct model)
        {
            var product = new Product
            {
                ProductName = model.ProductName,
                Description = model.Description,
                CategoryId = model.CategoryId,
                BrandId = model.BrandId,
                WarrantyMonths = model.WarrantyMonths,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return await GetProduct(product.ProductId);
        }

        // PUT: api/Products/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] CreateProduct model)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.ProductName = model.ProductName;
            product.Description = model.Description;
            product.CategoryId = model.CategoryId;
            product.BrandId = model.BrandId;
            product.WarrantyMonths = model.WarrantyMonths;
            product.IsActive = model.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            _context.Products.Update(product);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/Products/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return NotFound();

            _context.ProductImages.RemoveRange(product.ProductImages);
            _context.ProductVariants.RemoveRange(product.ProductVariants);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
