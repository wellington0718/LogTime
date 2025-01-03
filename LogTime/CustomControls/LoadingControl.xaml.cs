namespace LogTime.CustomControls;

public partial class LoadingControl : UserControl
{
    public LoadingControl()
    {
        InitializeComponent();
    }

    // DependencyProperty for LoadingMessage
    public static readonly DependencyProperty MessageProperty =
        DependencyProperty.Register("LoadingMessage", typeof(string), typeof(LoadingControl), new PropertyMetadata("Loading, please wait..."));

    public string LoadingMessage
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    // DependencyProperty for IsIndeterminate
    public static readonly DependencyProperty IsIndeterminateProperty =
        DependencyProperty.Register("IsIndeterminate", typeof(bool), typeof(LoadingControl), new PropertyMetadata(true));

    public bool IsIndeterminate
    {
        get => (bool)GetValue(IsIndeterminateProperty);
        set => SetValue(IsIndeterminateProperty, value);
    }

    // DependencyProperty for IsLoading (Visibility)
    public static readonly DependencyProperty IsLoadingProperty =
        DependencyProperty.Register("IsLoading", typeof(bool), typeof(LoadingControl), new PropertyMetadata(false));

    public bool IsLoading
    {
        get => (bool)GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }
}
