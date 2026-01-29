namespace Backend_Api.Models.Model_DTO
{
    public class ProductViewDTO
    {
        public long ViewId { get; set; }
        public int? UserId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public string? ProductImage { get; set; }
        public decimal OriginalPrice { get; set; }
        public decimal DiscountPrice { get; set; }
        public decimal DiscountPercentage { get; set; }
        public bool IsDiscounted { get; set; }
        public string Username { get; set; } = "Guest";
        public DateTime? ViewedAt { get; set; }
    }
}
