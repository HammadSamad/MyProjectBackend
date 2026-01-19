namespace Backend_Api.Models.Model_Create
{
    public class CreateComplaint
    {
        public int UserId { get; set; }
        public long? OrderId { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
        public IFormFile? Image { get; set; }  
    }
}
