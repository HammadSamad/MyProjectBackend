namespace Backend_Api.Models.Model_DTO
{
    public class WishlistItemDTO
    {
        public int WishlistItemId { get; set; }
        public int WishlistId { get; set; }
        public int VariantId { get; set; }
        public string ProductName { get; set; } = "";
        public string Image { get; set; } = "";
        public decimal Price { get; set; }

        // Add variant specifications
        public List<VariantSpecificationOptionDTO> VariantSpecifications { get; set; } = null!;
    }
}
