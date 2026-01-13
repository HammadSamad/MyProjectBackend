namespace Backend_Api.Models.Model_DTO
{
    public class NotificationDTO
    {
        public long NotificationId { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public string? Type { get; set; }
        public bool? IsRead { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
