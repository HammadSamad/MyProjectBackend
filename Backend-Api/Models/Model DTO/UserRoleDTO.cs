namespace Backend_Api.Models.Model_DTO
{
    public class UserRoleDTO
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string? RoleName { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
