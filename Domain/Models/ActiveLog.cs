namespace Domain.Models;

public class ActiveLog
{
    public int Id { get; set; }
    public string UserId { get; set; }

    public int ActualLogHistoryId { get; set; }
    public int ActualStatusHistoryId { get; set; }
    public int StatusId { get; set; }
    public string ClientVersion { get; set; }

    public LogHistory LogHistory { get; set; }
}
