using System.Diagnostics;
using System.Windows;

namespace LogTime;

public partial class HelpWindow : Window
{
    public HelpWindow()
    {
        InitializeComponent();
        CopyRightText.Text = $"© Copyright {DateTime.Now.Year}, Synergies Corps. \n Todos los derechos reservados.";
        AppNameVersion.Text = GlobalData.AppNameVersion;
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);

        if (Owner != null)
        {
            Owner.IsEnabled = false;

            Closed += (s, v) =>
            {
                Owner.IsEnabled = true;
            };
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        if (Owner != null)
            Owner.IsEnabled = true;

        Close();
    }

    private void NavigateToUrl(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = e.Uri.AbsoluteUri,
            UseShellExecute = true
        });
    }
}
