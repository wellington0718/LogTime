using Domain.Models;
using LogTime.Client.Contracts;
using LogTime.Client.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace LogTime.Client;

public partial class MainWindow : Window
{
    private readonly MainVM _mainVM;
    private readonly ILogService _logService;

    public MainWindow(MainVM mainVM, ILogService logService)
    {
        InitializeComponent();
        Application.Current.ThemeMode = ThemeMode.Dark;
        CopyRightText.Text = $"SYNERGIES © {DateTime.Now.Year}";
        Title = GlobalData.AppNameVersion;
        _mainVM = mainVM;
        _logService = logService;
        Closing += OnWindowClosing;
        DataContext = _mainVM;
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

}