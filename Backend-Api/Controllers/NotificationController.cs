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

        // ================= GET ALL WITH PAGINATION, FILTER & SORT =================
        [HttpGet]
        public async Task<ActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isRead = null,
            [FromQuery] string? sortBy = "CreatedAt",
            [FromQuery] string? sortOrder = "desc")
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized(new { error = "User ID not found in token." });

                int userId = int.Parse(userIdClaim);

                var query = _context.Notifications
                    .Where(n => n.UserId == userId || n.TargetAudience == "All");

                if (isRead.HasValue)
                    query = query.Where(n => n.IsRead == isRead.Value);

                query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
                {
                    ("title", "asc") => query.OrderBy(n => n.Title),
                    ("title", "desc") => query.OrderByDescending(n => n.Title),
                    ("createdat", "asc") => query.OrderBy(n => n.CreatedAt),
                    _ => query.OrderByDescending(n => n.CreatedAt)
                };

                var totalCount = await query.CountAsync();

                var notifications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationDTO
                    {
                        NotificationId = n.NotificationId,
                        UserId = n.UserId,
                        Title = n.Title,
                        Message = n.Message,
                        Type = n.Type,
                        IsRead = n.IsRead,
                        CreatedAt = n.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                    data = notifications
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch notifications.", details = ex.Message });
            }
        }

        // ================= GET BY ID =================
        [HttpGet("{id}")]
        public async Task<ActionResult<NotificationDTO>> GetById(long id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                    return NotFound(new { error = "Notification not found." });

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && notification.UserId.HasValue && notification.UserId != int.Parse(userIdClaim) && notification.TargetAudience != "All")
                    return StatusCode(403, new { error = "You do not have access to this notification." });

                return new NotificationDTO
                {
                    NotificationId = notification.NotificationId,
                    UserId = notification.UserId,
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                };
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch notification.", details = ex.Message });
            }
        }

        // ================= CREATE =================
        [HttpPost]
        public async Task<ActionResult<NotificationDTO>> Create([FromBody] CreateNotification model)
        {
            try
            {
                if (model == null || string.IsNullOrWhiteSpace(model.Title) || string.IsNullOrWhiteSpace(model.Message))
                    return BadRequest(new { error = "Title and Message are required." });

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                int? currentUserId = userIdClaim != null ? int.Parse(userIdClaim) : null;

                var notification = new Notification
                {
                    UserId = model.UserId ?? currentUserId,
                    Title = model.Title,
                    Message = model.Message,
                    Type = model.Type,
                    TargetAudience = model.TargetAudience,
                    IsRead = model.IsRead ?? false,
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
                    Type = notification.Type,
                    IsRead = notification.IsRead,
                    CreatedAt = notification.CreatedAt
                };

                return CreatedAtAction(nameof(GetById), new { id = notification.NotificationId }, dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to create notification.", details = ex.Message });
            }
        }

        // ================= UPDATE =================
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateNotification model)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                    return NotFound(new { error = "Notification not found." });

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && notification.UserId.HasValue && notification.UserId != int.Parse(userIdClaim))
                    return StatusCode(403, new { error = "You can only update your own notifications." });

                notification.Title = model.Title ?? notification.Title;
                notification.Message = model.Message ?? notification.Message;
                notification.Type = model.Type ?? notification.Type;
                notification.TargetAudience = model.TargetAudience ?? notification.TargetAudience;
                notification.IsRead = model.IsRead ?? notification.IsRead;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Notification updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to update notification.", details = ex.Message });
            }
        }

        // ================= DELETE =================
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                    return NotFound(new { error = "Notification not found." });

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && notification.UserId.HasValue && notification.UserId != int.Parse(userIdClaim))
                    return StatusCode(403, new { error = "You can only delete your own notifications." });

                _context.Notifications.Remove(notification);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification deleted successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to delete notification.", details = ex.Message });
            }
        }

        // ================= MARK AS READ =================
        [HttpPut("mark-read/{id}")]
        public async Task<IActionResult> MarkAsRead(long id)
        {
            try
            {
                var notification = await _context.Notifications.FindAsync(id);
                if (notification == null)
                    return NotFound(new { error = "Notification not found." });

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim != null && notification.UserId.HasValue && notification.UserId != int.Parse(userIdClaim))
                    return StatusCode(403, new { error = "You can only mark your own notifications as read." });

                notification.IsRead = true;
                await _context.SaveChangesAsync();

                return Ok(new { message = "Notification marked as read." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to mark notification as read.", details = ex.Message });
            }
        }

        // ================= MARK ALL AS READ =================
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized(new { error = "User ID not found in token." });

                int userId = int.Parse(userIdClaim);

                var updatedCount = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .ExecuteUpdateAsync(n => n.SetProperty(p => p.IsRead, true));

                return Ok(new { message = "All notifications marked as read.", count = updatedCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to mark all notifications as read.", details = ex.Message });
            }
        }

        // ================= GET UNREAD COUNT =================
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized(new { error = "User ID not found in token." });

                int userId = int.Parse(userIdClaim);

                var unreadCount = await _context.Notifications
                    .CountAsync(n => n.UserId == userId && n.IsRead == false);

                return Ok(new { unreadCount });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to fetch unread notifications count.", details = ex.Message });
            }
        }
    }
}
