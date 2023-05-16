namespace DataAccess.Models
{
    public class User : BaseResponse
    {
        public string? Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Initials => $"{FirstName?.Substring(0, 1)}{LastName?.Substring(0,1)}";

        public string? Email { get; set; }
        public int RoleId { get; set; }
        public Project? Project { get; set; }
        public ProjectGroup? ProjectGroup { get; set; }
    }
}
