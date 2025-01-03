namespace LogTime.ViewModels;

public partial class MainVM : ObservableObject
{
    [ObservableProperty]
    private string sessionTime = "00:00:00";
    [ObservableProperty]
    private string activityTime = "00:00:00";
    [ObservableProperty]
    private string serverConnection;
    [ObservableProperty]
    private string loginDate;
    [ObservableProperty]
    private int currentStatusIndex;

    private readonly DispatcherTimer sessionTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer activityTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer idleTimer = new() { Interval = TimeSpan.FromSeconds(1) };

    private readonly ILogService logService;
    private readonly ILogTimeApiClient logTimeApiClient;
    private readonly ILoadingService loadingService;
    private TimeSpan sessionTimeSpan = new();
    private TimeSpan activityTimeSpan = new();
    private TimeSpan idleTimeSpan = new();
    private int _previousStatusId;
    private int? activityIdleTimeSeconds;
    private readonly LogEntry logEntry;

    private bool networkWasAlive;
    private bool isPcOnIdleState;
    private bool isPcLocked;
    private bool isScreenSaverRunning;
    private bool screenSaverLastState;

    public SessionData SessionData { get; }
    public bool IsShuttingDown { get; set; }
    public bool IsRestarting { get; set; }

    public MainVM()
    {
        var app = (App)Application.Current;
        SystemEvents.SessionSwitch += SystemEventsSessionSwitch;
        SystemEvents.SessionEnded += LoggingOff;
        networkWasAlive = true;
        SessionData = GlobalData.SessionData;
        var loginDateTime = GlobalData.SessionData.LogHistory.LoginDate;
        serverConnection = loginDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        loginDate = serverConnection;
        currentStatusIndex = (int)SharedStatus.NoActivity;
        _previousStatusId = currentStatusIndex;
        activityIdleTimeSeconds = 1; //SessionData.User.Project.Statuses.ToList()[currentStatusIndex].IdleTime * 60;
        sessionTimer.Tick += (s, e) => GeneralTimerTick(nameof(SessionTime)).ConfigureAwait(false);
        activityTimer.Tick += (s, e) => GeneralTimerTick(nameof(ActivityTime)).ConfigureAwait(false);
        sessionTimer.Start();
        activityTimer.Start();
        logService = app.ServiceProvider.GetRequiredService<ILogService>();
        logTimeApiClient = app.ServiceProvider.GetRequiredService<ILogTimeApiClient>();
        loadingService = app.ServiceProvider.GetRequiredService<ILoadingService>();
        logEntry = new LogEntry
        {
            ClassName = nameof(MainVM),
            UserId = SessionData.User?.Id,
        };
    }

    [DllImport("Sensapi")]
    private static extern bool IsNetworkAlive(out int flags);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref bool pvParam, uint fWinIni);

    private bool IsScreenSaverRunning()
    {
        SystemParametersInfo(Constants.SPI_GETSCREENSAVERRUNNING, 0, ref isScreenSaverRunning, 0);

        if ((!isScreenSaverRunning || screenSaverLastState) && (!screenSaverLastState || isScreenSaverRunning))
            return isScreenSaverRunning;

        var screenSaverStatusMessage = isScreenSaverRunning
            ? "Screen saver on."
            : "Screen saver off.";

        logEntry.MethodName = nameof(IsScreenSaverRunning);
        logEntry.LogMessage = screenSaverStatusMessage;

        logService.Log(logEntry);
        screenSaverLastState = isScreenSaverRunning;

        return isScreenSaverRunning;
    }

    public async Task ChangeActivity()
    {
        try
        {
            if (IsEarlyBreakChange() && !ShowBreakWarning())
            {
                RevertToPreviousStatus();
                return;
            }

            if (CurrentStatusIndex == (int)SharedStatus.Lunch)
            {
                await CloseSession();
            }
            else
            {
                await UpdateStatus(CurrentStatusIndex);
            }
        }
        catch (Exception ex)
        {
            HandleException(ex.GetBaseException().Message, nameof(ChangeActivity),
                Resource.ACTIVITY_CHANGE_ERROR, Resource.ACTIVITY_CHANGE_ERROR_TITLE);
        }
    }

    private void LoggingOff(object sender, SessionEndedEventArgs e)
    {
        logEntry.MethodName = nameof(LoggingOff);
        logEntry.LogMessage = string.Format("Automatic logout: Shutting down or signing out of the Operating System. Reason: {0}", e.Reason);

        logService.Log(logEntry);
        Thread.Yield();

        Application.Current.Shutdown();
    }

    private void SystemEventsSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        logEntry.MethodName = nameof(SystemEventsSessionSwitch);

        switch (e.Reason)
        {
            case SessionSwitchReason.SessionLock:
                logEntry.LogMessage = "OS session locked.";
                isPcLocked = isPcOnIdleState = true;
                break;
            case SessionSwitchReason.SessionUnlock:
                logEntry.LogMessage = "OS session unlocked.";
                isPcLocked = isPcOnIdleState = false;
                break;

            default:
                logEntry.LogMessage = "Unknown session switch reason.";
                logService.Log(logEntry);
                break;
        }

        logService.Log(logEntry);
    }

    [RelayCommand]
    public async Task CloseSession()
    {
        try
        {
            var closeSessionConfirmation = ConfirmCloseSession();
            var isLunchStatusIndex = CurrentStatusIndex == (int)SharedStatus.Lunch;

            if (!closeSessionConfirmation && isLunchStatusIndex)
            {
                RevertToPreviousStatus();
                return;
            }
            else if (!closeSessionConfirmation)
            {
                return;
            }

            if (isLunchStatusIndex)
            {
                await UpdateStatus(CurrentStatusIndex);
                IsShuttingDown = true;
            }

            await HandleCloseSession();
            if (IsShuttingDown)
            {
                Application.Current.Shutdown();
            }
            else
            {
                RestartApp();
            }
        }
        catch (Exception ex)
        {
            HandleException(ex.GetBaseException().Message,
                nameof(CloseSession),
                Resource.UNKNOWN_ERROR, Resource.UNKNOWN_ERROR_TITLE);

            RestartApp();
        }
        finally
        {
            loadingService.Close();
        }
    }

    private async Task GeneralTimerTick(string propertyName)
    {
        try
        {
            if (!IsNetworkAlive(out _))
            {
                logEntry.MethodName = nameof(GeneralTimerTick);

                if (networkWasAlive)
                {
                    loadingService.Show("Cable/Adaptador de red desconectado. Esperando reconexión.");
                    logEntry.LogMessage = "Local network connection lost.";
                    logService.Log(logEntry);
                    networkWasAlive = false;
                }
                else
                {
                    if (sessionTimeSpan.TotalSeconds % 30 == 0)
                    {
                        loadingService.Show("El adaptador o cable de red estuvo desconectado por mucho tiempo. Reiniciando applicación.");
                        logEntry.LogMessage = "The network adapter has been down for too long. Restarting app.";
                        RestartApp();
                        return;
                    }
                }
            }
            else
            {
                if (!networkWasAlive)
                {
                    loadingService.Close();
                    logEntry.LogMessage = "Network connection restablished.";
                    logService.Log(logEntry);
                    networkWasAlive = true;
                }

                if (IsScreenSaverRunning())
                {
                    if (!isPcOnIdleState)
                    {
                        isPcOnIdleState = true;
                    }
                }
                else if (!isPcLocked)
                {
                    isPcOnIdleState = false;
                }

                if (isPcOnIdleState)
                {
                    idleTimeSpan = idleTimeSpan.Add(new TimeSpan(0, 0, 1));

                    if (idleTimeSpan.TotalSeconds >= activityIdleTimeSeconds)
                    {
                        loadingService.Show("Cerrando sesión debido a que el tiempo de inactividad fue exedido.");
                        var sessionLogOutData = new SessionLogOutData
                        {
                            Id = SessionData.ActiveLog.ActualLogHistoryId,
                            LoggedOutBy = "Auto-logout: Idle time"
                        };

                        var CloseSessionResponse = await logTimeApiClient.CloseSessionAsync(sessionLogOutData);
                        HandleResponseErrors(CloseSessionResponse);
                    }

                }
                else
                {
                    idleTimeSpan = TimeSpan.Zero;
                }

                switch (propertyName)
                {
                    case nameof(SessionTime):
                        sessionTimeSpan = sessionTimeSpan.Add(TimeSpan.FromSeconds(1));
                        SessionTime = sessionTimeSpan.ToString(@"hh\:mm\:ss");
                        await UpdateSessionAliveDateAsync();
                        await CloseSessionByUserGroupLogOutTimeReached();
                        break;
                    case nameof(ActivityTime):
                        activityTimeSpan = activityTimeSpan.Add(TimeSpan.FromSeconds(1));
                        ActivityTime = activityTimeSpan.ToString(@"hh\:mm\:ss"); ;
                        break;
                }
            }
        }
        catch (Exception ex)
        {
            HandleException(Resource.UNKNOWN_ERROR, nameof(GeneralTimerTick),
                ex.GetBaseException().Message, Resource.UNKNOWN_ERROR_TITLE);
        }
    }

    private async Task CloseSessionByUserGroupLogOutTimeReached()
    {
        var logOutTime = SessionData.User?.ProjectGroup?.LogOutTime;

        if (logOutTime.HasValue && logOutTime.Value.TimeOfDay == DateTime.Now.TimeOfDay)
        {
            var sessionLogOutData = new SessionLogOutData
            {
                Id = SessionData.ActiveLog.ActualLogHistoryId,
                LoggedOutBy = "Auto-logout: logout time reached",
            };

            var closeSessionResponse = await logTimeApiClient.CloseSessionAsync(sessionLogOutData);
            HandleResponseErrors(closeSessionResponse);
        }
    }

    private async Task UpdateSessionAliveDateAsync()
    {
        if (sessionTimeSpan.Minutes % 1 == 0 && sessionTimeSpan.Seconds == 0)
        {
            var updateSessionAliveDateResponse = await logTimeApiClient.UpdateSessionAliveDateAsync(SessionData.ActiveLog.ActualLogHistoryId);
            var hasResponseErros = HandleResponseErrors(updateSessionAliveDateResponse);

            if (!hasResponseErros && updateSessionAliveDateResponse.LastDate is not null)
                ServerConnection = updateSessionAliveDateResponse.LastDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }

    private async Task UpdateStatus(int newStatusIndex)
    {
        try
        {
            if (newStatusIndex != (int)SharedStatus.Lunch)
                if (CurrentStatusIndex != _previousStatusId && newStatusIndex != (int)SharedStatus.Lunch)
                    loadingService.Show("Cambiando de actividad, por favor espere...");


            CurrentStatusIndex = newStatusIndex;
            _previousStatusId = CurrentStatusIndex;
            var activity = GlobalData.SessionData.User.Project.Statuses.ToList()[newStatusIndex];
            activityIdleTimeSeconds = activity.IdleTime * 60;

            logEntry.LogMessage = $"Cambiando a actividad de {activity.Description}";
            logEntry.MethodName = nameof(UpdateStatus);

            logService.Log(logEntry);
            ResetActivityTimer();

            var statusChange = new StatusHistoryChange
            {
                Id = SessionData.ActiveLog.ActualStatusHistoryId,
                NewActivityId = activity.Id,
            };

            var statusHistoryChangeResponse = await logTimeApiClient.ChangeActivityAsync(statusChange);
            SessionData.ActiveLog.ActualStatusHistoryId = statusHistoryChangeResponse.Id;

            if (HandleResponseErrors(statusHistoryChangeResponse))
                return;

            if (HandleResponseErrors(statusHistoryChangeResponse))
                return;

            ServerConnection = statusHistoryChangeResponse.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
            loadingService.Close();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Error de actividad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task HandleCloseSession()
    {
        loadingService.Show("Cerrando sesión, por favor espere...");
        logService.Log(new LogEntry { LogMessage = "Cerrando sesión.", ClassName = nameof(MainVM) });

        var updateSessionAliveDateResponse = await logTimeApiClient.UpdateSessionAliveDateAsync(SessionData.LogHistory.Id);
        if (HandleResponseErrors(updateSessionAliveDateResponse)) return;

        var sessionLogOutData = new SessionLogOutData
        {
            UserIds = SessionData.User.Id,
            LoggedOutBy = CurrentStatusIndex == (int)SharedStatus.Lunch ? "Auto-logout: Lunch" : SessionData.User.Id
        };

        var closeSessionResponse = await logTimeApiClient.CloseSessionAsync(sessionLogOutData);
        if (HandleResponseErrors(closeSessionResponse)) return;

        logService.Log(new LogEntry { LogMessage = "Sesión cerrada.", ClassName = nameof(MainVM) });
    }

    private bool HandleResponseErrors(BaseResponse baseResponse)
    {
        if (baseResponse.IsSessionAlreadyClose)
        {
            DialogBox.Show(Resource.CLOSE_SESSION, Resource.CLOSE_SESSION_TITLE);
            RestartApp();
            return true;
        }

        if (baseResponse.HasError)
        {
            sessionTimer.Stop();
            activityTimer.Stop();

            var retry = ShowError(Resource.RETRY_CLOSE_SESSION, Resource.RETRY_CLOSE_SESSION_TITLE);
            if (!retry)
                RestartApp();

            return true;
        }

        return false;
    }


    private bool ConfirmCloseSession()
    {
        var action = IsShuttingDown ? "salir de la aplicación" : "reiniciar la aplicación";
        var prompt = CurrentStatusIndex != (int)SharedStatus.Lunch
            ? $"¿Seguro que deseas cerrar la sesión y {action}?"
            : "El estado de Lunch terminará la sesión y cerrará la aplicación. ¿Seguro que deseas continuar?";

        return ShowConfirmation(prompt, "LogTime - Cierre de sesión");
    }

    private void HandleException(string showMessage, string methodName, string errorMessage, string title)
    {
        logService.Log(new LogEntry { LogMessage = showMessage, MethodName = methodName });
        ShowError(errorMessage, title);
    }

    private static bool ShowBreakWarning() => ShowConfirmation(Resource.EARLY_BREAK_CHANGE, Resource.EARLY_BREAK_TITLE);

    private void RevertToPreviousStatus() => CurrentStatusIndex = _previousStatusId;

    public void RestartApp()
    {
        IsRestarting = true;
        IsShuttingDown = false;
        App.Restart();
    }

    private void ResetActivityTimer() => activityTimeSpan = TimeSpan.Zero;

    private static bool ShowConfirmation(string message, string title)
        => MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static void ShowInfo(string message, string title)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);

    private static bool ShowError(string message, string title)
        => MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error) == MessageBoxResult.Yes;

    private bool IsEarlyBreakChange()
        => (_previousStatusId == (int)SharedStatus.Break && activityTimeSpan.Minutes < Constants.MinimumBreakDurationMinutes)
               && CurrentStatusIndex != (int)SharedStatus.Break;
}

