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
            if (model.Quantity <= 0)
                return BadRequest("Quantity must be greater than zero.");

            var cart = await _context.Carts.FindAsync(model.CartId);
            if (cart == null)
                return NotFound("Cart not found");

            var existingItem = await _context.CartItems
                .FirstOrDefaultAsync(ci => ci.CartId == model.CartId && ci.VariantId == model.VariantId);

            if (existingItem != null)
            {
                existingItem.Quantity += model.Quantity;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart item quantity increased" });
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

            return Ok(new { message = "Item added to cart", cartItemId = cartItem.CartItemId });
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CartItemDTO>>> GetAllCartItems()
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

            return Ok(items);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<CartItemDTO>> GetCartItemById(int id)
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
                return NotFound("Cart item not found");

            return Ok(item);
        }

        // ================= GET BY CART =================
        [HttpGet("cart/{cartId}")]
        public async Task<ActionResult<IEnumerable<CartItemDTO>>> GetItemsByCartId(int cartId)
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

            return Ok(items);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCartItem(int id, [FromBody] AddCartItemDTO model)
        {
            if (model.Quantity <= 0)
                return BadRequest("Quantity must be greater than zero.");

            var item = await _context.CartItems.FindAsync(id);
            if (item == null)
                return NotFound("Cart item not found");

            item.Quantity = model.Quantity;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart item updated successfully" });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCartItem(int id)
        {
            var item = await _context.CartItems.FindAsync(id);
            if (item == null)
                return NotFound("Cart item not found");

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart item deleted successfully" });
        }

        // ================= CLEAR CART =================
        [HttpDelete("clear/{cartId}")]
        public async Task<IActionResult> ClearCart(int cartId)
        {
            var items = await _context.CartItems
                .Where(ci => ci.CartId == cartId)
                .ToListAsync();

            if (!items.Any())
                return NotFound("No items found in this cart");

            _context.CartItems.RemoveRange(items);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart cleared successfully" });
        }
    }
}
