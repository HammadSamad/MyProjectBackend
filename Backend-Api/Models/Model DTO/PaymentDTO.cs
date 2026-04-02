namespace Backend_Api.Models.Model_DTO
{
    public class PaymentDTO
    {
        public long PaymentId { get; set; }
        public long OrderId { get; set; }
        public int UserId { get; set; }
        public int PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = null!;
        public string? TransactionReference { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
