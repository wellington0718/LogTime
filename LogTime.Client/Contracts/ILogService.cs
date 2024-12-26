using Domain.Models;

namespace LogTime.Client.Contracts;

public interface ILogService
{
    void Log(LogEntry logEntry);
}
