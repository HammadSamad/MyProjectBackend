namespace Backend_Api.Models.Model_DTO
{
    public class RefundDTO
    {
        public long RefundId { get; set; }
        public long PaymentId { get; set; }
        public long? ReturnId { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
