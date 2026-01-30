using Backend_Api.Data;
using Backend_Api.Models;
using Backend_Api.Models.Model_DTO;
using Microsoft.EntityFrameworkCore;

namespace Backend_Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly LaptopHarbourDbContext _context;

        public NotificationService(LaptopHarbourDbContext context)
        {
            _context = context;
        }

        // ================= CREATE NOTIFICATION =================
        public async Task<NotificationDTO> CreateNotificationAsync(int? userId, string title, string message, string? type = null, string? targetAudience = "User")
        {
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Title cannot be empty.");
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Message cannot be empty.");

            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type ?? "General",
                TargetAudience = targetAudience ?? "User",
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

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

        // ================= MARK SINGLE AS READ =================
        public async Task MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);

            if (notification == null)
                throw new KeyNotFoundException("Notification not found or not authorized.");

            notification.IsRead = true;
            await _context.SaveChangesAsync();
        }

        // ================= MARK ALL AS READ =================
        public async Task MarkAllAsReadAsync(int userId)
        {
            await _context.Notifications
                .Where(n => n.UserId == userId && n.IsRead.GetValueOrDefault() == false)
                .ExecuteUpdateAsync(n => n.SetProperty(p => p.IsRead, true));
        }

        // ================= GET UNREAD COUNT =================
        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && n.IsRead.GetValueOrDefault() == false);
        }

        // ================= GET NOTIFICATIONS WITH PAGINATION =================
        public async Task<IEnumerable<NotificationDTO>> GetNotificationsAsync(int userId, bool? isRead = null, string sortBy = "CreatedAt", string sortOrder = "desc", int page = 1, int pageSize = 20)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 20;

            var query = _context.Notifications
                .Where(n => n.UserId == userId || n.TargetAudience == "All");

            if (isRead.HasValue)
                query = query.Where(n => n.IsRead.GetValueOrDefault() == isRead.Value);

            query = (sortBy?.ToLower(), sortOrder?.ToLower()) switch
            {
                ("title", "asc") => query.OrderBy(n => n.Title),
                ("title", "desc") => query.OrderByDescending(n => n.Title),
                ("createdat", "asc") => query.OrderBy(n => n.CreatedAt),
                _ => query.OrderByDescending(n => n.CreatedAt)
            };

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

            return notifications;
        }
    }
}
