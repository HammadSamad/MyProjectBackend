using Backend_Api.Models.Model_DTO;

namespace Backend_Api.Models
{
    public class ProductVariantDTO
    {
        public int VariantId { get; set; }
        public string? Sku { get; set; }
        public decimal? Price { get; set; }
        public decimal FinalPrice { get; set; }
        public int? Stock { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public DateTime? DiscountStart { get; set; }
        public DateTime? DiscountEnd { get; set; }
        public List<VariantSpecificationOptionDTO> Specifications { get; set; } = new();
    }
}
