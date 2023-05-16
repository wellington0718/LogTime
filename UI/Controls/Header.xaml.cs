using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace UI.Controls;

public partial class Header : UserControl
{
    public Header()
    {
        InitializeComponent();
    }

    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        ApplicationHelper.DragWindow(e);
    }

    private void MinimizeApplication_Click(object sender, RoutedEventArgs e)
    {
        ApplicationHelper.MinimizeApp(e);
    }

    private void CloseApplication_Click(object sender, RoutedEventArgs e)
    {
        ApplicationHelper.ShutdownApp();
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        ApplicationHelper.RestartApp();
    }
}
