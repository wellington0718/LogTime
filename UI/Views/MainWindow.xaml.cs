using Logtime.UI;
using System.Windows;
using UI.ViewModels;

namespace UI.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel model)
    {
        InitializeComponent();
        IsWaitingIndicatorVisible = false;
        DataContext = model;
    }

    public bool IsWaitingIndicatorVisible { get; set; }
}
