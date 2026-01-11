namespace Backend_Api.Models
{
    public class CreateProductSpecificationValue
    {
        public int ProductId { get; set; }
        public int SpecificationId { get; set; }

        public string? ValueText { get; set; }
        public decimal? ValueNumber { get; set; }
        public bool? ValueBool { get; set; }
        public int? OptionId { get; set; }
    }
}
