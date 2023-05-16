using System.Diagnostics;
using System.Windows;
using System.Windows.Input;

namespace UI;

public class ApplicationHelper
{
    public static void RestartApp()
    {
        var currentExecutablePath = Process.GetCurrentProcess().MainModule?.FileName;

        if (currentExecutablePath != null)
        {
            Process.Start(currentExecutablePath);
            Application.Current.Shutdown();
        }
    }

    public static void DragWindow(MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            Application.Current.MainWindow.DragMove();
        }
    }

    public static void MinimizeApp(RoutedEventArgs e)
    {
        Application.Current.MainWindow.WindowState = WindowState.Minimized;
    }

    public static void ShutdownApp()
    {
        Application.Current.Shutdown();
    }
}
