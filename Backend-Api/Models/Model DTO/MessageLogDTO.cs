namespace Backend_Api.Models.Model_DTO
{
    public class MessageLogDTO
    {
        public long MessageId { get; set; }
        public string? Channel { get; set; }
        public string? Recipient { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
