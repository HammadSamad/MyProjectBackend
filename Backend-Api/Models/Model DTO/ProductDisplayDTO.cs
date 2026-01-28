namespace Backend_Api.Models.Model_DTO
{
    public class ProductDisplayDTO
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? ProductImage { get; set; }
        public decimal ProductPrice { get; set; }   // minimum variant price
        public double AverageRating { get; set; }
    }
}
