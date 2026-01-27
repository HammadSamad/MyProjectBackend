namespace Backend_Api.Models.Model_DTO
{
    public class CartItemDTO
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public int VariantId { get; set; }
        public int? Quantity { get; set; }
        public int UserId { get; set; }

        // Variant details
        public string? VariantSku { get; set; }
        public decimal Price { get; set; }
        public string? ProductName { get; set; }
        public string? Image { get; set; }

        public decimal TotalPrice { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
