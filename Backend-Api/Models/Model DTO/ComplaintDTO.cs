namespace Backend_Api.Models.Model_DTO
{
    public class ComplaintDTO
    {
        public long ComplaintId { get; set; }
        public long? OrderId { get; set; }
        public string Status { get; set; } = null!;
    }
}
