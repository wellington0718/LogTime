using LogTime.Client.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace LogTime.Client;

public partial class MainWindow : Window
{
    public MainWindow(MainVM mainVM)
    {
        InitializeComponent();
        Application.Current.ThemeMode = ThemeMode.Dark;
        CopyRightText.Text = $"SYNERGIES © {DateTime.Now.Year}";
        Title = GlobalData.AppNameVersion;
        DataContext = mainVM;
    }

    private void OpenFlyout(object sender, MouseButtonEventArgs e)
    {
        double popupWidth = FlyoutPopup.Child.RenderSize.Width;
        FlyoutPopup.HorizontalOffset = (popupWidth == 0 ? -300 : -popupWidth);
        FlyoutPopup.VerticalOffset = 0;
        FlyoutPopup.IsOpen = true;
    }
}