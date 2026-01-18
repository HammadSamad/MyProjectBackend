namespace Backend_Api.Models.Model_Create
{
    public class CreateOrderAddress
    {
        public long OrderId { get; set; }
        public string? RecipientName { get; set; }
        public string? FullAddress { get; set; }
        public string? Phone { get; set; }
    }
}
