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

    private readonly static DispatcherTimer generalTimer = new() { Interval = TimeSpan.FromSeconds(1)};
    private readonly ILogService logService;
    private readonly ILogTimeApiClient logTimeApiClient;
    private readonly ILoadingService loadingService;
    private TimeSpan sessionTimeSpan;
    private TimeSpan activityTimeSpan;
    private TimeSpan idleTimeSpan;
    private int previousStatusId;
    private int? activityIdleTimeSeconds;
    private readonly LogEntry logEntry;
    private bool networkWasAlive;
    private bool isPcOnIdleState;
    private bool isPcLocked;
    private bool isScreenSaverRunning;
    private bool screenSaverLastState;
    private bool canHandleStatusChange = true;

    public SessionData SessionData { get; }

    public MainVM(ILogService logService, ILogTimeApiClient logTimeApiClient, ILoadingService loadingService)
    {
        var loginDateTime = GlobalData.SessionData.LogHistory.LoginDate;
        this.logService = logService;
        this.logTimeApiClient = logTimeApiClient;
        this.loadingService = loadingService;
        SystemEvents.SessionSwitch += SystemEventsSessionSwitch;
        SystemEvents.SessionEnded += LoggingOff;
        networkWasAlive = true;
        SessionData = GlobalData.SessionData;
        serverConnection = loginDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        loginDate = serverConnection;
        currentStatusIndex = (int)SharedStatus.NoActivity;
        previousStatusId = currentStatusIndex;
        activityIdleTimeSeconds = 1;//SessionData.User.Project.Statuses.ToList()[currentStatusIndex].IdleTime * 60;
        logEntry = new LogEntry { ClassName = nameof(MainVM), UserId = SessionData.User?.Id, };
        generalTimer.Start();
        generalTimer.Tick += GeneralTimerTick;
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

    [RelayCommand]
    public async Task ChangeActivity(Status selectedStatus)
    {
        try
        {
            var selectedStatusIndex = SessionData.User.Project.Statuses.ToList().IndexOf(selectedStatus);
            previousStatusId = CurrentStatusIndex;

            if (IsEarlyBreakChange(selectedStatusIndex))
            {
                canHandleStatusChange = ConfirmEarlyBreakChange();

                if (!canHandleStatusChange)
                {
                    RevertToPrevoiusStatus();
                    canHandleStatusChange = false;
                    return;
                }
            }

            if (selectedStatusIndex == (int)SharedStatus.Lunch)
            {
                canHandleStatusChange = ConfirmLunchChange();
                if (!canHandleStatusChange)
                {
                    RevertToPrevoiusStatus();
                    return;
                }

                await HandleLunchSessionClose(selectedStatus);
            }

            if (canHandleStatusChange)
            {
                await HandleStatusChange(selectedStatus, selectedStatusIndex);
            }
        }
        catch (Exception exception)
        {
            HandleException(exception.GetBaseException().Message, nameof(HandleStatusChange));
        }
        finally
        {
            canHandleStatusChange = true;
        }

    }

    [RelayCommand]
    public void ShowLogFile() => logService.ShowLog(SessionData.User.Id);

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
    public async Task CloseSession(string shutDown)
    {
        try
        {
            var isShutDown = !string.IsNullOrEmpty(shutDown);
            var action = isShutDown ? "salir la aplicación" : "reiniciar la aplicación";
            var closeSessionConfirmation = DialogBox.Show(string.Format(Resource.CLOSE_SESSION_ACONFIRMATION, action),
                Resource.CLOSE_SESSION_CONFIRMATION_TITLE, DialogBoxButton.YesNo, AlertType.Question);

            if (!closeSessionConfirmation) return;

            await HandleCloseSession();

            if (!string.IsNullOrEmpty(shutDown))
                Application.Current.Shutdown();
            else
                App.Restart();
        }
        catch (Exception exception)
        {
            HandleException(exception.GetBaseException().Message, nameof(HandleStatusChange));
            App.Restart();
        }
        finally
        {
            loadingService.Close();
        }
    }

    [RelayCommand]
    public async Task RefreshServerConnection() => await UpdateSessionAliveDateAsync(true);

    private async void GeneralTimerTick(object? sender, EventArgs e)
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
                            UserIds = SessionData.ActiveLog.UserId,
                            LoggedOutBy = "Auto-logout: Idle time"
                        };

                        var CloseSessionResponse = await logTimeApiClient.CloseSessionAsync(sessionLogOutData);
                        var hasError = HandleResponseErrors(CloseSessionResponse);

                        if (!hasError)
                        {
                            var result = DialogBox.Show("La sesión fue cerrada debido a que el tiempo de inactividad fue exedido", "LogTime - Tiempo de inactividad", alertType: AlertType.Information);
                            App.Restart();
                        }
                    }
                }
                else
                {
                    idleTimeSpan = TimeSpan.Zero;
                }

                sessionTimeSpan = sessionTimeSpan.Add(TimeSpan.FromSeconds(1));
                SessionTime = sessionTimeSpan.ToString(@"hh\:mm\:ss");
                await UpdateSessionAliveDateAsync(false);
                await CloseSessionByUserGroupLogOutTimeReached();

                activityTimeSpan = activityTimeSpan.Add(TimeSpan.FromSeconds(1));
                ActivityTime = activityTimeSpan.ToString(@"hh\:mm\:ss"); ;
            }
        }
        catch (Exception exception)
        {
            loadingService.Close();
            HandleException(exception.GetBaseException().Message, nameof(HandleStatusChange));
        }
        finally
        {
            loadingService.Close();
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

    private async Task UpdateSessionAliveDateAsync(bool isRefreshing)
    {
        
        if (sessionTimeSpan.Minutes % 1 == 0 && sessionTimeSpan.Seconds == 0 || isRefreshing)
        {
            if (isRefreshing)
            {
                loadingService.Show("Actualizando datos de conexión, por favor espere...");
            }

            var updateSessionAliveDateResponse = await logTimeApiClient.UpdateSessionAliveDateAsync(SessionData.ActiveLog.ActualLogHistoryId);
            var hasResponseErros = HandleResponseErrors(updateSessionAliveDateResponse);

            if (!hasResponseErros && updateSessionAliveDateResponse.LastDate is not null)
                ServerConnection = updateSessionAliveDateResponse.LastDate.Value.ToString("yyyy-MM-dd HH:mm:ss");

            loadingService.Close();
        }
    }

    private async Task HandleStatusChange(Status selectedStatus, int selectedStatusIndex)
    {
        try
        {
            logEntry.MethodName = nameof(HandleStatusChange);

            loadingService.Show("Cambiando de actividad, por favor espere...");
           // activityIdleTimeSeconds = selectedStatus.IdleTime * 60;
            logEntry.LogMessage = $"Cambiando a actividad de {selectedStatus.Description}";
            logService.Log(logEntry);

            var statusChange = new StatusHistoryChange
            {
                Id = SessionData.ActiveLog.ActualStatusHistoryId,
                NewActivityId = selectedStatus.Id,
            };

            var statusHistoryChangeResponse = await logTimeApiClient.ChangeActivityAsync(statusChange);

            if (HandleResponseErrors(statusHistoryChangeResponse))
                return;

            CurrentStatusIndex = selectedStatusIndex;
            SessionData.ActiveLog.ActualStatusHistoryId = statusHistoryChangeResponse.Id;
            ServerConnection = statusHistoryChangeResponse.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
            activityTimeSpan = TimeSpan.Zero;
            loadingService.Close();
        }
        catch (Exception exception)
        {
            HandleException(exception.GetBaseException().Message, nameof(HandleStatusChange));
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
            generalTimer?.Stop();

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
        ExceptionService.Handle(errorMessage);
    }

    private void RevertToPrevoiusStatus() => CurrentStatusIndex = previousStatusId;

    private static bool ConfirmEarlyBreakChange() =>
         DialogBox.Show(Resource.EARLY_BREAK_CHANGE, Resource.EARLY_BREAK_TITLE, DialogBoxButton.YesNo);

    private static bool ConfirmLunchChange() =>
         DialogBox.Show(Resource.LUNCH_CONFIRMATION, "LogTime - Cierre de sesión", DialogBoxButton.YesNo, AlertType.Question);

    private async Task HandleLunchSessionClose(Status selectedStatus)
    {
        await HandleStatusChange(selectedStatus, (int)SharedStatus.Lunch);
        await HandleCloseSession();
        Application.Current.Shutdown();
    }
    private bool IsEarlyBreakChange(int selectedStatusIndex) => !(previousStatusId != (int)SharedStatus.Break
        || activityTimeSpan.Minutes >= Constants.MinimumBreakDurationMinutes
        || selectedStatusIndex == (int)SharedStatus.Break);

    internal static void HandleGeneralTimerTickOnRetry(bool isRetring)
    {
        if(isRetring)
        {
            generalTimer?.Stop();
        }
        else
        {
            generalTimer?.Start();
        }
    }
}