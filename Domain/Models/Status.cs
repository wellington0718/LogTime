namespace Domain.Models;

public class Status
{
    public int Id { get; set; }
    public string Description { get; set; }
    public string Message { get; set; }
    public string Project { get; set; }
    public int? IdleTime { get; set; }
    public bool Enabled { get; set; }
}
