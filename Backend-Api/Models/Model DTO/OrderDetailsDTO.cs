namespace Backend_Api.Models.Model_DTO
{
    public class OrderDetailsDTO
    {
        public long OrderId { get; set; }
        public int UserId { get; set; }
        public int ItemsCount { get; set; }
        public string? DeliveryStatus { get; set; }
        public decimal? TotalAmount { get; set; }
        public List<OrderDetailsItemDTO> Items { get; set; } = new();
    }
}
