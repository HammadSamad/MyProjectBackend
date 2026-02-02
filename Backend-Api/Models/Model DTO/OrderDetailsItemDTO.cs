using Backend_Api.Models.Model_DTO;

public class OrderDetailsItemDTO
{
    public int ProductId { get; set; }
    public int VariantId { get; set; }
    public int VariantSpecificationOptionsId { get; set; }
    public int? Quantity { get; set; }
    public int UserId { get; set; }
    public decimal? Price { get; set; }
    public string? ProductName { get; set; }
    public string? Image { get; set; }
    public List<VariantSpecificationOptionDTO> VariantSpecifications { get; set; } = new();
}
