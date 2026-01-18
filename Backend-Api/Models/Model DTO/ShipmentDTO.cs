namespace Backend_Api.Models.Model_DTO
{
    public class ShipmentDTO
    {
        public long ShipmentId { get; set; }
        public long OrderId { get; set; }
        public string? TrackingNumber { get; set; }
        public string? CourierName { get; set; }
        public decimal ShippingCost { get; set; }
        public string? Status { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
