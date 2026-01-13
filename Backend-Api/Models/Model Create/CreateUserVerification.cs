namespace Backend_Api.Models.Model_Create
{
    public class CreateUserVerification
    {
        public int UserId { get; set; }
        public string Channel { get; set; } = null!;
        public string Code { get; set; } = null!;
        public DateTime ExpiresAt { get; set; }
    }
}
