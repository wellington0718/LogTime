using Domain.Models;
using System.Reflection;

namespace LogTime.Client;

public class GlobalData
{
    public static bool IsUsingClientVpn { get; set; } = false;
    public static string LastConnectedHost { get; set; } = string.Empty;
    public static SessionData SessionData { get; set; } = new();

    public static string? AppVersion => Assembly.GetExecutingAssembly().GetName().Version?.ToString(2);
    public static string AppNameVersion => $"LOGTIME {AppVersion}";
}
