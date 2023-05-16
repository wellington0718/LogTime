using Microsoft.Extensions.Configuration;
using System.Windows;
using System.Windows.Input;
using UI.ViewModels;

namespace UI.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _model;

    public LoginWindow(LoginViewModel model)
    {
        InitializeComponent();
        UserIdBox.Focus();
        _model = model;
        DataContext = _model;
    }

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void BtnMinimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void HandleKeyPressed(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter) {
            _model.LoginCommand.Execute(null);
        }
    }
}
