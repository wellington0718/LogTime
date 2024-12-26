using System.Windows;
using System.Windows.Controls;

namespace LogTime.Client.CustomControls;

public partial class BindablePasswordBox : UserControl
{
    public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register("Password", typeof(string), typeof(BindablePasswordBox));

    public string Password
    {
        get => (string) GetValue(PasswordProperty);
        set => SetValue(PasswordProperty, value);
    }


    public BindablePasswordBox()
    {
        InitializeComponent();
        PasswordBox.PasswordChanged += OnPasswordChange;
    }

    private void OnPasswordChange(object sender, RoutedEventArgs e) => Password = PasswordBox.Password;

}
