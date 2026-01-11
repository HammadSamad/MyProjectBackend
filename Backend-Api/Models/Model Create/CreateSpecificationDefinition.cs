namespace Backend_Api.Models.Model_Create
{
    public class CreateSpecificationDefinition
    {
        public string? SpecificationName { get; set; }
        public string? DataType { get; set; }   // text, number, bool, option
        public bool? IsVariant { get; set; }
    }
}
