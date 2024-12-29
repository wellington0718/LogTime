using Domain.Models;
using LogTime.Contracts;
using System.IO;

namespace LogTime.Services;

public class LogService : ILogService
{
    public void Log(LogEntry logEntry)
    {
        var filePath = $"C:\\LogTimeLogs\\{logEntry.UserId}.log";
        var header = "Version         | Date                      | Class Name                | Method Name                                        | Log Message";
        var separator = new string('-', logEntry.ToString().Length);

        var directory = Path.GetDirectoryName(filePath);
        var fileExists = File.Exists(filePath);

        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var writer = new StreamWriter(filePath, append: true);
      
        if (!fileExists)
        {
            writer.WriteLine(header);
            writer.WriteLine(separator);
        }

        logEntry.Version = GlobalData.AppVersion;

        writer.WriteLine(logEntry);
        writer.WriteLine(separator);
    }
}
