namespace Backend_Api.Models
{
    public class CreateProductImage
    {
        public int ProductId { get; set; }

        public IFormFile? CoverImageUrl { get; set; }

        public IFormFile[]? ImageUrl { get; set; }
    }
}
