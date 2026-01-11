using Backend_Api.Models.Model_DTO;

namespace Backend_Api.Models
{
    public class ProductDTO
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public int? WarrantyMonths { get; set; }
        public bool? IsActive { get; set; }

        public string? BrandName { get; set; }
        public string? CategoryName { get; set; }

        public string? CoverImage { get; set; }
        public List<string> GalleryImages { get; set; } = new();

        public List<ProductVariantDTO> Variants { get; set; } = new();
        public List<ProductSpecificationDTO> Specifications { get; set; } = new();
        public DateTime? CreatedAt { get; internal set; }
        public DateTime? UpdatedAt { get; internal set; }
    }
}
