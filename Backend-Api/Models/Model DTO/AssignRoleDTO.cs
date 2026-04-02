namespace Backend_Api.Models.Model_DTO
{
    public class AssignRoleDTO
    {
        public int UserId { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
    }
}
