namespace Backend_Api.Models.Model_DTO
{
    public class CartItemDTO
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public int VariantId { get; set; }
        public int? Quantity { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
