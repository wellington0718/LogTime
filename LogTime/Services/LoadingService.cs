namespace LogTime.Services;

public class LoadingService : ILoadingService
{
    private Loading? _loading;
    private LoadingVM? _viewModel;

    public void Show(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_loading == null)
            {
                _viewModel = new LoadingVM { Message = message };
                _loading = new Loading
                {
                    DataContext = _viewModel,
                    Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible)
                };

                if (_loading.Owner != null)
                    _loading.Owner.IsEnabled = false;

                _loading.Show();
                return;
            }

            _viewModel!.Message = message;

        });
    }

    public void Close()
    {
        Application.Current?.Dispatcher.Invoke(() =>
            {
                if (_loading != null)
                {
                    if (_loading.Owner != null)
                        _loading.Owner.IsEnabled = true;

                    _loading.Close();
                    _loading = null;
                }
            });
    }
}
