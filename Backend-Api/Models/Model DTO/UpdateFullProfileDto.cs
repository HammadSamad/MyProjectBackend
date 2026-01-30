namespace Backend_Api.Models.Model_DTO
{
    public class UpdateFullProfileDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }

        // Profile fields
        public string? Bio { get; set; }
        public IFormFile? ProfileImage { get; set; }
    }
}
