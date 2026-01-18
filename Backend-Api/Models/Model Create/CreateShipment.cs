namespace Backend_Api.Models.Model_Create
{
    public class CreateShipment
    {
        public long OrderId { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CourierName { get; set; }
        public decimal ShippingCost { get; set; }
        public string? Status { get; set; } = "Pending";
        public DateTime? ExpectedDeliveryDate { get; set; }
    }
}
