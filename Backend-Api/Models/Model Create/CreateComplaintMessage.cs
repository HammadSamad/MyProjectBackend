namespace Backend_Api.Models.Model_Create
{
    public class CreateComplaintMessage
    {
        public long ComplaintId { get; set; }
        public string? SenderType { get; set; }  // "User" or "Admin"
        public IFormFile? Image { get; set; }       
        public string? Message { get; set; }
    }
}
