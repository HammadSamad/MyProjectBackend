namespace Backend_Api.Models.Model_Create
{
    public class CreateComplaintMessage
    {
        public long ComplaintId { get; set; }
        public string? SenderType { get; set; }  // "User" or "Admin"
        public List<IFormFile>? Images { get; set; }    // multiple       
        public string? Message { get; set; }
    }
}
