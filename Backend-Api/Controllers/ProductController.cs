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

        // =========================================================
        // GET ALL PRODUCTS (Search + Filter + Sort + Pagination)
        // =========================================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetProducts(
            string? search = null,
            int? categoryId = null,
            int? brandId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? specs = null,     // Example: "RAM:16GB,Storage:512GB"
            string? sort = null,      // price_asc, price_desc, newest, name
            int page = 1,
            int pageSize = 12)
        {
            var query = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                            .ThenInclude(o => o.Specification)
                .Include(p => p.ProductSpecificationValues)
                    .ThenInclude(psv => psv.Specification)
                .Include(p => p.ProductSpecificationValues)
                    .ThenInclude(psv => psv.Option)
                .AsQueryable();

            // 🔍 SEARCH
            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p =>
                    p.ProductName.Contains(search) ||
                    (p.Description ?? "").Contains(search));
            }

            // 🗂 CATEGORY FILTER
            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId.Value);

            // 🏷 BRAND FILTER
            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId.Value);

            // 💰 PRICE FILTER (based on variants)
            if (minPrice.HasValue)
                query = query.Where(p => p.ProductVariants.Any(v => v.Price >= minPrice.Value));

            if (maxPrice.HasValue)
                query = query.Where(p => p.ProductVariants.Any(v => v.Price <= maxPrice.Value));

            // 🧠 SPEC FILTER
            // Format: specs=RAM:16GB,Storage:512GB
            if (!string.IsNullOrEmpty(specs))
            {
                var filters = specs.Split(',');

                foreach (var f in filters)
                {
                    var parts = f.Split(':');
                    if (parts.Length != 2) continue;

                    var specName = parts[0].Trim();
                    var specValue = parts[1].Trim();

                    query = query.Where(p =>
                        // Product-level specs
                        p.ProductSpecificationValues.Any(psv =>
                            psv.Specification.SpecificationName == specName &&
                            (
                                (psv.ValueText != null && psv.ValueText == specValue) ||
                                (psv.Option != null && psv.Option.OptionValue == specValue)
                            )
                        )
                        ||
                        // Variant-level specs
                        p.ProductVariants.Any(v =>
                            v.VariantSpecificationOptions.Any(vso =>
                                vso.Option != null &&
                                vso.Option.Specification.SpecificationName == specName &&
                                vso.Option.OptionValue == specValue
                            )
                        )
                    );
                }
            }

            // ↕ SORTING
            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.ProductVariants.Min(v => v.Price)),
                "price_desc" => query.OrderByDescending(p => p.ProductVariants.Max(v => v.Price)),
                "newest" => query.OrderByDescending(p => p.CreatedAt),
                "name" => query.OrderBy(p => p.ProductName),
                _ => query.OrderByDescending(p => p.CreatedAt)
            };

            // 📄 PAGINATION
            var products = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(MapToDTO(products));
        }

        // =========================================================
        // GET ALL PRODUCTS (WITHOUT FILTERS, JUST ALL)
        // =========================================================
        [HttpGet("all")]
        public async Task<ActionResult<IEnumerable<ProductDTO>>> GetAllProducts()
        {
            var products = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                            .ThenInclude(o => o.Specification)
                .Include(p => p.ProductSpecificationValues)
                    .ThenInclude(psv => psv.Specification)
                .Include(p => p.ProductSpecificationValues)
                    .ThenInclude(psv => psv.Option)
                .ToListAsync();

            return Ok(MapToDTO(products));
        }

        // =========================================================
        // GET PRODUCT BY ID
        // =========================================================
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDTO>> GetProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                            .ThenInclude(o => o.Specification)
                .Include(p => p.ProductSpecificationValues)
                    .ThenInclude(psv => psv.Specification)
                .Include(p => p.ProductSpecificationValues)
                    .ThenInclude(psv => psv.Option)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound("Product not found");

            return Ok(MapToDTO(new List<Product> { product }).First());
        }

        // =========================================================
        // MAPPING METHOD
        // =========================================================
        private List<ProductDTO> MapToDTO(List<Product> products)
        {
            return products.Select(p => new ProductDTO
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName,
                Description = p.Description,
                WarrantyMonths = p.WarrantyMonths,
                IsActive = p.IsActive,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                BrandName = p.Brand?.BrandName,
                CategoryName = p.Category?.CategoryName,

                // Nullable bool fix: IsCover == true
                CoverImage = p.ProductImages
                    .FirstOrDefault(i => i.IsCover == true)?.ImageUrl,

                GalleryImages = p.ProductImages
                    .Where(i => i.IsCover != true)
                    .Select(i => i.ImageUrl!)
                    .ToList(),

                Variants = p.ProductVariants.Select(v => new ProductVariantDTO
                {
                    VariantId = v.VariantId,
                    Sku = v.Sku,
                    Price = v.Price,
                    Stock = v.Stock,
                    Specifications = v.VariantSpecificationOptions
                        .Where(vso => vso.Option != null)
                        .Select(vso => new VariantSpecificationOptionDTO
                        {
                            SpecificationName = vso.Option!.Specification.SpecificationName,
                            OptionValue = vso.Option.OptionValue
                        }).ToList()
                }).ToList(),

                Specifications = p.ProductSpecificationValues.Select(psv => new ProductSpecificationDTO
                {
                    SpecificationName = psv.Specification.SpecificationName,
                    DataType = psv.Specification.DataType,
                    ValueText = psv.ValueText,
                    ValueNumber = psv.ValueNumber,
                    ValueBool = psv.ValueBool ?? false,   // nullable bool fixed
                    OptionValue = psv.Option?.OptionValue
                }).ToList()

            }).ToList();
        }
    }
}
