namespace Backend_Api.Models
{
    public class CreateProductVariant
    {
        public int ProductId { get; set; }
        public string? Sku { get; set; }
        public decimal? Price { get; set; }
        public int? Stock { get; set; }
        // --- Discount fields ---
        public decimal? DiscountPercentage { get; set; }   // e.g., 10 = 10%
        public decimal? DiscountAmount { get; set; }       // fixed amount
        public DateTime? DiscountStart { get; set; }       // optional, for time-based
        public DateTime? DiscountEnd { get; set; }         // optional, for time-based
    }
}
