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
    public class CartItemController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public CartItemController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE / ADD CART ITEM =================
        [HttpPost]
        public async Task<IActionResult> AddCartItem([FromBody] CreateCartItem model)
        {
            try
            {
                int modelQuantity = model.Quantity ?? 0;
                if (model == null || modelQuantity <= 0)
                    return BadRequest(new { message = "Invalid cart item data or quantity must be > 0." });

                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CartId == model.CartId);
                if (cart == null)
                    return NotFound(new { message = "Cart not found." });

                var variant = await _context.ProductVariants.FindAsync(model.VariantId);
                if (variant == null)
                    return NotFound(new { message = "Product variant not found." });

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci =>
                        ci.CartId == model.CartId &&
                        ci.VariantId == model.VariantId &&
                        ci.VariantSpecificationOptionsId == model.VariantSpecificationOptionsId);

                int existingQuantity = existingItem?.Quantity ?? 0;
                int finalQuantity = existingQuantity + modelQuantity;

                // Cap quantity to stock
                if (variant.Stock.HasValue && finalQuantity > variant.Stock.Value)
                    finalQuantity = variant.Stock.Value;

                if (existingItem != null)
                {
                    existingItem.Quantity = finalQuantity;
                    await _context.SaveChangesAsync();

                    var updatedItem = await GetCartItemDTO(existingItem.CartItemId);
                    return Ok(new
                    {
                        message = $"Cart item quantity updated successfully. Capped at available stock ({variant.Stock ?? 0}).",
                        data = updatedItem
                    });
                }

                // New cart item
                if (finalQuantity > 0)
                {
                    var cartItem = new CartItem
                    {
                        CartId = model.CartId,
                        VariantId = model.VariantId,
                        VariantSpecificationOptionsId = model.VariantSpecificationOptionsId,
                        Quantity = finalQuantity,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.CartItems.Add(cartItem);
                    await _context.SaveChangesAsync();

                    var newItem = await GetCartItemDTO(cartItem.CartItemId);
                    return Ok(new
                    {
                        message = $"Item added to cart successfully. Quantity capped at available stock ({variant.Stock ?? 0}).",
                        data = newItem
                    });
                }

                return BadRequest(new { message = "Cannot add zero quantity." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error adding item to cart.", error = ex.Message });
            }
        }

        // ================= GET ALL CART ITEMS =================
        [HttpGet]
        public async Task<IActionResult> GetAllCartItems()
        {
            try
            {
                var items = await _context.CartItems
                    .Select(ci => ci.CartItemId)
                    .ToListAsync();

                var itemsDto = new List<CartItemDTO>();
                foreach (var id in items)
                {
                    var dto = await GetCartItemDTO(id);
                    if (dto != null) itemsDto.Add(dto);
                }

                return Ok(itemsDto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching cart items.", error = ex.Message });
            }
        }

        // ================= GET CART ITEM BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCartItemById(int id)
        {
            try
            {
                var item = await GetCartItemDTO(id);
                if (item == null)
                    return NotFound(new { message = "Cart item not found." });

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching cart item.", error = ex.Message });
            }
        }

        // ================= GET ITEMS BY CART ID =================
        [HttpGet("cart/{cartId}")]
        public async Task<IActionResult> GetItemsByCartId(int cartId)
        {
            try
            {
                var items = await _context.CartItems
                    .Where(ci => ci.CartId == cartId)
                    .Select(ci => ci.CartItemId)
                    .ToListAsync();

                var itemsDto = new List<CartItemDTO>();
                foreach (var id in items)
                {
                    var dto = await GetCartItemDTO(id);
                    if (dto != null) itemsDto.Add(dto);
                }

                var subtotal = itemsDto.Sum(i => i.TotalPrice);
                var totalItems = itemsDto.Sum(i => i.Quantity);

                return Ok(new { message = "Cart fetched successfully.", subtotal, totalItems, items = itemsDto });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching cart items.", error = ex.Message });
            }
        }

        // ================= GET CART BY USER ID =================
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetCartByUserId(int userId)
        {
            try
            {
                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.UserId == userId);
                if (cart == null)
                {
                    cart = new Cart { UserId = userId, CreatedAt = DateTime.UtcNow };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                return await GetItemsByCartId(cart.CartId);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error fetching user cart.", error = ex.Message });
            }
        }

        // ================= UPDATE CART ITEM =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] UpdateCartItemQuantity model)
        {
            try
            {
                if (model == null || model.Quantity <= 0)
                    return BadRequest(new { message = "Quantity must be greater than 0." });

                var item = await _context.CartItems.FindAsync(id);
                if (item == null)
                    return NotFound(new { message = "Cart item not found." });

                var variant = await _context.ProductVariants.FindAsync(item.VariantId);
                if (variant == null)
                    return NotFound(new { message = "Product variant not found." });

                int finalQuantity = model.Quantity;

                // Stock validation
                if (variant.Stock.HasValue && finalQuantity > variant.Stock.Value)
                    finalQuantity = variant.Stock.Value;

                item.Quantity = finalQuantity;
                await _context.SaveChangesAsync();

                var updatedItem = await GetCartItemDTO(item.CartItemId);

                return Ok(new
                {
                    message = $"Cart item updated successfully. Quantity capped at stock ({variant.Stock ?? 0}).",
                    data = updatedItem
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error updating cart item.",
                    error = ex.Message
                });
            }
        }

        // ================= DELETE CART ITEM =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCartItem(int id)
        {
            try
            {
                var item = await _context.CartItems.FindAsync(id);
                if (item == null)
                    return NotFound(new { message = "Cart item not found." });

                _context.CartItems.Remove(item);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart item deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error deleting cart item.", error = ex.Message });
            }
        }

        // ================= CLEAR CART =================
        [HttpDelete("clear/{cartId}")]
        public async Task<IActionResult> ClearCart(int cartId)
        {
            try
            {
                var items = await _context.CartItems
                    .Where(ci => ci.CartId == cartId)
                    .ToListAsync();

                if (!items.Any())
                    return NotFound(new { message = "No items found in this cart." });

                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart cleared successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error clearing cart.", error = ex.Message });
            }
        }

        // ================= PRIVATE HELPER: FETCH CART ITEM DTO =================
        private async Task<CartItemDTO?> GetCartItemDTO(int cartItemId)
        {
            // Fetch cart item with variant, product, product images, and variant specification options + option + specification
            var item = await _context.CartItems
    .Include(ci => ci.Cart)
    .Include(ci => ci.Variant)
        .ThenInclude(v => v.Product)
            .ThenInclude(p => p.ProductImages)
    .Include(ci => ci.VariantSpecificationOptions)   // load SpecificationOption
        .ThenInclude(vso => vso.Specification)      // load Specification from SpecificationOption
    .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);


            if (item == null) return null;

            var variant = item.Variant;
            var product = variant.Product;

            // Get cover image
            string coverImage = product.ProductImages
                .OrderByDescending(pi => pi.IsCover ?? false)
                .Select(pi => pi.ImageUrl)
                .FirstOrDefault() ?? "";

            // Calculate final price considering discounts
            decimal finalPrice = variant.Price ?? 0;
            var now = DateTime.UtcNow;

            if ((!variant.DiscountStart.HasValue || variant.DiscountStart.Value <= now) &&
                (!variant.DiscountEnd.HasValue || variant.DiscountEnd.Value >= now))
            {
                if (variant.DiscountPercentage.HasValue && variant.DiscountPercentage.Value > 0)
                    finalPrice -= finalPrice * (variant.DiscountPercentage.Value / 100);

                if (variant.DiscountAmount.HasValue && variant.DiscountAmount.Value > 0)
                    finalPrice -= variant.DiscountAmount.Value;
            }

            if (finalPrice < 0) finalPrice = 0;

            // ================= MAP SPECIFICATIONS =================
            var specs = await _context.VariantSpecificationOptions
                .Include(vso => vso.Option)
                    .ThenInclude(o => o.Specification)
                .Where(vso => vso.VariantId == variant.VariantId &&
                              (vso.Option.Specification.SpecificationName == "RAM" ||
                               vso.Option.Specification.SpecificationName == "Storage" ||
                               vso.Option.Specification.SpecificationName == "Color"))
                .Select(vso => new VariantSpecificationOptionDTO
                {
                    OptionId = vso.OptionId,
                    SpecificationName = vso.Option.Specification.SpecificationName,
                    OptionValue = vso.Option.OptionValue
                })
                .ToListAsync();

            return new CartItemDTO
            {
                CartItemId = item.CartItemId,
                CartId = item.CartId,
                VariantId = item.VariantId,
                VariantSpecificationOptionsId = item.VariantSpecificationOptionsId,
                Quantity = item.Quantity ?? 0,
                UserId = item.Cart.UserId,
                Price = finalPrice,
                TotalPrice = finalPrice * (item.Quantity ?? 0),
                ProductName = product.ProductName ?? "",
                Image = coverImage,
                VariantSpecifications = specs,
                CreatedAt = item.CreatedAt
            };
        }


    }
}
