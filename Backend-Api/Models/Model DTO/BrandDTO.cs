namespace Backend_Api.Models.Model_DTO
{
    public class BrandDTO
    {
        public int BrandId { get; set; }
        public string? BrandName { get; set; }

        public List<ProductDTO> Products { get; set; } = new();
    }
}
