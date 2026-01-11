namespace Backend_Api.Models.Model_DTO
{
    public class VariantPriceHistoryDTO
    {
        public decimal? OldPrice { get; set; }
        public decimal? NewPrice { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
