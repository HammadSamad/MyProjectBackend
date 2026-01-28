namespace Backend_Api.Models.Model_DTO
{
    public class ProductDisplayDTO
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImage { get; set; }
        public decimal OriginalPrice { get; set; }   // Actual price
        public decimal DiscountPrice { get; set; }   // Final price after discount
        public bool IsDiscounted { get; set; }       // For badge like "Sale"
        public double AverageRating { get; set; }
    }
}
