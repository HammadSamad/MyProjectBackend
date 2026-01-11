namespace Backend_Api.Models.Model_DTO
{
    public class ProductSpecificationDTO
    {
        public string? SpecificationName { get; set; }
        public string? DataType { get; set; }

        public string? ValueText { get; set; }
        public decimal? ValueNumber { get; set; }
        public bool? ValueBool { get; set; }
        public string? OptionValue { get; set; }
    }
}
