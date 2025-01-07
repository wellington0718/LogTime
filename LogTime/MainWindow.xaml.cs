namespace LogTime;

public partial class MainWindow : Window
{

    public MainWindow(MainVM mainVM)
    {
        try
        {
            InitializeComponent();
            Application.Current.ThemeMode = ThemeMode.Dark;
            Title = GlobalData.AppNameVersion;
            DataContext = mainVM;
            App.Update();
        }
        catch (Exception ex)
        {

        }
    }

    private void OpenFlyout(object sender, MouseButtonEventArgs e) => FlyoutPopup.IsOpen = true;
    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();
    private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ShowHelpDialog(object sender, RoutedEventArgs e) => new HelpWindow().Show();
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
    private void ShowLogsDialog(object sender, RoutedEventArgs e)
    {

    }
}