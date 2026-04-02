namespace Backend_Api.Models.Model_Create
{
    public class CreatePayment
    {
        public long OrderId { get; set; }
        public int UserId { get; set; }
        public int PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Pending";
    }
}
