namespace Backend_Api.Models.Model_DTO
{
    public class SearchHistoryDTO
    {
        public long SearchId { get; set; }
        public int? UserId { get; set; }
        public string? SearchText { get; set; }
        public DateTime? SearchedAt { get; set; }
    }
}
