using LogTime.ViewModels;
using System.Windows;
using System.Windows.Input;
namespace LogTime;

public partial class Login : Window
{
    private readonly LoginVM _loginVM;

    public Login(LoginVM loginVM)
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
