namespace Backend_Api.Models.Model_DTO
{
    public class PermissionDTO
    {
        public int PermissionId { get; set; }
        public string PermissionName { get; set; } = null!;
        public string? PermissionPath { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
