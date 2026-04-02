namespace Backend_Api.Models.Model_DTO
{
    public class ComplaintMessageDTO
    {
        public long MessageId { get; set; }
        public long ComplaintId { get; set; }
        public string? SenderType { get; set; }
        public string? Image { get; set; }
        public string? Message { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
