using Backend_Api.Models.Model_DTO;

namespace Backend_Api.Services
{
    public interface INotificationService
    {
        Task<NotificationDTO> CreateNotificationAsync(
            int? userId,
            string title,
            string message,
            string? type = null,
            string? targetAudience = "User"
        );

        Task MarkAsReadAsync(int notificationId, int userId);
        Task MarkAllAsReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);

        Task<IEnumerable<NotificationDTO>> GetNotificationsAsync(
            int userId,
            bool? isRead = null,
            string sortBy = "CreatedAt",
            string sortOrder = "desc",
            int page = 1,
            int pageSize = 20
        );
    }
}
