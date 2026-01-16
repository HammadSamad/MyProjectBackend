namespace Backend_Api.Models.Model_DTO
{
    public class PasswordResetDTO
    {
        public long ResetId { get; set; }
        public bool? IsUsed { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
