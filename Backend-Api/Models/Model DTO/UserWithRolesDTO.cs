namespace Backend_Api.Models.Model_DTO
{
    public class UserWithRolesDTO
    {
        public int UserId { get; set; }

        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;

        public List<string> Roles { get; set; } = new();
    }
}
