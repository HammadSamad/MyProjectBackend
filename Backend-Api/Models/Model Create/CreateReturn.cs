namespace Backend_Api.Models.Model_Create
{
    public class CreateReturn
    {
        public long OrderId { get; set; }
        public int UserId { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
