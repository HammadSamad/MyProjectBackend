namespace Backend_Api.Models.Model_DTO
{
    public class ProductViewDTO
    {
        public long ViewId { get; set; }
        public int? UserId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string Username { get; set; } = "Guest";
        public string? ProductImage { get; set; }
        public DateTime? ViewedAt { get; set; }
    }
}
