namespace Backend_Api.Models.Model_DTO
{
    public class AddWishlistItemDTO
    {
        public int UserId { get; set; }
        public int WishlistId { get; set; }
        public int VariantId { get; set; }
    }
}
