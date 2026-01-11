using System.ComponentModel.DataAnnotations;
namespace Backend_Api.Models
{
    public class CreateProduct
    {
        public int CategoryId { get; set; }
        public int BrandId { get; set; }
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public int? WarrantyMonths { get; set; }
        public bool? IsActive { get; set; }
    }
}
