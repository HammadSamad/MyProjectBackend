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
        public async Task<ActionResult<IEnumerable<NotificationDTO>>> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool? isRead = null,
            [FromQuery] string? sortBy = "CreatedAt", // CreatedAt or Title
            [FromQuery] string? sortOrder = "desc") // asc or desc
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized(new { error = "User ID not found in token." });

                int userId = int.Parse(userIdClaim);

                // Base query: notifications for user or global
                var query = _context.Notifications
                    .Where(n => n.UserId == userId || n.TargetAudience == "All");

                // Optional filter by read/unread
                if (isRead.HasValue)
                    query = query.Where(n => n.IsRead == isRead.Value);

                // Sorting
                query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
                {
                    ("title", "asc") => query.OrderBy(n => n.Title),
                    ("title", "desc") => query.OrderByDescending(n => n.Title),
                    ("createdat", "asc") => query.OrderBy(n => n.CreatedAt),
                    _ => query.OrderByDescending(n => n.CreatedAt) // default descending CreatedAt
                };

                // Total count for pagination
                var totalCount = await query.CountAsync();

                // Paginate
                var notifications = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(n => new NotificationDTO
                    {
                        NotificationId = n.NotificationId,
                        UserId = n.UserId,
                        Title = n.Title,
                        Message = n.Message,
                        IsRead = n.IsRead
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
                    IsRead = notification.IsRead
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

        // ================= MARK ALL AS READ (Optimized) =================
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (userIdClaim == null)
                    return Unauthorized(new { error = "User ID not found in token." });

                int userId = int.Parse(userIdClaim);

                // Optimized: update directly in DB
                var updatedCount = await _context.Notifications
                    .Where(n => n.UserId == userId && n.IsRead == false)
                    .ExecuteUpdateAsync(n => n.SetProperty(p => p.IsRead, true));

                if (updatedCount == 0)
                    return Ok(new { message = "No unread notifications found." });

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
