namespace Backend_Api.Models.Model_DTO
{
    public class ProductReviewDTO
    {
        public int ReviewId { get; set; }
        public int Rating { get; set; }
        public string? ReviewText { get; set; }
        public string? UserName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
