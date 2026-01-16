namespace Backend_Api.Models.Model_Create
{
    public class CreatePasswordResetToken
    {
        public int UserId { get; set; }
        public string ResetToken { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
