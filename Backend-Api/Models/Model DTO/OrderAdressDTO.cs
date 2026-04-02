namespace Backend_Api.Models.Model_DTO
{
    public class OrderAdressDTO
    {
        public long OrderAddressId { get; set; }
        public long OrderId { get; set; }
        public string? RecipientName { get; set; }
        public int AddressId { get; set; }
        public string? Label { get; set; }
        public string? AddressLine1 { get; set; }
        public string? CityName { get; set; }
        public string? Phone { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
