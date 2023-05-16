using Domain.Models;
using Infrastructure.UnitsOfWork;
using System.Threading;

namespace UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    public MainViewModel(ISessionUnitOfWork sessionUnitOfWork)
    {
        _sessionUnitOfWork = sessionUnitOfWork;
        LoadCurrentUserData();
    }

    private async void LoadCurrentUserData()
    {
        if (Thread.CurrentPrincipal is not null)
        {
            var accountPrincipal = (AccountPrincipal)Thread.CurrentPrincipal;
            var user = await _sessionUnitOfWork.UserRepository.GetInfo(accountPrincipal.Id);

            User = new CurrentUserViewModel(user);
        }
    }

    private CurrentUserViewModel? _user;
    private readonly ISessionUnitOfWork _sessionUnitOfWork;

    public CurrentUserViewModel? User
    {
        get => _user;

        set
        {
            _user = value;
            RaisePropertyChanged();
        }
    }
}
