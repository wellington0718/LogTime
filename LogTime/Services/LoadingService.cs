namespace LogTime.Services;

public class LoadingService : ILoadingService
{
    private Loading? _loading;
    private Window? _ownerWindow;
    public static dynamic? ViewModel { get; set; }

    public void Show(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_loading == null)
            {
                InitializeLoading(message);
            }
            else
            {
                UpdateMessage(message);
            }
        });
    }

    public void Close()
    {
        Application.Current?.Dispatcher.Invoke(() =>
        {
            if (_loading != null)
            {
                DetachOwnerHandlers();
                _loading.Close();
                _loading = null;

                if (ViewModel != null)
                {
                    ViewModel.IsInteractionEnabled = true;
                    ViewModel = null;
                }
            }
        });
    }

    private void InitializeLoading(string message)
    {
        var currentWindow = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsVisible);
        if (currentWindow is not null )
        {
            ViewModel = currentWindow.DataContext;
            ViewModel.Message = message;
        }

        _loading = new Loading
        {
            DataContext = ViewModel,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Owner = currentWindow,
        };

        _loading.CancelBtn.Visibility = Visibility.Collapsed;

        if (_loading?.Owner != null)
        {
            _ownerWindow = _loading.Owner;
            AttachOwnerHandlers();
        }

        _loading?.Show();

        if (ViewModel != null)
        {
            ViewModel.IsInteractionEnabled = false;
        }

        CenterLoadingWindow();
    }

    private void UpdateMessage(string message)
    {
        if (message.Contains("Intento"))
        {
            _loading!.CancelBtn.Visibility = Visibility.Visible;
        }
        else
        {
            _loading!.CancelBtn.Visibility = Visibility.Collapsed;
        }

        if (ViewModel != null)
        {
            ViewModel.Message = message;
        }
    }

    private void CenterLoadingWindow()
    {
        if (_loading != null && _loading.Owner != null)
        {
            var owner = _loading.Owner;
            _loading.Left = owner.Left + (owner.Width - _loading.Width) / 2;
            _loading.Top = owner.Top + (owner.Height - _loading.Height) / 2;
        }
    }

    private void AttachOwnerHandlers()
    {
        if (_loading?.Owner != null)
        {
            WeakEventManager<Window, EventArgs>.AddHandler(
                _loading.Owner,
                nameof(_loading.Owner.LocationChanged),
                OwnerWindowChanged
            );
        }
    }

    private void DetachOwnerHandlers()
    {
        if (_loading?.Owner != null)
        {
            WeakEventManager<Window, EventArgs>.RemoveHandler(
                _loading.Owner,
                nameof(_loading.Owner.LocationChanged),
                OwnerWindowChanged
            );
        }
    }

    private void OwnerWindowChanged(object? sender, EventArgs e) => CenterLoadingWindow();
}

