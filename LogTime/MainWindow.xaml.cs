using Domain.Models;
using LogTime.Contracts;
using LogTime.Services;
using LogTime.Utils;
using LogTime.ViewModels;
using Squirrel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
        CopyRightText.Text = $"SYNERGIES © {DateTime.Now.Year}";
        Title = GlobalData.AppNameVersion;
        _mainVM = mainVM;
        _logService = logService;
        _ftpService = ftpService;
        Closing += OnWindowClosing;
        DataContext = _mainVM;

        UpdateApp();
    }

    private void OpenFlyout(object sender, MouseButtonEventArgs e)
    {
        double popupWidth = FlyoutPopup.Child.RenderSize.Width;
        FlyoutPopup.HorizontalOffset = (popupWidth == 0 ? -300 : -popupWidth);
        FlyoutPopup.VerticalOffset = 0;
        FlyoutPopup.IsOpen = true;
    }

    private async void OnWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        e.Cancel = true;

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
                MethodName = nameof(OnWindowClosing),
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

    private async void ChangeActivity(object sender, EventArgs e)
    {
        await _mainVM.ChangeActivity();
    }
}