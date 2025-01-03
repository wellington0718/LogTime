namespace LogTime;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; private set; }
    private static readonly string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    private static FtpService? _ftpService;

    public Login LoginWindow { get; }

    public App()
    {
        Configuration = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .Build();

        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        ServiceProvider = serviceCollection.BuildServiceProvider();
        LoginWindow = ServiceProvider.GetRequiredService<Login>();
        _ftpService = ServiceProvider.GetRequiredService<FtpService>();
    }

    public static void Restart()
    {
        var executablePath = Environment.ProcessPath;
        if (executablePath != null)
        {
            Process.Start(executablePath);
            Current.Shutdown();
        }
    }

    public static async void Update()
    {
        try
        {
            var appUpdatesLocalFolderPath = $"{localApplicationData}/{Constants.AppLocalUpdatesFolderPath}";
            await _ftpService!.DownloadFiles(Constants.AppReleases);

            using var mgr = new UpdateManager(appUpdatesLocalFolderPath);

            if (mgr.IsInstalledApp)
            {
                var updateAppInfo = await mgr.CheckForUpdate();

                if (updateAppInfo.ReleasesToApply.Count != 0)
                {
                    await _ftpService.DownloadFiles(Constants.NUPKG);
                    var update = await mgr.UpdateApp();

                    var newVersion = $"{update.Version.Major}.{update.Version.Minor}";
                    Directory.Delete(appUpdatesLocalFolderPath, true);

                    Current.Dispatcher.Invoke(() =>
                    {
                        var newVersionMessage = $"¡Una nueva versión de LogTime ({newVersion}) está disponible! Los cambios se aplicarán cuando reinicies la aplicación.";
                        MessageBox.Show(newVersionMessage, "LogTime - Actualización Disponible", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(ex.Message, "LogTime - Error de actualización", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        LoginWindow.Show();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<LoginVM>();
        services.AddTransient<MainVM>();
        services.AddTransient<LoadingVM>();

        services.AddTransient<Login>();
        services.AddTransient<MainWindow>();
        services.AddTransient<Loading>();

        services.AddSingleton<ILoadingService, LoadingService>();
        services.AddSingleton(Configuration);
        services.AddSingleton<FtpService>();
        services.AddSingleton<ILogService, LogService>();
        services.AddHttpClient<ILogTimeApiClient, LogTimeApiClient>();
    }
}
