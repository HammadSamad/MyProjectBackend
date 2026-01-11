namespace Backend_Api.Models.Model_Create
{
    public class CreateVarientPriceHistory
    {
        public int VariantId { get; set; }
        public decimal? OldPrice { get; set; }
        public decimal? NewPrice { get; set; }
    }
}
