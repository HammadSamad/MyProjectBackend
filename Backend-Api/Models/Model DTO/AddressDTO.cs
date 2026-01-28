namespace Backend_Api.Models.Model_DTO
{
    public class AddressDTO
    {
        public int AddressId { get; set; }
        public int UserId { get; set; }
        public int CityId { get; set; }
        public string? CityName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? PostalCode { get; set; }
        public bool? IsDefault { get; set; }
    }
}
