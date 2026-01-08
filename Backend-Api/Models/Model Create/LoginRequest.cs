namespace Backend_Api.Models.Model_Create
{
    public class LoginRequest
    {
        public string UsernameOrEmail { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
