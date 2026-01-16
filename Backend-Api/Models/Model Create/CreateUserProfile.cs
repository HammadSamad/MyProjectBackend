namespace Backend_Api.Models.Model_Create
{
    public class CreateUserProfile
    {
        public int UserId { get; set; }
        public string? ProfileImage { get; set; }
        public string? Bio { get; set; }
    }
}
