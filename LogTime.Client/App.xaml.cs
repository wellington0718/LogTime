using LogTime.Client.Contracts;
using LogTime.Client.Services;
using LogTime.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace LogTime.Client;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }
    // Import Win32 API functions
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_RESTORE = 9; // Restore the window if minimized

    public Login LoginWindow { get; }

    public App()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        ServiceProvider = serviceCollection.BuildServiceProvider();
        LoginWindow = ServiceProvider.GetRequiredService<Login>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        const string mutexName = "MyUniqueAppMutexName";

        _ = new Mutex(true, mutexName, out bool isNewInstance);

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

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddTransient<LoginVM>();
        services.AddTransient<MainVM>();
        services.AddTransient<LoadingVM>();

        services.AddTransient<Login>();
        services.AddTransient<MainWindow>();
        services.AddTransient<Loading>();

        services.AddSingleton<ILoadingService, LoadingService>();
        services.AddSingleton<ILogService, LogService>();
        services.AddHttpClient<ILogTimeApiClient, LogTimeApiClient>();
    }
}
