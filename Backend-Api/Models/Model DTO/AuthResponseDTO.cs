namespace Backend_Api.Models.Model_DTO
{
    public class AuthResponseDTO
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
    }
}
