using Backend_Api.Data;
using Backend_Api.Models;
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

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> AddCartItem([FromBody] AddCartItemDTO model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { message = "Invalid cart item data." });

                if (model.Quantity <= 0)
                    return BadRequest(new { message = "Quantity must be greater than zero." });

                var cart = await _context.Carts.FindAsync(model.CartId);
                if (cart == null)
                    return NotFound(new { message = "Cart not found." });

                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => ci.CartId == model.CartId && ci.VariantId == model.VariantId);

                if (existingItem != null)
                {
                    existingItem.Quantity += model.Quantity;
                    await _context.SaveChangesAsync();

                    return Ok(new { message = "Cart item quantity increased successfully." });
                }

                var cartItem = new CartItem
                {
                    CartId = model.CartId,
                    VariantId = model.VariantId,
                    Quantity = model.Quantity,
                    CreatedAt = DateTime.UtcNow
                };

                _context.CartItems.Add(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Item added to cart successfully.",
                    cartItemId = cartItem.CartItemId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while adding item to the cart.",
                    error = ex.Message
                });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<IActionResult> GetAllCartItems()
        {
            try
            {
                var items = await _context.CartItems
                    .Select(ci => new CartItemDTO
                    {
                        CartItemId = ci.CartItemId,
                        CartId = ci.CartId,
                        VariantId = ci.VariantId,
                        Quantity = ci.Quantity,
                        CreatedAt = ci.CreatedAt
                    })
                    .ToListAsync();

                if (items.Count == 0)
                    return Ok(new { message = "No cart items found.", data = new List<CartItemDTO>() });

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching cart items.",
                    error = ex.Message
                });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCartItemById(int id)
        {
            try
            {
                var item = await _context.CartItems
                    .Where(ci => ci.CartItemId == id)
                    .Select(ci => new CartItemDTO
                    {
                        CartItemId = ci.CartItemId,
                        CartId = ci.CartId,
                        VariantId = ci.VariantId,
                        Quantity = ci.Quantity,
                        CreatedAt = ci.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (item == null)
                    return NotFound(new { message = "Cart item not found." });

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching the cart item.",
                    error = ex.Message
                });
            }
        }

        // ================= GET BY CART =================
        [HttpGet("cart/{cartId}")]
        public async Task<IActionResult> GetItemsByCartId(int cartId)
        {
            try
            {
                var items = await _context.CartItems
                    .Where(ci => ci.CartId == cartId)
                    .Select(ci => new CartItemDTO
                    {
                        CartItemId = ci.CartItemId,
                        CartId = ci.CartId,
                        VariantId = ci.VariantId,
                        Quantity = ci.Quantity,
                        CreatedAt = ci.CreatedAt
                    })
                    .ToListAsync();

                if (items.Count == 0)
                    return NotFound(new { message = "No items found for this cart." });

                return Ok(items);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching cart items.",
                    error = ex.Message
                });
            }
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] AddCartItemDTO model)
        {
            try
            {
                if (model == null)
                    return BadRequest(new { message = "Invalid cart item data." });

                if (model.Quantity <= 0)
                    return BadRequest(new { message = "Quantity must be greater than zero." });

                var item = await _context.CartItems.FindAsync(id);
                if (item == null)
                    return NotFound(new { message = "Cart item not found." });

                item.Quantity = model.Quantity;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart item updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the cart item.",
                    error = ex.Message
                });
            }
        }

        // ================= DELETE =================
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
                return StatusCode(500, new
                {
                    message = "An error occurred while deleting the cart item.",
                    error = ex.Message
                });
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

                if (items.Count == 0)
                    return NotFound(new { message = "No items found in this cart." });

                _context.CartItems.RemoveRange(items);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart cleared successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while clearing the cart.",
                    error = ex.Message
                });
            }
        }
    }
}
