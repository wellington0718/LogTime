namespace Domain.Models;

public class LogHistory
{
    public int Id { get; set; }
    public string Hostname { get; set; }
    public string IdUser { get; set; }
    public DateTime? LastTimeConnectionAlive { get; set; }
    public DateTime LoginDate { get; set; }
    public DateTime? LogoutDate { get; set; }
    public string LogedOutBy { get; set; }
    public string ClientVersion { get; set; }

    public ICollection<ActiveLog> ActiveLogs { get; set; } = [];
    public ICollection<StatusHistory> StatusHistories { get; set; } = [];
}
