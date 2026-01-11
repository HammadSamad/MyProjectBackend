using System.ComponentModel.DataAnnotations;

namespace Backend_Api.Models
{
    public class CreateCategory
    {
        public int? ParentCategoryId { get; set; }
        [Required(ErrorMessage = "Category name is required")]
        public string CategoryName { get; set; } = null!;
        public IFormFile? CategoryImage { get; set; }
    }
}
