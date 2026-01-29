namespace Backend_Api.Models.Model_Create
{
    public class CreateCartItem
    {
        public int CartId { get; set; }
        public int VariantId { get; set; }
        public int VariantSpecificationOptionsId { get; set; }
        public int? Quantity { get; set; }
    }
}
