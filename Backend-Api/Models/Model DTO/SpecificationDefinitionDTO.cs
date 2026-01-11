namespace Backend_Api.Models.Model_DTO
{
    public class SpecificationDefinitionDTO
    {
        public int SpecificationId { get; set; }
        public string? SpecificationName { get; set; }
        public string? DataType { get; set; }
        public bool? IsVariant { get; set; }
    }
}
