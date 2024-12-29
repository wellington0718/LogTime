using Domain.Models;

namespace LogTime.Contracts;

public interface ILogService
{
    void Log(LogEntry logEntry);
}
