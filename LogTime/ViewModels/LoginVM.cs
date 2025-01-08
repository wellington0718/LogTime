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
                DialogBox.Show(Resource.CREDENTIALS_ERROR, Resource.AUTH_ERROR_TITLE, DialogBoxButton.OK, AlertType.Error);
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
                DialogBox.Show(Resource.CREDENTIALS_ERROR, Resource.AUTH_ERROR_TITLE, DialogBoxButton.OK, AlertType.Error);
                return;
            }
            else if (validateCredData.Title.Equals(nameof(StatusMessage.NotAllowed)))
            {
                logEntry.LogMessage = Resource.RESTRICTED_ACCESS;
                logService.Log(logEntry);

                DialogBox.Show(Resource.RESTRICTED_ACCESS, Resource.RESTRICTED_ACCESS_TITLE, DialogBoxButton.OK, AlertType.Information);
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

                DialogBox.Show(fetchSessionData.Message, fetchSessionData.Title, DialogBoxButton.OK, AlertType.Error);
            }
            else
            {
                if (fetchSessionData.IsAlreadyOpened)
                {
                    logEntry.LogMessage = "Se encontró una sesión activa.";
                    logService.Log(logEntry);

                    var message = string.Format(Resource.OPEN_SESSION_MESSAGE, UserId, fetchSessionData.CurrentRemoteHost);
                    var dialogResult = DialogBox.Show(message, Resource.OPEN_SESSION_TITLE, DialogBoxButton.YesNo, AlertType.Warning);

                    if (dialogResult)
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
                            App.ShowWindow<MainWindow>();
                            App.CloseWindow<LoginWindow>();
                        }
                        else
                        {
                            logEntry.LogMessage = newsessionData.Message;
                            logService.Log(logEntry);
                            DialogBox.Show(newsessionData.Message, newsessionData.Title, DialogBoxButton.OK, AlertType.Error);
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
                        App.ShowWindow<MainWindow>();
                        App.CloseWindow<LoginWindow>();
                    }
                    else
                    {
                        loadingService.Close();
                        logEntry.LogMessage = newsessionData.Message;
                        logService.Log(logEntry);

                        DialogBox.Show(newsessionData.Message, newsessionData.Title, DialogBoxButton.OK, AlertType.Error);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            loadingService.Close();
            logEntry.LogMessage = exception.Message;
            logService.Log(logEntry);
            ExceptionService.Handle(exception.Message);
        }
        finally
        {
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
