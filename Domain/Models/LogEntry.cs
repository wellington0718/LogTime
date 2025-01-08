namespace Domain.Models;

public class LogEntry
{
    public string Version { get; set; }
    public string Date { get; set; }
    public string ClassName { get; set; }
    public string MethodName { get; set; }
    public string LogMessage { get; set; }
    public string UserId { get; set; }

    public override string ToString()
    {
        return string.Format(
         "{0,-15} | {1,-25} | {2,-25} | {3,-50} | {4}",
         Version,
         Date = DateTime.Now.ToString("yyyy/mm/dd hh:MM:ss tt"),
         ClassName,
         MethodName,
         LogMessage);
    }
}
