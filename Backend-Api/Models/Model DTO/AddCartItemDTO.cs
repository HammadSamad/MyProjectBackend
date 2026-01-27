namespace Backend_Api.Models.Model_DTO
{
    public class AddCartItemDTO
    {
        public int UserId { get; set; }
        public int CartId { get; set; }
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
