namespace LogTime;

public partial class DialogBox : Window
{
    public string ImageSource { get; set; }

    public string Message { get; set; }
    public string Caption { get; set; }
    public MessageBoxResult Result { get; private set; }

    public DialogBox(string message, string caption, DialogBoxButton boxButton, AlertType alertType)
    {
        InitializeComponent();
        DataContext = this;
        Caption = caption;
        Message = message;
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);

        ConfigureButtons(boxButton);
        ImageSource = GetImageSource(alertType);
    }

    public static MessageBoxResult Show(string caption, string message, DialogBoxButton buttonGroup = DialogBoxButton.Ok, AlertType alertType = AlertType.Info)
    {
        var messageBox = new DialogBox(caption, message, buttonGroup, alertType);
        messageBox.ShowDialog();
        return messageBox.Result;
    }

    private static string GetImageSource(AlertType alertType)
    {
        return alertType switch
        {
            AlertType.Info => "Images/info.png",
            AlertType.Warning => "Images/warning.png",
            AlertType.Error => "Images/error.png",
            _ => ""
        };
    }

    private void ConfigureButtons(DialogBoxButton dialogBoxButton)
    {
        ButtonPanel.Children.Clear(); // Clear any existing buttons

        switch (dialogBoxButton)
        {
            case DialogBoxButton.Ok:
                AddButton("OK", MessageBoxResult.OK);
                break;

            case DialogBoxButton.OkCancel:
                AddButton("OK", MessageBoxResult.OK);
                AddButton("Cancel", MessageBoxResult.Cancel);
                break;

            case DialogBoxButton.YesNo:
                AddButton("Yes", MessageBoxResult.Yes);
                AddButton("No", MessageBoxResult.No);
                break;

            case DialogBoxButton.RetryCancel:
                AddButton("Retry", MessageBoxResult.OK);
                AddButton("Cancel", MessageBoxResult.Cancel);
                break;
        }
    }

    private void AddButton(string content, MessageBoxResult result)
    {
        var button = new Button
        {
            Content = content,
            Width = 75,
            Margin = new Thickness(5),
            IsDefault = content == "OK" || content == "Yes", // Default action button
            IsCancel = content == "Cancel" || content == "No" // Cancel action button
        };

        button.Click += (sender, e) =>
        {
            Result = result;
            Close();
        };

        ButtonPanel.Children.Add(button);
    }
}
