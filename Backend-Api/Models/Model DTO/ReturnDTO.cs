namespace Backend_Api.Models.Model_DTO
{
    public class ReturnDTO
    {
        public long ReturnId { get; set; }
        public long OrderId { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
    }
}
