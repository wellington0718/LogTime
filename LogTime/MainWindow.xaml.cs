using Domain.Models;
using LogTime.Contracts;
using LogTime.Services;
using LogTime.Utils;
using LogTime.ViewModels;
using Squirrel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace LogTime;

public partial class MainWindow : Window
{
    private readonly MainVM _mainVM;
    private readonly ILogService _logService;
    private readonly FtpService _ftpService;
    private readonly string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    public MainWindow(MainVM mainVM, ILogService logService, FtpService ftpService)
    {
        InitializeComponent();
        Application.Current.ThemeMode = ThemeMode.Dark;
        Title = GlobalData.AppNameVersion;
        _mainVM = mainVM;
        _logService = logService;
        _ftpService = ftpService;
        DataContext = _mainVM;

        UpdateApp();
    }

    private void OpenFlyout(object sender, MouseButtonEventArgs e)
    {
        FlyoutPopup.IsOpen = true;
    }

    private async void UpdateApp()
    {
        try
        {
            var appUpdatesLocalFolderPath = $"{localApplicationData}/{Constants.AppLocalUpdatesFolderPath}";
            await _ftpService.DownloadFiles(Constants.AppReleases);

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

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var newVersionMessage = $"¡Una nueva versión de LogTime ({newVersion}) está disponible! Los cambios se aplicarán cuando reinicies la aplicación.";
                        MessageBox.Show(newVersionMessage, "LogTime - Actualización Disponible", MessageBoxButton.OK, MessageBoxImage.Information);
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(ex.Message, "LogTime - Error de actualización", MessageBoxButton.OK, MessageBoxImage.Error);
            });
        }
    }

    private async void ChangeActivity(object sender, EventArgs e) => await _mainVM.ChangeActivity();

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private async void ShutdownApplication(object sender, RoutedEventArgs e)
    {
        if (_mainVM.IsShuttingDown || _mainVM.IsRestarting)
            return;

        _mainVM.IsShuttingDown = true;

        try
        {
            await _mainVM.CloseSession();
        }
        catch (Exception exception)
        {
            var logEntry = new LogEntry
            {
                ClassName = nameof(MainWindow),
                MethodName = nameof(ShutdownApplication),
                UserId = GlobalData.SessionData.User?.Id,
                LogMessage = exception.GetBaseException().Message
            };

            _logService.Log(logEntry);

            MessageBox.Show(
                $"({DateTime.Now}) Un error desconocido ocurrió al intentar cerrar la sesión.",
                "Error de sesión",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );

            _mainVM.RestartApp();
        }
    }

    private void NavigateToUrl(object sender, RoutedEventArgs e)
    {
        try
        {
            var endPoint = (string)((MenuItem)sender).Tag;
            string baseUrl = "http://intranet/SynergiesSystem/LogTime";

            baseUrl = endPoint switch
            {
                "Activities" => $"{baseUrl}/Activities",
                "Groups" => $"{baseUrl}/SaveGroupInactivityTime",
                "UsersSessions" => $"{baseUrl}/UsersSessions",
                _ => baseUrl,
            };

            Process.Start(new ProcessStartInfo
            {
                FileName = baseUrl,
                UseShellExecute = true 
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to open browser: {ex.Message}");
        }
    }

    private void ShowHelpDialog(object sender, RoutedEventArgs e)
    {
        var helpDialogWindow = new HelpWindow();
        helpDialogWindow.Show();
    }

    private void ShowLogsDialog(object sender, RoutedEventArgs e)
    {

    }
}