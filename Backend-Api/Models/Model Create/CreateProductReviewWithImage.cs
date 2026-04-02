namespace Backend_Api.Models.Model_Create
{
    public class CreateProductReviewWithImage : CreateProductReview
    {
        public IFormFile? Image { get; set; }
    }
}
