namespace LogTime.Windows;

public partial class HelpWindow : Window
{
    public string AppDescription { get; }
    public string AppVersion { get; }
    public string CopyRight { get; }

    public HelpWindow()
    {
        InitializeComponent();
        DataContext = this;
        CopyRight = string.Format(Resource.COPY_RIGHT, DateTime.Now.Year, Environment.NewLine);
        AppVersion = $"Versión {GlobalData.AppVersion}";
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);

        AppDescription = Resource.APP_DESCRIPTION;
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

        Close();
    }
}
