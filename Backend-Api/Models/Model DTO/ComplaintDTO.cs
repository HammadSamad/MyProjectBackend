namespace Backend_Api.Models.Model_DTO
{
    public class ComplaintDTO
    {
        public long ComplaintId { get; set; }
        public int UserId { get; set; }
        public long? OrderId { get; set; }
        public string? Image { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public string Status { get; set; } = null!;
        public string? Priority { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
