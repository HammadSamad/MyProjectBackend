namespace Backend_Api.Models.Model_Create
{
    public class CreateAddress
    {
        public int CityId { get; set; }

        public string? AddressLine1 { get; set; }

        public string? AddressLine2 { get; set; }

        public string? PostalCode { get; set; }
    }
}
