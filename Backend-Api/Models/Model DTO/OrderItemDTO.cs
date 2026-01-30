namespace Backend_Api.Models.Model_DTO
{
    public class OrderItemDTO
    {
        public long OrderItemId { get; set; }
        public long OrderId { get; set; }
        public int VariantId { get; set; }
        public string? ProductName { get; set; }
        public string? Image { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
        public int VariantSpecificationOptionsId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<VariantSpecificationOptionDTO> VariantSpecifications { get; set; } = new();
    }
}
