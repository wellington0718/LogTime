namespace LogTime.Services;

public class FtpService
{
    private readonly IConfiguration _configuration;
    private readonly string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private readonly FtpSettings ftpSettings = new();

    public FtpService(IConfiguration configuration)
    {
        _configuration = configuration;
        _configuration.Bind("FtpSettings", ftpSettings);
    }

    public async Task DownloadFiles(string fileName)
    {
        var networkCredential = new NetworkCredential(ftpSettings.Username, ftpSettings.Password);
        var cong = new FtpConfig()
        {
            EncryptionMode = FtpEncryptionMode.Explicit,
            SslProtocols = System.Security.Authentication.SslProtocols.Tls12,
            ValidateAnyCertificate = true,
            DataConnectionType = FtpDataConnectionType.AutoPassive,
        };

        using var ftpClient = new AsyncFtpClient(ftpSettings.FtpHost, networkCredential, ftpSettings.FtpPort, cong);
        await ftpClient.Connect();

        var appUpdatesLocalFolderPath = $"{localApplicationData}/{Constants.AppLocalUpdatesFolderPath}";

        Directory.CreateDirectory(appUpdatesLocalFolderPath);

        if (fileName.Equals(Constants.AppReleases))
        {
            await ftpClient.DownloadFile($"{appUpdatesLocalFolderPath}/{Constants.AppReleases}", $"{ftpSettings.FtpUpdatesFolderPath}/{Constants.AppReleases}");
        }
        else if (fileName.Equals(Constants.NUPKG))
        {
            var filesToDownload = (await ftpClient.GetListing(ftpSettings.FtpUpdatesFolderPath))
               .Where(f => f.Name.EndsWith(".nupkg"))
               .OrderByDescending(f => f.Name)
               .Take(2)
               .Select(f => f.FullName)
               .ToList();

            await ftpClient.DownloadFiles($"{appUpdatesLocalFolderPath}", filesToDownload);
        }

        await ftpClient.Disconnect();

    }
}
