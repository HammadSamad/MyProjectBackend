namespace Backend_Api.Models.Model_DTO
{
    public class VerifyEmailDTO
    {
        public int UserId { get; set; }
        public string Code { get; set; } = null!;
    }
}
