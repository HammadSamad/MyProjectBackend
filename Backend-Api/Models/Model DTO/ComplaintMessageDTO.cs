namespace Backend_Api.Models.Model_DTO
{
    public class ComplaintMessageDTO
    {
        public long MessageId { get; set; }
        public long ComplaintId { get; set; }
        public string? Message { get; set; }
    }
}
