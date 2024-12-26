using LogTime.Client.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace LogTime.Client;

public partial class MainWindow : Window
{
    private readonly MainVM _mainVM;
    private bool _isShuttingDown = false;

    public MainWindow(MainVM mainVM)
    {
        InitializeComponent();
        Application.Current.ThemeMode = ThemeMode.Dark;
        CopyRightText.Text = $"SYNERGIES © {DateTime.Now.Year}";
        Title = GlobalData.AppNameVersion;
        _mainVM = mainVM;
        this.Closing += OnWindowClosing;
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
        if (_isShuttingDown)
            return;

        _isShuttingDown = true;

        e.Cancel = true;

        try
        {
            await _mainVM.CloseSession(_isShuttingDown);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"An error occurred while closing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

            _isShuttingDown = false;
            e.Cancel = false;
        }
    }
}