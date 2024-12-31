using LogTime.Contracts;
using LogTime.Services;
using LogTime.Utils;
using LogTime.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace LogTime;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; private set; }


    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9;

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

    protected override void OnStartup(StartupEventArgs e)
    {
        _ = new Mutex(true, Constants.MutexName, out bool isNewInstance);

        if (!isNewInstance)
        {
            BringExistingAppToFront();
            Current.Shutdown();
            return;
        }

        LoginWindow.Show();
    }

    private static void BringExistingAppToFront()
    {
        var currentProcess = Process.GetCurrentProcess();
        foreach (var process in Process.GetProcessesByName(currentProcess.ProcessName))
        {
            if (process.Id != currentProcess.Id) // Find the other instance
            {
                IntPtr handle = process.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    // Bring the window to the foreground
                    SetForegroundWindow(handle);
                    ShowWindow(handle, SW_RESTORE); // Restore if minimized
                }
                break;
            }
        }
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
