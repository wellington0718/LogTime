using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Models;
using LogTime.Contracts;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;

namespace LogTime.ViewModels;

public partial class MainVM : ObservableObject
{
    private readonly DispatcherTimer sessionTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer activityTimer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly ILogService logService;
    private readonly ILogTimeApiClient logTimeApiClient;
    private readonly ILoadingService loadingService;
    private TimeSpan sessionTimeSpan = new();
    private TimeSpan activityTimeSpan = new();

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

    private int _previousStatusId;
    private readonly LogEntry logEntry;
    private const int MinimumBreakDurationMinutes = 2;

    public SessionData SessionData { get; }
    public bool IsShuttingDown { get; set; }
    public bool IsRestarting { get; set; }

    public MainVM()
    {
        var app = (App)Application.Current;
        SessionData = GlobalData.SessionData;
        var loginDateTime = GlobalData.SessionData.LogHistory.LoginDate;
        serverConnection = loginDateTime.ToString("yyyy-MM-dd HH:mm:ss");
        loginDate = serverConnection;
        currentStatusIndex = (int)SharedStatus.NoActivity;
        _previousStatusId = currentStatusIndex;

        sessionTimer.Tick += (s, e) => UpdateTimer(ref sessionTimeSpan, nameof(SessionTime));
        activityTimer.Tick += (s, e) => UpdateTimer(ref activityTimeSpan, nameof(ActivityTime));
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
            HandleException(ex, nameof(ChangeActivity), "Ocurrió un error al intentar cambiar la actividad.");
        }
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
            else if(!closeSessionConfirmation)
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
            HandleException(ex, nameof(CloseSession), "Un error ocurrió al intentar cerrar la sesión.");
            RestartApp();
        }
        finally
        {
            loadingService.Close();
        }
    }

    private void UpdateTimer(ref TimeSpan timeSpan, string propertyName)
    {
        timeSpan = timeSpan.Add(TimeSpan.FromSeconds(1));
        var timeString = timeSpan.ToString(@"hh\:mm\:ss");

        switch (propertyName)
        {
            case nameof(SessionTime):
                SessionTime = timeString; 
                break;
            case nameof(ActivityTime):
                ActivityTime = timeString; 
                break;
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

    private bool HandleResponseErrors(BaseResponse response)
    {
        if (response.IsSessionAlreadyClose)
        {
            ShowInfo("La sesión ya había sido cerrada por alguien más o por un servicio.");
            RestartApp();
            return true;
        }

        if (response.HasError)
        {
            var retry = ShowError("Ocurrió un error. Puedes intentar cerrar sesión nuevamente o reiniciar la aplicación.");
            if (!retry) RestartApp();
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

        return ShowConfirmation(prompt);
    }

    private void HandleException(Exception ex, string methodName, string errorMessage)
    {
        logService.Log(new LogEntry { LogMessage = ex.GetBaseException().Message, MethodName = methodName });
        ShowError(errorMessage);
    }

    private static bool ShowBreakWarning() => ShowConfirmation("Advertencia: El cambio de estado es temprano. ¿Deseas continuar?");

    private void RevertToPreviousStatus() => CurrentStatusIndex = _previousStatusId;

    public void RestartApp()
    {
        IsRestarting = true;
        IsShuttingDown = false;
        App.Restart();
    }

    private void ResetActivityTimer() => activityTimeSpan = TimeSpan.Zero;

    private static bool ShowConfirmation(string message)
        => MessageBox.Show(message, "Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;

    private static void ShowInfo(string message)
        => MessageBox.Show(message, "Información", MessageBoxButton.OK, MessageBoxImage.Information);

    private static bool ShowError(string message)
        => MessageBox.Show(message, "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes;

    private bool IsEarlyBreakChange()
        => (_previousStatusId == (int)SharedStatus.Break && activityTimeSpan.Minutes < MinimumBreakDurationMinutes)
               && CurrentStatusIndex != (int)SharedStatus.Break;
}

