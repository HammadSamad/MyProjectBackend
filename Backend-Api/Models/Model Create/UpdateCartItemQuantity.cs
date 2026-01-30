namespace Backend_Api.Models.Model_Create
{
    public class UpdateCartItemQuantity
    {
        public int CartId { get; set; }
        public int Quantity { get; set; }
        public List<int> VariantSpecificationOptionIds { get; set; } = new List<int>();
    }
}
