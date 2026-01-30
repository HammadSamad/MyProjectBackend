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
                if (model == null || model.Quantity.GetValueOrDefault() <= 0 || !model.VariantSpecificationOptionIds.Any())
                    return BadRequest(new { message = "Invalid cart item data, quantity must be > 0, and at least one spec must be selected." });

                var cart = await _context.Carts.FirstOrDefaultAsync(c => c.CartId == model.CartId);
                if (cart == null) return NotFound(new { message = "Cart not found." });

                var variant = await _context.ProductVariants.FindAsync(model.VariantId);
                if (variant == null) return NotFound(new { message = "Product variant not found." });

                int finalQuantity = model.Quantity.GetValueOrDefault();
                if (variant.Stock.HasValue && finalQuantity > variant.Stock.Value)
                    finalQuantity = variant.Stock.Value;

                // Create cart item
                var cartItem = new CartItem
                {
                    CartId = model.CartId,
                    VariantId = model.VariantId,
                    Quantity = finalQuantity,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync();

                // Save selected spec options
                foreach (var optionId in model.VariantSpecificationOptionIds)
                {
                    var vso = new VariantSpecificationOption
                    {
                        VariantId = model.VariantId,
                        OptionId = optionId,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.VariantSpecificationOptions.Add(vso);
                }

                await _context.SaveChangesAsync();

                var newItem = await GetCartItemDTO(cartItem.CartItemId);
                return Ok(new
                {
                    message = $"Item added to cart successfully. Quantity capped at stock ({variant.Stock ?? 0}).",
                    data = newItem
                });
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
                    .Include(ci => ci.Cart)
                    .Include(ci => ci.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                    .Include(ci => ci.Variant.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                    .ToListAsync();

                var itemsDto = items.Select(ci => MapCartItemDTO(ci)).ToList();
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
                if (item == null) return NotFound(new { message = "Cart item not found." });

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
                    .Include(ci => ci.Cart)
                    .Include(ci => ci.Variant)
                        .ThenInclude(v => v.Product)
                            .ThenInclude(p => p.ProductImages)
                    .Include(ci => ci.Variant.VariantSpecificationOptions)
                        .ThenInclude(vso => vso.Option)
                    .Where(ci => ci.CartId == cartId)
                    .ToListAsync();

                var itemsDto = items.Select(ci => MapCartItemDTO(ci)).ToList();
                var subtotal = itemsDto.Sum(i => i.TotalPrice);
                var totalItems = itemsDto.Sum(i => i.Quantity ?? 0);

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

                var item = await _context.CartItems
                    .Include(ci => ci.Variant)
                    .FirstOrDefaultAsync(ci => ci.CartItemId == id);

                if (item == null) return NotFound(new { message = "Cart item not found." });

                var variant = item.Variant;

                int finalQuantity = model.Quantity;
                if (variant.Stock.HasValue && finalQuantity > variant.Stock.Value)
                    finalQuantity = variant.Stock.Value;

                item.CartId = model.CartId;
                item.Quantity = finalQuantity;

                // Update specs
                if (model.VariantSpecificationOptionIds != null && model.VariantSpecificationOptionIds.Any())
                {
                    // Remove old spec links for this variant that are not in the new list
                    var oldSpecs = await _context.VariantSpecificationOptions
                        .Where(vso => vso.VariantId == item.VariantId &&
                                      !model.VariantSpecificationOptionIds.Contains(vso.OptionId))
                        .ToListAsync();

                    _context.VariantSpecificationOptions.RemoveRange(oldSpecs);

                    // Add new spec options if not already exists
                    foreach (var optionId in model.VariantSpecificationOptionIds)
                    {
                        bool exists = await _context.VariantSpecificationOptions
                            .AnyAsync(vso => vso.VariantId == item.VariantId && vso.OptionId == optionId);

                        if (!exists)
                        {
                            _context.VariantSpecificationOptions.Add(new VariantSpecificationOption
                            {
                                VariantId = item.VariantId,
                                OptionId = optionId,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }

                await _context.SaveChangesAsync();

                var updatedItem = await GetCartItemDTO(item.CartItemId);
                return Ok(new { message = $"Cart item updated successfully. Quantity capped at stock ({variant.Stock ?? 0}).", data = updatedItem });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error updating cart item.", error = ex.Message });
            }
        }

        // ================= DELETE CART ITEM =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCartItem(int id)
        {
            try
            {
                var item = await _context.CartItems.FindAsync(id);
                if (item == null) return NotFound(new { message = "Cart item not found." });

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
                var items = await _context.CartItems.Where(ci => ci.CartId == cartId).ToListAsync();
                if (!items.Any()) return NotFound(new { message = "No items found in this cart." });

                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart cleared successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error clearing cart.", error = ex.Message });
            }
        }

        // ================= PRIVATE HELPERS =================
        private async Task<CartItemDTO?> GetCartItemDTO(int cartItemId)
        {
            var item = await _context.CartItems
                .Include(ci => ci.Cart)
                .Include(ci => ci.Variant)
                    .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.ProductImages)
                .Include(ci => ci.Variant.VariantSpecificationOptions)
                    .ThenInclude(vso => vso.Option)
                .FirstOrDefaultAsync(ci => ci.CartItemId == cartItemId);

            if (item == null) return null;
            return MapCartItemDTO(item);
        }

        private CartItemDTO MapCartItemDTO(CartItem ci)
        {
            var specs = new Dictionary<string, string?>();

            foreach (var vso in ci.Variant.VariantSpecificationOptions)
            {
                if (vso.Option != null && !string.IsNullOrEmpty(vso.Option.OptionValue))
                {
                    var specName = vso.Option.Specification?.SpecificationName ?? "Option";
                    specs[specName] = vso.Option.OptionValue;
                }
            }

            return new CartItemDTO
            {
                CartItemId = ci.CartItemId,
                CartId = ci.CartId,
                VariantId = ci.VariantId,
                Quantity = ci.Quantity,
                UserId = ci.Cart.UserId,
                ProductName = ci.Variant.Product.ProductName,
                Image = ci.Variant.Product.ProductImages
                            .OrderByDescending(pi => pi.IsCover ?? false)
                            .Select(pi => pi.ImageUrl)
                            .FirstOrDefault(),
                Price = ci.Variant.Price ?? 0,
                TotalPrice = GetFinalPrice(ci.Variant, ci.Quantity ?? 0),
                CreatedAt = ci.CreatedAt,
                Specs = specs
            };
        }

        private static decimal GetFinalPrice(ProductVariant v, int quantity)
        {
            decimal price = v.Price ?? 0;
            var now = DateTime.UtcNow;

            if (v.DiscountStart.HasValue && now < v.DiscountStart) { }
            else if (v.DiscountEnd.HasValue && now > v.DiscountEnd) { }
            else
            {
                if (v.DiscountPercentage.HasValue && v.DiscountPercentage > 0)
                    price -= price * (v.DiscountPercentage.Value / 100);

                if (v.DiscountAmount.HasValue && v.DiscountAmount > 0)
                    price -= v.DiscountAmount.Value;
            }

            return (price < 0 ? 0 : price) * quantity;
        }
    }
}
