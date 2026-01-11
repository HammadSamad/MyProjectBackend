namespace Backend_Api.Models.Model_Create
{
    public class CreateProductReview
    {
        public int ProductId { get; set; }
        public int Rating { get; set; }
        public string? ReviewText { get; set; }
    }
}
