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
    public class ProductsController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;
        private readonly IWebHostEnvironment _env;

        public ProductsController(LaptopHarbourDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // =========================================================
        // GET ALL PRODUCTS (Search + Filter + Sort + Pagination)
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetProducts(
            string? search = null,
            int? categoryId = null,
            int? brandId = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            string? specs = null,
            string? sort = null,
            bool? discountedOnly = null,
            int page = 1,
            int pageSize = 12)
        {
            try
            {
                if (page <= 0 || pageSize <= 0)
                    return BadRequest(new { error = "Page and PageSize must be greater than 0." });

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
                    .Include(p => p.ProductReviews)
                    .AsQueryable();

                // DB-safe filters
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim();
                    query = query.Where(p =>
                        EF.Functions.Like(p.ProductName ?? "", $"%{search}%") ||
                        EF.Functions.Like(p.Description ?? "", $"%{search}%"));
                }

                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId.Value);

                if (brandId.HasValue)
                    query = query.Where(p => p.BrandId == brandId.Value);

                // Load data into memory for in-memory filtering
                var products = await query.ToListAsync();

                // PRICE FILTER
                if (minPrice.HasValue)
                    products = products.Where(p => p.ProductVariants.Any(v => CalculateFinalPrice(v) >= minPrice.Value)).ToList();

                if (maxPrice.HasValue)
                    products = products.Where(p => p.ProductVariants.Any(v => CalculateFinalPrice(v) <= maxPrice.Value)).ToList();

                // DISCOUNTED ONLY FILTER
                if (discountedOnly == true)
                {
                    var now = DateTime.UtcNow;
                    products = products.Where(p =>
                        p.ProductVariants.Any(v =>
                            ((v.DiscountPercentage ?? 0) > 0 || (v.DiscountAmount ?? 0) > 0) &&
                            (!v.DiscountStart.HasValue || v.DiscountStart <= now) &&
                            (!v.DiscountEnd.HasValue || v.DiscountEnd >= now)
                        )).ToList();
                }

                // SPECIFICATION FILTER
                if (!string.IsNullOrWhiteSpace(specs))
                {
                    var filters = specs.Split(',');
                    foreach (var f in filters)
                    {
                        var parts = f.Split(':');
                        if (parts.Length != 2) continue;

                        var specName = parts[0].Trim();
                        var specValue = parts[1].Trim();

                        products = products.Where(p =>
                            p.ProductSpecificationValues.Any(psv =>
                                psv.Specification.SpecificationName == specName &&
                                ((psv.ValueText != null && psv.ValueText == specValue) ||
                                 (psv.Option != null && psv.Option.OptionValue == specValue))
                            )
                            || p.ProductVariants.Any(v =>
                                v.VariantSpecificationOptions.Any(vso =>
                                    vso.Option != null &&
                                    vso.Option.Specification.SpecificationName == specName &&
                                    vso.Option.OptionValue == specValue
                                ))
                        ).ToList();
                    }
                }

                // SORTING
                products = sort switch
                {
                    "price_asc" => products
                        .OrderBy(p => p.ProductVariants.Any() ? p.ProductVariants.Min(v => CalculateFinalPrice(v)) : decimal.MaxValue)
                        .ToList(),

                    "price_desc" => products
                        .OrderByDescending(p => p.ProductVariants.Any() ? p.ProductVariants.Max(v => CalculateFinalPrice(v)) : 0)
                        .ToList(),

                    "newest" => products.OrderByDescending(p => p.CreatedAt).ToList(),
                    "name" => products.OrderBy(p => p.ProductName).ToList(),
                    _ => products.OrderByDescending(p => p.CreatedAt).ToList()
                };

                var totalRecords = products.Count;

                // PAGINATION
                products = products.Skip((page - 1) * pageSize).Take(pageSize).ToList();

                if (!products.Any())
                    return NotFound(new { error = "No products found with given filters." });

                return Ok(new
                {
                    message = "Products fetched successfully",
                    totalRecords,
                    page,
                    pageSize,
                    data = MapToDTO(products)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        // =========================================================
        // GET PRODUCTS FOR DISPLAY (Min Price Variant)
        // Only: Image, Name, Min Price, Average Rating
        // =========================================================
        [HttpGet("display")]
        public async Task<IActionResult> GetProductsForDisplay()
        {
            try
            {
                var products = await _context.Products
                    .Where(p => p.IsActive == true)
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                    .Include(p => p.ProductReviews)
                    .ToListAsync();

                var result = products.Select(p =>
                {
                    // Get cheapest variant by final discounted price
                    var cheapestVariant = p.ProductVariants
                        .OrderBy(v => CalculateFinalPrice(v))
                        .FirstOrDefault();

                    decimal originalPrice = cheapestVariant?.Price ?? 0;
                    decimal discountPrice = cheapestVariant != null
                        ? CalculateFinalPrice(cheapestVariant)
                        : 0;

                    bool isDiscounted = discountPrice < originalPrice;

                    // Calculate effective discount percentage
                    decimal discountPercentage = 0;
                    if (cheapestVariant != null && originalPrice > 0)
                    {
                        discountPercentage = ((originalPrice - discountPrice) / originalPrice) * 100;
                        discountPercentage = Math.Round(discountPercentage, 2);
                    }

                    // ✅ DECLARE BEFORE RETURN
                    var coverFile = p.ProductImages
                        .FirstOrDefault(i => i.IsCover == true)?.ImageUrl
                        ?? p.ProductImages.FirstOrDefault()?.ImageUrl;

                    return new ProductDisplayDTO
                    {
                        ProductId = p.ProductId,
                        VariantId = cheapestVariant?.VariantId,
                        ProductName = p.ProductName,

                        // ✅ USE HERE
                        ProductImage = GetImageUrl(coverFile),

                        OriginalPrice = originalPrice,
                        DiscountPrice = discountPrice,
                        DiscountPercentage = discountPercentage,
                        IsDiscounted = isDiscounted,

                        AverageRating = p.ProductReviews.Any()
                            ? Math.Round(p.ProductReviews.Average(r => r.Rating ?? 0), 1)
                            : 0
                    };
                }).ToList();

                if (!result.Any())
                    return NotFound(new { message = "No products available for display." });

                return Ok(new
                {
                    message = "Display products fetched successfully",
                    total = result.Count,
                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error while fetching display products",
                    details = ex.Message
                });
            }
        }




        // =========================================================
        // GET PRODUCT BY ID
        // =========================================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Brand)
                    .Include(p => p.Category)
                    .Include(p => p.ProductImages)

                    // Variants + Variant Specifications
                    .Include(p => p.ProductVariants)
                        .ThenInclude(v => v.VariantSpecificationOptions)
                            .ThenInclude(vso => vso.Option)
                                .ThenInclude(o => o.Specification)

                    // Product level Specifications (IMPORTANT: add Option include)
                    .Include(p => p.ProductSpecificationValues)
                        .ThenInclude(psv => psv.Specification)
                    .Include(p => p.ProductSpecificationValues)
                        .ThenInclude(psv => psv.Option)

                    .Include(p => p.ProductReviews)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                    return NotFound(new { error = "Product not found." });

                var dto = MapToDTO(new List<Product> { product }).First();

                return Ok(new
                {
                    message = "Product fetched successfully",
                    data = dto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred while fetching the product.",
                    details = ex.Message
                });
            }
        }


        // =========================================================
        // CREATE PRODUCT
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProduct model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

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

            return Ok(new { message = "Product created successfully", productId = product.ProductId });
        }

        // =========================================================
        // UPDATE PRODUCT
        // =========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] CreateProduct model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound(new { error = "Product not found." });

            product.ProductName = model.ProductName;
            product.Description = model.Description;
            product.CategoryId = model.CategoryId;
            product.BrandId = model.BrandId;
            product.WarrantyMonths = model.WarrantyMonths;
            product.IsActive = model.IsActive;
            product.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Product updated successfully." });
        }

        // =========================================================
        // DELETE PRODUCT
        // =========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.ProductImages)
                .Include(p => p.ProductVariants)
                    .ThenInclude(v => v.OrderItems)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null)
                return NotFound(new { error = "Product not found." });

            if (product.ProductVariants.Any(v => v.OrderItems.Any()))
                return BadRequest(new { error = "Cannot delete product with orders." });

            _context.ProductImages.RemoveRange(product.ProductImages);
            _context.ProductVariants.RemoveRange(product.ProductVariants);
            _context.Products.Remove(product);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Product deleted successfully." });
        }

        // =========================================================
        // DTO MAPPING
        // =========================================================
        private List<ProductDTO> MapToDTO(List<Product> products)
        {
            return products.Select(p =>
            {
                // 🔥 Find cheapest variant for this product
                var cheapestVariant = p.ProductVariants
                    .OrderBy(v => CalculateFinalPrice(v))
                    .FirstOrDefault();

                // ✅ Get cover image before the object initializer
                var coverFile = p.ProductImages
                    .FirstOrDefault(i => i.IsCover.GetValueOrDefault())?.ImageUrl;

                return new ProductDTO
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

                    CoverImage = GetImageUrl(coverFile),

                    GalleryImages = p.ProductImages
                        .Where(i => !i.IsCover.GetValueOrDefault())
                        .Select(i => GetImageUrl(i.ImageUrl))
                        .Where(url => url != null)
                        .ToList()!,

                    // ✅ Minimum price information
                    MinPrice = cheapestVariant != null
                        ? CalculateFinalPrice(cheapestVariant)
                        : 0,

                    MinPriceVariantId = cheapestVariant?.VariantId,

                    Variants = p.ProductVariants.Select(v => new ProductVariantDTO
                    {
                        VariantId = v.VariantId,
                        Sku = v.Sku,
                        Price = v.Price,
                        FinalPrice = CalculateFinalPrice(v),
                        Stock = v.Stock,
                        DiscountPercentage = v.DiscountPercentage,
                        DiscountAmount = v.DiscountAmount,
                        DiscountStart = v.DiscountStart,
                        DiscountEnd = v.DiscountEnd,

                        VariantSpecifications = v.VariantSpecificationOptions
                            .Select(vso => new VariantSpecificationOptionDTO
                            {
                                OptionId = vso.OptionId,
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
                        OptionValue = psv.Option != null ? psv.Option.OptionValue : null
                    }).ToList(),

                    AverageRating = p.ProductReviews.Any()
                        ? Math.Round(p.ProductReviews.Average(r => r.Rating ?? 0), 1)
                        : 0
                };
            }).ToList();
        }

        // =========================================================
        // BUILD FULL IMAGE URL
        // =========================================================
        private string? GetImageUrl(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return null;

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            return $"{baseUrl}/upload/Products/{fileName}";
        }


        // =========================================================
        // FINAL PRICE CALCULATOR
        // =========================================================
        private decimal CalculateFinalPrice(ProductVariant v)
        {
            decimal price = v.Price ?? 0;
            var now = DateTime.UtcNow;

            if (v.DiscountStart.HasValue && now < v.DiscountStart) return price;
            if (v.DiscountEnd.HasValue && now > v.DiscountEnd) return price;

            if (v.DiscountPercentage.HasValue && v.DiscountPercentage > 0)
                price -= price * (v.DiscountPercentage.Value / 100);

            if (v.DiscountAmount.HasValue && v.DiscountAmount > 0)
                price -= v.DiscountAmount.Value;

            return price < 0 ? 0 : price;
        }
    }
}
