namespace Backend_Api.Models
{
    public class CreateCategory
    {
        public int? ParentCategoryId { get; set; }
        public string CategoryName { get; set; } = null!;
        public IFormFile? CategoryImage { get; set; }
    }
}
