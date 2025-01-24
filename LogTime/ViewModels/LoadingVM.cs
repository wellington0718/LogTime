namespace LogTime.ViewModels;

public partial class LoadingVM : ObservableObject
{
    [ObservableProperty]
    private string message = string.Empty;
    [ObservableProperty]
    private bool isInteractionEnabled;

    [RelayCommand]
    public void CancelRetry() => LoadingService.CancellationTokenSource?.Cancel();

}
