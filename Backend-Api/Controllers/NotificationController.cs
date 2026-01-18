using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_Create;
using Backend_Api.Models.Model_DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Backend_Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Require JWT
    public class NotificationController : ControllerBase
    {
        private readonly LaptopHarbourDbContext _context;

        public NotificationController(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // GET: api/Notification
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NotificationDTO>>> GetAll()
        {
            var notifications = await _context.Notifications
                .Select(n => new NotificationDTO
                {
                    NotificationId = n.NotificationId,
                    UserId = n.UserId,
                    Title = n.Title,
                    Message = n.Message,
                    IsRead = n.IsRead
                }).ToListAsync();

            return Ok(notifications);
        }

        // GET: api/Notification/5
        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDTO>> GetById(long id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            return new NotificationDTO
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead
            };
        }

        // POST: api/Notification
        [HttpPost]
        public async Task<ActionResult<NotificationDTO>> Create([FromBody] CreateNotification model)
        {
            // Extract UserId from JWT (optional if user-specific)
            int? userId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null) userId = int.Parse(userIdClaim);

            var notification = new Notification
            {
                UserId = model.UserId ?? userId,
                Title = model.Title,
                Message = model.Message,
                TargetAudience = model.TargetAudience,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            var dto = new NotificationDTO
            {
                NotificationId = notification.NotificationId,
                UserId = notification.UserId,
                Title = notification.Title,
                Message = notification.Message,
                IsRead = notification.IsRead
            };

            return CreatedAtAction(nameof(GetById), new { id = notification.NotificationId }, dto);
        }

        // PUT: api/Notification/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateNotification model)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            // Optional: Only owner can update
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && notification.UserId.HasValue && notification.UserId != int.Parse(userIdClaim))
                return Forbid("You can only update your own notifications.");

            notification.Title = model.Title;
            notification.Message = model.Message;
            notification.TargetAudience = model.TargetAudience;
            notification.IsRead = model.IsRead ?? notification.IsRead;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/Notification/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            // Optional: Only owner can delete
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && notification.UserId.HasValue && notification.UserId != int.Parse(userIdClaim))
                return Forbid("You can only delete your own notifications.");

            _context.Notifications.Remove(notification);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        // PUT: api/Notification/mark-read/5
        [HttpPut("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification == null) return NotFound();

            notification.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
