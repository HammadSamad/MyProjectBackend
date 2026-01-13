namespace Backend_Api.Models.Model_DTO
{
    public class RoleDTO
    {
        public int RoleId { get; set; }
        public string RoleName { get; set; } = null!;
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
