using Domain.Models;
using LogTime.Contracts;
using LogTime.Services;
using LogTime.ViewModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;

namespace LogTime;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }
    public IConfiguration Configuration { get; private set; }

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
