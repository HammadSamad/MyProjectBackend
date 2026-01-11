namespace Backend_Api.Models
{
    public class ProductSpecificationValueDTO
    {
        public int OptionId { get; set; }
        public string OptionValue { get; set; } = null!;
        public string SpecificationName { get; set; } = null!;
    }
}
