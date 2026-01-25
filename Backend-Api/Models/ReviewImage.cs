using System.Collections.Generic;

namespace Backend_Api.Models;

    public class ReviewImage
    {
    public int ReviewImageId { get; set; }
    public int ReviewId { get; set; }
    public string ImageUrl { get; set; } = null!;
    
    public ProductReview Review { get; set; } = null!;
}

