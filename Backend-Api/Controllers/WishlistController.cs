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
    public class WishlistController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public WishlistController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ============================
        // GET: api/Wishlist/user/{userId}
        // Get user's wishlist (auto create if not exists)
        // ============================
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetUserWishlist(int userId)
        {
            try
            {
                var wishlist = await _context.Wishlists
                    .Include(w => w.WishlistItems)
                    .ThenInclude(wi => wi.Variant)
                    .FirstOrDefaultAsync(w => w.UserId == userId);

                if (wishlist == null)
                {
                    wishlist = new Wishlist
                    {
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.Wishlists.Add(wishlist);
                    await _context.SaveChangesAsync();
                }

                var result = new WishlistDTO
                {
                    WishlistId = wishlist.WishlistId,
                    UserId = wishlist.UserId ?? 0
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve user's wishlist.", details = ex.Message });
            }
        }

        // ============================
        // POST: api/Wishlist
        // Create wishlist manually (Admin use only)
        // ============================
        [HttpPost]
        public async Task<IActionResult> CreateWishlist(CreateWishlist model)
        {
            if (model == null)
                return BadRequest(new { error = "Request body cannot be empty." });

            try
            {
                var exists = await _context.Wishlists.AnyAsync(w => w.UserId == model.UserId);
                if (exists)
                    return BadRequest(new { error = "Wishlist already exists for this user." });

                var wishlist = new Wishlist
                {
                    UserId = model.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Wishlists.Add(wishlist);
                await _context.SaveChangesAsync();

                return Ok(new WishlistDTO
                {
                    WishlistId = wishlist.WishlistId,
                    UserId = wishlist.UserId ?? 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create wishlist.", details = ex.Message });
            }
        }

        // ============================
        // GET: api/Wishlist/{id}
        // ============================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetWishlistById(int id)
        {
            try
            {
                var wishlist = await _context.Wishlists.FindAsync(id);

                if (wishlist == null)
                    return NotFound(new { error = "Wishlist not found." });

                return Ok(new WishlistDTO
                {
                    WishlistId = wishlist.WishlistId,
                    UserId = wishlist.UserId ?? 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve wishlist.", details = ex.Message });
            }
        }

        // ============================
        // DELETE: api/Wishlist/{id}
        // Admin use (normally we do not delete wishlist)
        // ============================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWishlist(int id)
        {
            try
            {
                var wishlist = await _context.Wishlists
                    .Include(w => w.WishlistItems)
                    .FirstOrDefaultAsync(w => w.WishlistId == id);

                if (wishlist == null)
                    return NotFound(new { error = "Wishlist not found." });

                _context.WishlistItems.RemoveRange(wishlist.WishlistItems);
                _context.Wishlists.Remove(wishlist);

                await _context.SaveChangesAsync();
                return Ok(new { message = "Wishlist deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete wishlist.", details = ex.Message });
            }
        }
    }
}
