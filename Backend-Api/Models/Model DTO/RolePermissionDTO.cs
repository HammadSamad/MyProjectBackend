namespace Backend_Api.Models.Model_DTO
{
    public class RolePermissionDTO
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public int PermissionId { get; set; }
        public string? PermissionName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
