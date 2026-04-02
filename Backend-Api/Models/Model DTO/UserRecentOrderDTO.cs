namespace Backend_Api.Models.Model_DTO
{
    public class UserRecentOrderDTO
    {
        public long Id { get; set; }
        public int UserId { get; set; }
        public long OrderId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
