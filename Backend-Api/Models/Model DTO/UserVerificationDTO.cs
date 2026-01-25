namespace Backend_Api.Models.Model_DTO
{
    public class UserVerificationDTO
    {
        public long VerificationId { get; set; }
        public int UserId { get; set; }
        public string Channel { get; set; } = null!;
        public bool IsUsed { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
