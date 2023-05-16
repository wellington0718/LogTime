using Infrastructure.UnitsOfWork;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;
using UI.ViewModels;
using UI.Views;

namespace Logtime.UI;

public partial class App : Application, IDisposable
{
    private static IHost? appHost;
    public static Window? LoginWindow { get; set; }

    public App()
    {
        CreateDefaultBuilder();
    }

    private static void CreateDefaultBuilder()
    {
        appHost = Host.CreateDefaultBuilder()
             .ConfigureServices((context, services) =>
             {
                 ConfigureServices(services);
             })
             .ConfigureAppConfiguration(config =>
             {
                 config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                 var CurrentDirectory = Directory.GetCurrentDirectory();

#if DEBUG
                 config.AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true);
#else
                     config.AddJsonFile("appsettings.Production.json", optional: true, reloadOnChange: true);
#endif

             }).Build();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();
        services.AddSingleton<LoginWindow>();
        services.AddSingleton<LoginViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<ISessionUnitOfWork, SessionUnitOfWork>();
    }

    protected async override void OnStartup(StartupEventArgs e)
    {
        await appHost!.StartAsync();
        LoginWindow = appHost.Services.GetRequiredService<LoginWindow>();
        LoginWindow.Show();

        SystemEvents.SessionSwitch +=
      new SessionSwitchEventHandler(SystemEvents_SessionSwitch);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        appHost!.StopAsync();
    }

    void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        //switch (e.Reason)
        //{
        //    case SessionSwitchReason.SessionLock:

        //        if (!LoginWindow!.IsActive)
        //        {
        //            LoginWindow.Show();
        //            Current.MainWindow.Close();
        //        }

        //        break;

        //    case SessionSwitchReason.SessionUnlock:

        //        if (IsSessionActive)
        //        {
        //            IsSessionActive = false;
        //            MessageBox.Show("The Session was closed do to the OS got clocked, please log in again!");
        //        }

        //        break;
        //}
    }

    public void Dispose()
    {
        SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
    }
}
