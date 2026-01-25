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
    bool? discountedOnly = null, // <-- NEW PARAMETER
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

                // 🔍 SEARCH
                if (!string.IsNullOrWhiteSpace(search))
                {
                    search = search.Trim();
                    query = query.Where(p =>
                        EF.Functions.Like(p.ProductName ?? "", $"%{search}%") ||
                        EF.Functions.Like(p.Description ?? "", $"%{search}%"));
                }

                // 🗂 CATEGORY FILTER
                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId.Value);

                // 🏷 BRAND FILTER
                if (brandId.HasValue)
                    query = query.Where(p => p.BrandId == brandId.Value);

                // 💰 PRICE FILTER
                if (minPrice.HasValue)
                    query = query.Where(p => p.ProductVariants.Any(v => CalculateFinalPrice(v) >= minPrice.Value));

                if (maxPrice.HasValue)
                    query = query.Where(p => p.ProductVariants.Any(v => CalculateFinalPrice(v) <= maxPrice.Value));

                // 💸 DISCOUNTED ONLY FILTER
                if (discountedOnly.HasValue && discountedOnly.Value)
                {
                    var now = DateTime.UtcNow;
                    query = query.Where(p => p.ProductVariants.Any(v =>
                        (v.DiscountPercentage.HasValue && v.DiscountPercentage.Value > 0 || v.DiscountAmount.HasValue && v.DiscountAmount.Value > 0) &&
                        (!v.DiscountStart.HasValue || v.DiscountStart <= now) &&
                        (!v.DiscountEnd.HasValue || v.DiscountEnd >= now)
                    ));
                }

                // 🧠 SPECIFICATION FILTER
                if (!string.IsNullOrWhiteSpace(specs))
                {
                    var filters = specs.Split(',');

                    foreach (var f in filters)
                    {
                        var parts = f.Split(':');
                        if (parts.Length != 2) continue;

                        var specName = parts[0].Trim();
                        var specValue = parts[1].Trim();

                        query = query.Where(p =>
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
                        );
                    }
                }

                // ↕ SORTING
                query = sort switch
                {
                    "price_asc" => query.OrderBy(p => p.ProductVariants.Min(v => CalculateFinalPrice(v))),
                    "price_desc" => query.OrderByDescending(p => p.ProductVariants.Max(v => CalculateFinalPrice(v))),
                    "newest" => query.OrderByDescending(p => p.CreatedAt),
                    "name" => query.OrderBy(p => p.ProductName),
                    _ => query.OrderByDescending(p => p.CreatedAt)
                };

                var totalRecords = await query.CountAsync();

                var products = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

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
                return StatusCode(500, new
                {
                    error = "An error occurred while fetching products.",
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
                    .Include(p => p.ProductVariants)
                        .ThenInclude(v => v.VariantSpecificationOptions)
                            .ThenInclude(vso => vso.Option)
                                .ThenInclude(o => o.Specification)
                    .Include(p => p.ProductSpecificationValues)
                        .ThenInclude(psv => psv.Specification)
                    .Include(p => p.ProductSpecificationValues)
                        .ThenInclude(psv => psv.Option)
                    .Include(p => p.ProductReviews)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                    return NotFound(new { error = "Product not found." });

                return Ok(new
                {
                    message = "Product fetched successfully",
                    data = MapToDTO(new List<Product> { product }).First()
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
            try
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

                return Ok(new
                {
                    message = "Product created successfully.",
                    productId = product.ProductId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred while creating product.",
                    details = ex.Message
                });
            }
        }

        // =========================================================
        // UPDATE PRODUCT
        // =========================================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] CreateProduct model)
        {
            try
            {
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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred while updating product.",
                    details = ex.Message
                });
            }
        }

        // =========================================================
        // DELETE PRODUCT
        // =========================================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductImages)
                    .Include(p => p.ProductVariants)
                        .ThenInclude(v => v.OrderItems)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                    return NotFound(new { error = "Product not found." });

                bool hasOrders = product.ProductVariants.Any(v => v.OrderItems.Any());
                if (hasOrders)
                    return BadRequest(new { error = "Cannot delete this product because it has associated orders." });

                // Delete images from disk
                foreach (var img in product.ProductImages)
                {
                    if (!string.IsNullOrEmpty(img.ImageUrl))
                    {
                        var path = Path.Combine(_env.WebRootPath, "upload", "Products", img.ImageUrl);
                        if (System.IO.File.Exists(path))
                            System.IO.File.Delete(path);
                    }
                }

                _context.ProductImages.RemoveRange(product.ProductImages);
                _context.ProductVariants.RemoveRange(product.ProductVariants);
                _context.Products.Remove(product);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Product and related data deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = "An error occurred while deleting product.",
                    details = ex.Message
                });
            }
        }

        // =========================================================
        // DTO MAPPING WITH AverageRating & FinalPrice
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

                CoverImage = p.ProductImages.FirstOrDefault(i => i.IsCover == true)?.ImageUrl,
                GalleryImages = p.ProductImages.Where(i => i.IsCover != true).Select(i => i.ImageUrl!).ToList(),

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
                    ValueBool = psv.ValueBool ?? false,
                    OptionValue = psv.Option?.OptionValue
                }).ToList(),

                AverageRating = p.ProductReviews.Any()
                    ? Math.Round(p.ProductReviews.Average(r => r.Rating ?? 0), 1)
                    : 0
            }).ToList();
        }

        // =========================================================
        // Helper → Calculate Final Price considering discount
        // =========================================================
        private decimal CalculateFinalPrice(ProductVariant v)
        {
            decimal price = v.Price ?? 0;

            // Check discount date validity
            if (v.DiscountStart.HasValue && v.DiscountEnd.HasValue)
            {
                var now = DateTime.UtcNow;
                if (now < v.DiscountStart || now > v.DiscountEnd) return price;
            }

            // Apply percentage
            if (v.DiscountPercentage.HasValue)
                price -= price * (v.DiscountPercentage.Value / 100);

            // Apply fixed discount
            if (v.DiscountAmount.HasValue)
                price -= v.DiscountAmount.Value;

            return price >= 0 ? price : 0;
        }
    }
}
