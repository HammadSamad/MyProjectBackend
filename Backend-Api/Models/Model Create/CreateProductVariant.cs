namespace Backend_Api.Models
{
    public class CreateProductVariant
    {
        public int ProductId { get; set; }
        public string? Sku { get; set; }
        public decimal? Price { get; set; }
        public int? Stock { get; set; }
    }
}
