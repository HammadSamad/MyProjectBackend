namespace Backend_Api.Models.Model_Create
{
    public class CreateMessageLog
    {
        public int? UserId { get; set; }
        public string Channel { get; set; } = null!;
        public string Recipient { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? Status { get; set; }
    }
}
