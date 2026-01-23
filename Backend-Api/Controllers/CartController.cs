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
            try
            {
                if (model == null || model.UserId <= 0)
                    return BadRequest(new { message = "Invalid cart data. UserId is required." });

                var existingCart = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserId == model.UserId);

                if (existingCart != null)
                    return BadRequest(new { message = "Cart already exists for this user." });

                var cart = new Cart
                {
                    UserId = model.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Cart created successfully.",
                    cartId = cart.CartId
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while creating the cart.",
                    error = ex.Message
                });
            }
        }

        // ================= GET ALL =================
        [HttpGet]
        public async Task<IActionResult> GetAllCarts()
        {
            try
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

                if (carts.Count == 0)
                    return Ok(new { message = "No carts found.", data = new List<CartDTO>() });

                return Ok(carts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching carts.",
                    error = ex.Message
                });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCartById(int id)
        {
            try
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
                    return NotFound(new { message = "Cart not found." });

                return Ok(cart);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching the cart.",
                    error = ex.Message
                });
            }
        }

        // ================= GET BY USER =================
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetCartByUserId(int userId)
        {
            try
            {
                if (userId <= 0)
                    return BadRequest(new { message = "Invalid UserId." });

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
                    return NotFound(new { message = "Cart not found for this user." });

                return Ok(cart);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching the user's cart.",
                    error = ex.Message
                });
            }
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCart(int id)
        {
            try
            {
                var cart = await _context.Carts.FindAsync(id);
                if (cart == null)
                    return NotFound(new { message = "Cart not found." });

                cart.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while updating the cart.",
                    error = ex.Message
                });
            }
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCart(int id)
        {
            try
            {
                var cart = await _context.Carts.FindAsync(id);
                if (cart == null)
                    return NotFound(new { message = "Cart not found." });

                _context.Carts.Remove(cart);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while deleting the cart.",
                    error = ex.Message
                });
            }
        }
    }
}
