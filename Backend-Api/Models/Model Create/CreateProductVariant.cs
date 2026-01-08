namespace Backend_Api.Models
{
    public class CreateProductVariant
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = null!;
        public decimal Price { get; set; }
        public int Stock { get; set; }

        public int[]? SpecificationOptionIds { get; set; }
    }
}
