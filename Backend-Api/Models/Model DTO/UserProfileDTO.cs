namespace Backend_Api.Models.Model_DTO
{
    public class UserProfileDTO
    {
        public int ProfileId { get; set; }
        public string? ProfileImage { get; set; }
        public string? Bio { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
