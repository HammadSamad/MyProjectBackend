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
    public class CartController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public CartController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> CreateCart([FromBody] CreateCart model)
        {
            var existingCart = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserId == model.UserId);

            if (existingCart != null)
                return BadRequest("Cart already exists for this user.");

            var cart = new Cart
            {
                UserId = model.UserId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart created successfully", cartId = cart.CartId });
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CartDTO>>> GetAllCarts()
        {
            var carts = await _context.Carts
                .Select(c => new CartDTO
                {
                    CartId = c.CartId,
                    UserId = c.UserId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();

            return Ok(carts);
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<CartDTO>> GetCartById(int id)
        {
            var cart = await _context.Carts
                .Where(c => c.CartId == id)
                .Select(c => new CartDTO
                {
                    CartId = c.CartId,
                    UserId = c.UserId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (cart == null)
                return NotFound("Cart not found");

            return Ok(cart);
        }

        // ================= GET BY USER =================
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<CartDTO>> GetCartByUserId(int userId)
        {
            var cart = await _context.Carts
                .Where(c => c.UserId == userId)
                .Select(c => new CartDTO
                {
                    CartId = c.CartId,
                    UserId = c.UserId,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .FirstOrDefaultAsync();

            if (cart == null)
                return NotFound("Cart not found for this user");

            return Ok(cart);
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCart(int id)
        {
            var cart = await _context.Carts.FindAsync(id);
            if (cart == null)
                return NotFound("Cart not found");

            cart.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart updated successfully" });
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCart(int id)
        {
            var cart = await _context.Carts.FindAsync(id);
            if (cart == null)
                return NotFound("Cart not found");

            _context.Carts.Remove(cart);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Cart deleted successfully" });
        }
    }
}
