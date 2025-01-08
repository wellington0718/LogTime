using System.Collections.ObjectModel;

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

    public void ShowLog(string userId)
    {
        var logName = $"{userId}.log";
        string filePath = $"C:\\LogTimeLogs/{logName}";

        if (!File.Exists(filePath)) {
            DialogBox.Show($"El archivo ({logName}) no existe.", "LogTime - Archivo No Existe", DialogBoxButton.OK, AlertType.Error);
            return;
        }

        var logLines = File.ReadAllLines(filePath).Skip(1).Where(line => !line.StartsWith('-'));
        ObservableCollection<LogEntry> logs = new(logLines.Select(ParseLogString));

        var fileLogWindow = new FileLogWindow
        {
            Title = logName,
            DataContext = new { Logs = logs }
        };

        fileLogWindow.Show();
    }

    private LogEntry ParseLogString(string logString)
    {
        try
        {
            var parts = logString.Split('|').Select(p => p.Trim()).ToArray();

            return new LogEntry
            {
                Version = parts[0],
                Date = parts[1],
                ClassName = parts[2],
                MethodName = parts[3],
                LogMessage = parts[4]
            };

        }
        catch (Exception ex)
        {

            throw;
        }
    }
}
