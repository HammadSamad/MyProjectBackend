namespace Backend_Api.Models.Model_DTO
{
    public class OrderDTO
    {
        public long OrderId { get; set; }
        public int UserId { get; set; }
        public decimal? TotalAmount { get; set; }
        public int? PaymentMethodId { get; set; }
        public string OrderStatus { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
