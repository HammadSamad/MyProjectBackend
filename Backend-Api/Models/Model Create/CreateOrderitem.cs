namespace Backend_Api.Models.Model_Create
{
    public class CreateOrderitem
    {
        public long OrderId { get; set; }
        public int VariantId { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
    }
}
