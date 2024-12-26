namespace Domain.Models;

public class ProjectGroup
{
    public int Id { get; set; }
    public string ProjectId { get; set; }
    public string Name { get; set; }
    public string GroupDescription { get; set; }
    public DateTime? LogOutTime { get; set; }
}
