using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UI.ViewModels;

public class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public void RaisePropertyChanged([CallerMemberName] string? propertyNmae = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyNmae));
    }
}
