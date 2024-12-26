using CommunityToolkit.Mvvm.ComponentModel;

namespace LogTime.Client.ViewModels;

public partial class LoadingVM : ObservableObject
{
    [ObservableProperty]
    private string message = string.Empty;
}
