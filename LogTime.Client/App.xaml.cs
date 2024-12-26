using LogTime.Client.Contracts;
using LogTime.Client.Services;
using LogTime.Client.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace LogTime.Client;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }
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
        LoginWindow.Show();
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
