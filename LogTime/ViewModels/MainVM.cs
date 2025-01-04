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
            if (IsEarlyBreakChange() && !DialogBox.Show(Resource.EARLY_BREAK_CHANGE, Resource.EARLY_BREAK_TITLE))
            {
                CurrentStatusIndex = _previousStatusId;
                return;
            }

            if (CurrentStatusIndex == (int)SharedStatus.Lunch)
            {
                if (!DialogBox.Show(Resource.LUNCH_CONFIRMATION, "LogTime - Cierre de sesión", DialogBoxButton.YesNo, AlertType.Question))
                {
                    CurrentStatusIndex = _previousStatusId;
                    return;
                }

                await UpdateStatus(CurrentStatusIndex);
                await HandleCloseSession();
                Application.Current.Shutdown();
            }
            else
            {
                await UpdateStatus(CurrentStatusIndex);
            }
        }
        catch (Exception exception)
        {
            HandleException(exception.GetBaseException().Message, nameof(UpdateStatus));
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
            var closeSessionConfirmation = DialogBox.Show(Resource.CLOSE_SESSION_AND_RESTART_APP_ACONFIRMATION, "LogTime - Cierre de sesión", DialogBoxButton.YesNo, AlertType.Question);

            if (!closeSessionConfirmation) return;

            await HandleCloseSession();
            App.Restart();
        }
        catch (Exception exception)
        {
            HandleException(exception.GetBaseException().Message, nameof(UpdateStatus));
            App.Restart();
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
                        App.Restart();
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
        catch (Exception exception)
        {
            HandleException(exception.GetBaseException().Message, nameof(UpdateStatus));
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
        logEntry.MethodName = nameof(UpdateStatus);

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

            logService.Log(logEntry);
            activityTimeSpan = TimeSpan.Zero;

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
            HandleException(exception.GetBaseException().Message, nameof(UpdateStatus));
        }
    }

    private async Task HandleCloseSession()
    {
        loadingService.Show("Cerrando sesión, por favor espere...");
        logEntry.LogMessage = "Cerrando sesión.";
        logEntry.MethodName = nameof(HandleCloseSession);
        logService.Log(logEntry);

        var sessionLogOutData = new SessionLogOutData
        {
            UserIds = SessionData.User.Id,
            LoggedOutBy = CurrentStatusIndex == (int)SharedStatus.Lunch ? "Auto-logout: Lunch" : SessionData.User.Id
        };

        var closeSessionResponse = await logTimeApiClient.CloseSessionAsync(sessionLogOutData);

        if (HandleResponseErrors(closeSessionResponse)) return;

        logEntry.LogMessage = "Sesión cerrada.";
        logService.Log(logEntry);
    }

    private bool HandleResponseErrors(BaseResponse baseResponse)
    {
        logEntry.MethodName = nameof(HandleCloseSession);

        if (baseResponse.IsSessionAlreadyClose)
        {
            logEntry.LogMessage = Resource.SESSION_ALREADY_CLOSED;
            logService.Log(logEntry);
            DialogBox.Show(Resource.SESSION_ALREADY_CLOSED, Resource.SESSION_ALREADY_CLOSED_TITLE);
            App.Restart();
            return true;
        }

        if (baseResponse.HasError)
        {
            logEntry.LogMessage = baseResponse.Message;
            logService.Log(logEntry);

            sessionTimer.Stop();
            activityTimer.Stop();

            var retry = DialogBox.Show(Resource.RETRY_CLOSE_SESSION, Resource.RETRY_CLOSE_SESSION_TITLE);
            if (!retry)
                App.Restart();
            else
            {
                HandleCloseSession().ConfigureAwait(false);
            }

            return true;
        }

        return false;
    }

    private void HandleException(string errorMessage, string methodName)
    {
        logEntry.LogMessage += errorMessage;
        logEntry.MethodName += methodName;
        logService.Log(logEntry);
        DialogBox.Show(Resource.UNKNOWN_ERROR, Resource.UNKNOWN_ERROR_TITLE, alertType: AlertType.Error);
    }

    private bool IsEarlyBreakChange()
        => (_previousStatusId == (int)SharedStatus.Break && activityTimeSpan.Minutes < Constants.MinimumBreakDurationMinutes)
               && CurrentStatusIndex != (int)SharedStatus.Break;
}

