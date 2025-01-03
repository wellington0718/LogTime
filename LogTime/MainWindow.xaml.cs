namespace LogTime;

public partial class MainWindow : Window
{
    private readonly MainVM _mainVM;

    public MainWindow(MainVM mainVM, ILogService logService, FtpService ftpService)
    {
        InitializeComponent();
        Application.Current.ThemeMode = ThemeMode.Dark;
        Title = GlobalData.AppNameVersion;
        _mainVM = mainVM;
        DataContext = _mainVM;

        App.Update();
    }

    private void OpenFlyout(object sender, MouseButtonEventArgs e) => FlyoutPopup.IsOpen = true;

    private async void ChangeActivity(object sender, EventArgs e) => await _mainVM.ChangeActivity();

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

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