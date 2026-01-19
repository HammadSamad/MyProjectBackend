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
    public class UserRecentOrderController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public UserRecentOrderController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<IActionResult> AddRecentOrder([FromBody] CreateUserRecentOrder model)
        {
            // Optional: check if the record already exists
            var existing = await _context.UserRecentOrders
                .FirstOrDefaultAsync(u => u.UserId == model.UserId && u.OrderId == model.OrderId);

            if (existing != null)
            {
                // Update timestamp if already exists
                existing.CreatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Recent order timestamp updated", id = existing.Id });
            }

            var recentOrder = new UserRecentOrder
            {
                UserId = model.UserId,
                OrderId = model.OrderId,
                CreatedAt = DateTime.UtcNow
            };

            _context.UserRecentOrders.Add(recentOrder);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Recent order added successfully", id = recentOrder.Id });
        }

        // ================= GET BY USER =================
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<UserRecentOrderDTO>>> GetRecentOrdersByUser(int userId)
        {
            var orders = await _context.UserRecentOrders
                .Where(u => u.UserId == userId)
                .OrderByDescending(u => u.CreatedAt)
                .Select(u => new UserRecentOrderDTO
                {
                    Id = u.Id,
                    UserId = u.UserId,
                    OrderId = u.OrderId,
                    CreatedAt = u.CreatedAt
                })
                .ToListAsync();

            return Ok(orders);
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRecentOrder(long id)
        {
            var recentOrder = await _context.UserRecentOrders.FindAsync(id);
            if (recentOrder == null) return NotFound("Recent order not found");

            _context.UserRecentOrders.Remove(recentOrder);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Recent order deleted successfully" });
        }

        // ================= CLEAR ALL FOR USER =================
        [HttpDelete("user/{userId}")]
        public async Task<IActionResult> ClearRecentOrdersForUser(int userId)
        {
            var orders = await _context.UserRecentOrders
                .Where(u => u.UserId == userId)
                .ToListAsync();

            if (!orders.Any()) return NotFound("No recent orders found for this user");

            _context.UserRecentOrders.RemoveRange(orders);
            await _context.SaveChangesAsync();

            return Ok(new { message = "All recent orders cleared for this user" });
        }
    }
}
