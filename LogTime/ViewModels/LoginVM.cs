using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Models;
using LogTime.Contracts;
using LogTime.Properties;
using LogTime.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace LogTime.ViewModels;

public partial class LoginVM(ILogTimeApiClient logTimeApiClient, ILoadingService loadingService, ILogService logService) : ObservableObject
{
    [ObservableProperty]
    private string userId = string.Empty;

    [ObservableProperty]
    private string password = string.Empty;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string loadingMessage = string.Empty;


    [RelayCommand]
    public async Task Login()
    {
        var app = (App)Application.Current;

        UserId = UserId.Trim().PadLeft(8, '0');

        var logEntry = new LogEntry
        {
            ClassName = nameof(LoginVM),
            MethodName = nameof(Login),
            LogMessage = "El usuario hizo clic en iniciar sesión.",
            UserId = UserId
        };

        try
        {
            logService.Log(logEntry);

            if (!ValidateCredentialFormat())
            {
                MessageBox.Show(Resource.CREDENTIALS_ERROR, Resource.AUTH_ERROR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            logEntry.LogMessage = "Validando credenciales.";
            logService.Log(logEntry);

            loadingService.Show("Validando credenciales, por favor espere...");

            var clientData = new ClientData
            {
                User = UserId,
                Password = Password,
                HostName = Environment.MachineName,
                ClientVersion = GlobalData.AppVersion,
            };

            var validateCredData = await logTimeApiClient.ValidateCredentialsAsync(clientData);
            loadingService.Close();

            if (validateCredData.Title.Equals(nameof(StatusMessage.Unauthorized)))
            {
                logEntry.LogMessage = Resource.CREDENTIALS_ERROR;
                logService.Log(logEntry);
                MessageBox.Show(Resource.CREDENTIALS_ERROR, Resource.AUTH_ERROR_TITLE, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            else if (validateCredData.Title.Equals(nameof(StatusMessage.NotAllowed)))
            {
                logEntry.LogMessage = Resource.RESTRICTED_ACCESS;
                logService.Log(logEntry);

                MessageBox.Show(Resource.RESTRICTED_ACCESS, Resource.RESTRICTED_ACCESS_TITLE, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            loadingService.Show("Buscando sesiones abiertas, por favor espere...");
            logEntry.LogMessage = "Buscando sesiones abiertas.";
            logService.Log(logEntry);

            var fetchSessionData = await logTimeApiClient.FetchSessionAsync(clientData.User);
            loadingService.Close();

            if (fetchSessionData.HasError)
            {
                logEntry.LogMessage = fetchSessionData.Message;
                logService.Log(logEntry);

                MessageBox.Show(fetchSessionData.Message, fetchSessionData.Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                if (fetchSessionData.IsAlreadyOpened)
                {
                    logEntry.LogMessage = "Se encontró una sesión activa.";
                    logService.Log(logEntry);

                    var message = string.Format(Resource.OPEN_SESSION_MESSAGE, UserId, fetchSessionData.CurrentRemoteHost);
                    var result = MessageBox.Show(message, Resource.OPEN_SESSION_TITLE, MessageBoxButton.YesNo, MessageBoxImage.Warning);

                    if (result == MessageBoxResult.Yes)
                    {
                        loadingService.Show("Iniciando sesión, por favor espere...");
                        logEntry.LogMessage = "El usuario decidio cerra la sesión activa.";
                        logService.Log(logEntry);

                        await logTimeApiClient.CloseSessionAsync(new SessionLogOutData { LoggedOutBy = clientData.User, UserIds = clientData.User });

                        logEntry.LogMessage = "Creando nueva sesión";
                        logService.Log(logEntry);

                        var newsessionData = await logTimeApiClient.OpenSessionAsync(clientData);

                        if (!newsessionData.HasError)
                        {
                            logEntry.LogMessage = "Nueva sesión creada.";
                            logService.Log(logEntry);

                            GlobalData.SessionData = newsessionData;
                            MainWindow mainWindow = app.ServiceProvider.GetRequiredService<MainWindow>();
                            mainWindow.Show();
                            app.LoginWindow.Close();
                        }
                        else
                        {
                            logEntry.LogMessage = newsessionData.Message;
                            logService.Log(logEntry);
                            MessageBox.Show(newsessionData.Message, newsessionData.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    else
                    {
                        logEntry.LogMessage = "El usuario decidio no cerra la sesión activa.";
                        logService.Log(logEntry);
                    }
                }
                else
                {
                    logEntry.LogMessage = "Creado nueva sesión.";
                    logService.Log(logEntry);

                    loadingService.Show("Iniciando sesión, por favor espere...");
                    var newsessionData = await logTimeApiClient.OpenSessionAsync(clientData);

                    if (!newsessionData.HasError)
                    {
                        logEntry.LogMessage = "Nueva sesión creada.";
                        logService.Log(logEntry);
                        GlobalData.SessionData = newsessionData;

                        GlobalData.SessionData = newsessionData;
                        MainWindow mainWindow = app.ServiceProvider.GetRequiredService<MainWindow>();
                        mainWindow.Show();
                        app.LoginWindow.Close();
                    }
                    else
                    {
                        loadingService.Close();
                        logEntry.LogMessage = newsessionData.Message;
                        logService.Log(logEntry);

                        MessageBox.Show(newsessionData.Message, newsessionData.Title, MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            loadingService.Close();
            logEntry.LogMessage = exception.Message;
            logService.Log(logEntry);
            var message = exception.Message;
            MessageBox.Show(message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            app.LoginWindow.IsEnabled = true;
            loadingService.Close();
        }
    }

    [RelayCommand]
    public static void ShowHelpDialog()
    {
        var helpDialogWindow = new HelpWindow();
        helpDialogWindow.Show();
    }
    private bool ValidateCredentialFormat()
    {
        return
        !string.IsNullOrEmpty(UserId)
            && !string.IsNullOrEmpty(Password)
            && UserId.All(char.IsDigit)
            && UserId?.Length <= 8;
    }
}
