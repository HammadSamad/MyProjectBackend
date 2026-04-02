namespace Backend_Api.Models
{
    public class CreateProductSpecificationValue
    {
        // Product to which this specification belongs
        public int ProductId { get; set; }

        // Specification definition
        public int SpecificationId { get; set; }

        // Option (if the specification uses predefined options)
        public int? OptionId { get; set; }

        // Free text value (if applicable)
        public string ValueText { get; set; } = string.Empty;

        // Numeric value (if applicable)
        public decimal? ValueNumber { get; set; }

        // Boolean value (if applicable)
        public bool? ValueBool { get; set; }
    }
}
