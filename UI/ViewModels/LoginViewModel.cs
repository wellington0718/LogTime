using DataAccess.Models;
using Domain.Models;
using Infrastructure.UnitsOfWork;
using Logtime.UI;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using UI.Commands;
using UI.Views;

namespace UI.ViewModels;

public class LoginViewModel : ViewModelBase
{

    public LoginViewModel(ISessionUnitOfWork SessionUnitOfWork, IServiceProvider serviceProvider)
    {
        sessionUnitOfWork = SessionUnitOfWork;
        _serviceProvider = serviceProvider;
        LoginCommand = new RelayCommand(Login, CanLogin);
    }

    private string? _userId;
    private string? _password;
    private string? _errorMessage;
    private readonly ISessionUnitOfWork sessionUnitOfWork;
    private readonly IServiceProvider _serviceProvider;

    public string? UserId
    {
        get => _userId;

        set
        {
            _userId = value;
            RaisePropertyChanged();
        }
    }
    public string? Password
    {
        get => _password;

        set
        {
            _password = value;
            RaisePropertyChanged();
        }
    }
    public string? ErrorMessage
    {
        get => _errorMessage;

        set
        {
            _errorMessage = value;
            RaisePropertyChanged();
        }
    }

    public ICommand LoginCommand { get; }

    private async void Login(object? parameter)
    {
        if (!string.IsNullOrWhiteSpace(_password))
        {
            var credentail = new Credential
            {
                Password = _password,
                User = _userId!.PadLeft(8, '0'),
            };

            var isValid = await sessionUnitOfWork.CredentialRepository.ValidateAsync(credentail);

            if (isValid)
            {
                Thread.CurrentPrincipal = new AccountPrincipal(credentail.User);

                Application.Current.MainWindow = _serviceProvider.GetRequiredService<MainWindow>();
                Application.Current.MainWindow.Show();
                App.LoginWindow?.Close();
            }
            else
            {
                ErrorMessage = "Wrong Id or Password, please try again!";
            }
        }
    }

    private bool CanLogin(object? parameter) =>
         !string.IsNullOrWhiteSpace(_userId) && !string.IsNullOrWhiteSpace(_password);
}
