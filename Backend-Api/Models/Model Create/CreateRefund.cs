namespace Backend_Api.Models.Model_Create
{
    public class CreateRefund
    {
        public long PaymentId { get; set; }
        public long? ReturnId { get; set; }
        public decimal Amount { get; set; }
        public string? Reason { get; set; }
    }
}
