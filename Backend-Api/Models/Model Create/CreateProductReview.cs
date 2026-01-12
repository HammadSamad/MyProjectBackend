using System.ComponentModel.DataAnnotations;

namespace Backend_Api.Models.Model_Create
{
    public class CreateProductReview
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
        public int Rating { get; set; }

        public string? ReviewText { get; set; }
    }
}
