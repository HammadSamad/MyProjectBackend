namespace Backend_Api.Models
{
    public class CreateProductSpecificationValue
    {
        public string SpecificationName { get; set; } = null!;
        public string DataType { get; set; } = null!;
        public bool IsVariant { get; set; }
    }
}
