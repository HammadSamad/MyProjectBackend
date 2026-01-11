namespace Backend_Api.Models
{
    public class ProductImageDTO
    {
        public string? CoverImage { get; set; }
        public List<string> GalleryImages { get; set; } = new();
    }
}
