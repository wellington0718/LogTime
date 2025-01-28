namespace LogTime.Windows;

public partial class LoginWindow : Window
{
    private readonly LoginVM _loginVM;

    public LoginWindow(LoginVM loginVM)
    {
        InitializeComponent();
        _loginVM = loginVM;
        DataContext = _loginVM;
        Title = GlobalData.AppNameVersion;
    }

    private async void ControlKeyDownEvent(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            await _loginVM.Login();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e) => DragMove();

    private void ShutdownApplication(object sender, RoutedEventArgs e) => Application.Current.Shutdown();

    private void MinimizeWindow(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
}
