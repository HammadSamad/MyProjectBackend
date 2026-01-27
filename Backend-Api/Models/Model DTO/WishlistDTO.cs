namespace Backend_Api.Models.Model_DTO
{
    public class WishlistDTO
    {
        public int WishlistId { get; set; }
        public int UserId { get; set; }
        public List<WishlistItemDTO> Items { get; set; } = new List<WishlistItemDTO>();
    }
}
