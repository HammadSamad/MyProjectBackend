namespace Backend_Api.Models.Model_DTO
{
    public class PasswordResetDTO
    {
        public string Token { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
