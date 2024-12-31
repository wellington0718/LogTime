using LogTime.ViewModels;
using System.Windows;
namespace LogTime;

public partial class Login : Window
{
    public Login(LoginVM loginVM)
    {
        InitializeComponent();
        DataContext = loginVM;
        Title = GlobalData.AppNameVersion;
        CopyRightText.Text = $"SYNERGIES © {DateTime.Now.Year}";
    }
}
