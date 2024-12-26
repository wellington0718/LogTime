namespace Domain.Models;

public class ClientData
{
    public string User { get; set; }
    public string Password { get; set; }
    public string HostName { get; set; }
    public string ClientVersion { get; set; }
    public string LoggedOutBy { get; set; }
}
