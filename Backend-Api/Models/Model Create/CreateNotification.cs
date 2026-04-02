namespace Backend_Api.Models.Model_Create
{
    public class CreateNotification
    {
        public int? UserId { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? Type { get; set; }
        public string TargetAudience { get; set; } = null!;
        public bool? IsRead { get; set; }
    }
}
