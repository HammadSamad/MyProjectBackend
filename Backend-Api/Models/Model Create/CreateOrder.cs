namespace Backend_Api.Models.Model_Create
{
    public class CreateOrder
    {
        public int UserId { get; set; }
        public decimal? TotalAmount { get; set; }
        public int? PaymentMethodId { get; set; }
    }
}
