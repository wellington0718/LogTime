namespace Domain.Models;

public class FtpSettings
{
    public string FtpHost { get; set; }
    public string Username { get; set; }
    public int FtpPort { get; set; }
    public string Password { get; set; }
    public string FtpUpdatesFolderPath { get; set; }
}
