namespace LogTime.Contracts;

public interface ILogService
{
    void Log(LogEntry logEntry);
    void ShowLog(string userId);
}
