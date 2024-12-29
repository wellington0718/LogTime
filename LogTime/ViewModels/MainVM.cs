using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Models;
using LogTime.Contracts;
using LogTime.Properties;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;

namespace LogTime.ViewModels;

public partial class MainVM : ObservableObject
{
    private readonly DispatcherTimer sessionTimer;
    private readonly DispatcherTimer activityTimer;
    private readonly ILogService logService;
    private readonly ILogTimeApiClient logTimeApiClient;
    private readonly ILoadingService loadingService;
    private TimeSpan sessionTimeSpan;
    private TimeSpan activityTimeSpan;

    [ObservableProperty]
    private string sessionTime;
    [ObservableProperty]
    private string activityTime;
    [ObservableProperty]
    private string serverConnection;
    [ObservableProperty]
    private string loginDate;
    [ObservableProperty]
    private int currentStatusIndex;

    private int _previousStatusId;
    private readonly LogEntry logEntry;
    const int MinimumBreakDurationMinutes = 2;

    public SessionData SessionData { get; }
    public bool IsShuttingDown { get; set; }
    public bool IsRestarting { get; set; }

    public MainVM()
    {
        var app = (App)Application.Current;
        SessionData = GlobalData.SessionData;
        ServerConnection = GlobalData.SessionData.LogHistory.LoginDate.ToString("yyyy-MM-dd HH:mm:ss");
        LoginDate = GlobalData.SessionData.LogHistory.LoginDate.ToString("yyyy-MM-dd HH:mm:ss");
        CurrentStatusIndex = (int)SharedStatus.NoActivity;
        _previousStatusId = CurrentStatusIndex;
        sessionTimer = new();
        activityTimer = new();
        sessionTimeSpan = new();
        activityTimeSpan = new();

        sessionTime = "00:00:00";
        activityTime = "00:00:00";

        sessionTimer.Interval = TimeSpan.FromSeconds(1);
        activityTimer.Interval = TimeSpan.FromSeconds(1);
        sessionTimer.Tick += SessionTimerTick;
        activityTimer.Tick += ActivityTimerTick;
        sessionTimer.Start();
        activityTimer.Start();
        logService = app.ServiceProvider.GetRequiredService<ILogService>();
        logTimeApiClient = app.ServiceProvider.GetRequiredService<ILogTimeApiClient>();
        loadingService = app.ServiceProvider.GetRequiredService<ILoadingService>();
        logEntry = new LogEntry
        {
            ClassName = nameof(LoginVM),
            UserId = GlobalData.SessionData.User?.Id,
        };
    }

    [RelayCommand]
    public async Task ChangeActivity(object parameter)
    {
        try
        {
            var selectedStatusIndex = (int)parameter;

            if (IsEarlyBreakChange())
            {
                if (ShowBreakWarning())
                {
                    await UpdateStatus(selectedStatusIndex);
                }
                else
                {
                    RevertToPreviousStatus();
                }
            }
            else if (CurrentStatusIndex == (int)SharedStatus.Lunch)
            {
                await UpdateStatus(selectedStatusIndex);
                IsShuttingDown = true;
                await CloseSession();
            }
            else
            {
                await UpdateStatus(selectedStatusIndex);
            }
        }
        catch (Exception exception)
        {
            logEntry.LogMessage = exception.GetBaseException().Message;
            logEntry.MethodName = nameof(CloseSession);
            logService.Log(logEntry);
            var showMessge = $"({DateTime.Now}) Un error desconocido ocurrió al intentar cerrar la sesión.";
            var result = MessageBox.Show(showMessge, "LogTime - Error de conexión", MessageBoxButton.YesNo, MessageBoxImage.Question);
        }
    }

    [RelayCommand]
    public async Task CloseSession()
    {
        var shownErrorMessage = "";

        try
        {
            var action = IsShuttingDown ? "salir de la aplicación" : "reiniciar la aplicación";
            var promptMessage = $"¿Seguro que deseas cerrar la sesión y {action}?";

            logEntry.LogMessage = $"El usuario hizo click en cerrar la sesión y {action}.";
            logEntry.MethodName = nameof(CloseSession);
            logService.Log(logEntry);

            var result = MessageBox.Show(promptMessage, "LogTime - Confirmación", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                logEntry.LogMessage = $"El usuario decidió cancelar el cierre de sesión.";
                logService.Log(logEntry);
                IsShuttingDown = false;
                return;
            }

            await HandleCloseSession();

            if (!IsShuttingDown)
            {
                RestartApp();
            }
            else
            {
                Application.Current.Shutdown();
            }
        }
        catch (Exception exception)
        {
            if (exception is HttpRequestException)
            {
                shownErrorMessage = $"({DateTime.Now}) No se puedo conectar con el servidor.";
            }
            else
            {
                shownErrorMessage = $"({DateTime.Now}) Un error desconocido ocurrió al intentar cerrar la sesión.";
            }

            logEntry.LogMessage = exception.GetBaseException().Message;
            logService.Log(logEntry);

            MessageBox.Show(shownErrorMessage, "Error de conexión", MessageBoxButton.OK, MessageBoxImage.Error);
            RestartApp();
        }
        finally
        {
            loadingService.Close();
        }
    }

    private async Task HandleCloseSession()
    {
        loadingService.Show("Cerrando sesión, por favor espere...");
        logEntry.LogMessage = "Cerrando sesión.";
        logService.Log(logEntry);

        var updateSessionAliveDateResponse = await logTimeApiClient.UpdateSessionAliveDateAsync(GlobalData.SessionData.LogHistory.Id);

        if (HandleSessionAlreadyClosed(updateSessionAliveDateResponse))
            return;

        if (HandleErrorResponse(updateSessionAliveDateResponse))
            return;

        var userId = GlobalData.SessionData.User.Id;
        var sessionLogOutData = new SessionLogOutData
        {
            UserIds = userId,
            LoggedOutBy = CurrentStatusIndex == (int)SharedStatus.Lunch ? "Auto-logout: Lunch" : userId
        };

        var closeSessionResponse = await logTimeApiClient.CloseSessionAsync(sessionLogOutData);

        if (HandleSessionAlreadyClosed(closeSessionResponse))
            return;

        if (HandleErrorResponse(closeSessionResponse))
            return;

        logEntry.LogMessage = "Sesión cerrada.";
        logService.Log(logEntry);
    }

    private bool HandleSessionAlreadyClosed(BaseResponse response)
    {
        if (response.IsSessionAlreadyClose)
        {
            logEntry.LogMessage = "La sesión ya había sido cerrada por alguien más o por un servicio.";
            logService.Log(logEntry);
            MessageBox.Show(logEntry.LogMessage, "Sesión cerrada", MessageBoxButton.OK, MessageBoxImage.Information);
            RestartApp();
            return true;
        }
        return false;
    }

    private bool HandleErrorResponse(BaseResponse response)
    {
        if (response.HasError)
        {
            logEntry.LogMessage = response.Message;
            logEntry.MethodName = nameof(HandleErrorResponse);
            logService.Log(logEntry);
            string shownErrorMessage = $"({DateTime.Now}) Comunícate con el soporte técnico para recibir asistencia con este error.";
            var dialogResult = MessageBox.Show(
                $"{shownErrorMessage}{Environment.NewLine}{Environment.NewLine}" +
                "Puedes seleccionar (Yes) para intentar cerrar la sesión nuevamente o (No) para reiniciar la aplicación. " +
                "Tenga en cuenta que, al reiniciar, la sesión se asociará con la hora actual y será necesario iniciar sesión nuevamente para liberarla.",
                "LogTime - Error de sesión",
                MessageBoxButton.YesNo,
                MessageBoxImage.Error);

            if (dialogResult == MessageBoxResult.No)
            {
                return HandleRestartApplication(logEntry);
            }
            else
            {
                logEntry.LogMessage = "El usuario aceptó la opción de regresar para reintentar cerrar sesión.";
                logService.Log(logEntry);
                Application.Current.MainWindow.Activate();
                return true;
            }
        }
        return false;
    }

    private bool HandleRestartApplication(LogEntry logEntry)
    {
        logEntry.LogMessage = "Tras el error, el usuario rechazó reintentar y decidió reiniciar la aplicación.";
        logEntry.MethodName = nameof(HandleRestartApplication);
        logService.Log(logEntry);

        var restartDialogResult = MessageBox.Show(
            $"Has decidido reiniciar la aplicación. Esta acción dejará la sesión enganchada hasta que vuelvas a iniciar sesión o " +
            $"alguien más la cierre. Del modo que sea, la sesión se guardará con fecha de cierre: {ServerConnection}" +
            $"{Environment.NewLine}¿Deseas continuar?",
            "LogTime - Continuar reiniciando",
            MessageBoxButton.OKCancel,
            MessageBoxImage.Information);

        if (restartDialogResult == MessageBoxResult.Yes)
        {
            logEntry.LogMessage = "El usuario aceptó reiniciar la aplicación.";
            logService.Log(logEntry);
            RestartApp();
            return true;
        }
        else
        {
            logEntry.LogMessage = "El usuario rechazó reiniciar la aplicación.";
            logService.Log(logEntry);
            Application.Current.MainWindow.Activate();
            return true;
        }
    }

    public void RestartApp()
    {
        string? executablePath = Environment.ProcessPath;
        IsRestarting = true;
        IsShuttingDown = false;

        if (executablePath != null)
        {
            Process.Start(executablePath);
            Application.Current.Shutdown();
        }
    }

    private bool IsEarlyBreakChange()
    {
        return (_previousStatusId == (int)SharedStatus.Break && activityTimeSpan.Minutes < MinimumBreakDurationMinutes)
               && CurrentStatusIndex != (int)SharedStatus.Break;
    }

    private static bool ShowBreakWarning()
    {
        var result = MessageBox.Show(Resources.EARLY_BREAK_CHANGE, Resources.EARLY_BREAK_TITLE, MessageBoxButton.YesNo, MessageBoxImage.Warning);
        return result == MessageBoxResult.Yes;
    }

    private void ResetActivityTime() => activityTimeSpan = TimeSpan.Zero;

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
            ResetActivityTime();

            var statusChange = new StatusHistoryChange
            {
                Id = SessionData.ActiveLog.ActualStatusHistoryId,
                NewActivityId = activity.Id,
            };

            var statusHistoryChangeResponse = await logTimeApiClient.ChangeActivityAsync(statusChange);
            SessionData.ActiveLog.ActualStatusHistoryId = statusHistoryChangeResponse.Id;

            if (HandleSessionAlreadyClosed(statusHistoryChangeResponse))
                return;

            if (HandleErrorResponse(statusHistoryChangeResponse))
                return;

            ServerConnection = statusHistoryChangeResponse.StartTime.ToString("yyyy-MM-dd HH:mm:ss");
            loadingService.Close();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message, "Error de actividad", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RevertToPreviousStatus() => CurrentStatusIndex = _previousStatusId;

    private void ActivityTimerTick(object? sender, EventArgs e)
    {
        activityTimeSpan = activityTimeSpan.Add(TimeSpan.FromSeconds(1));
        ActivityTime = activityTimeSpan.ToString();
    }

    private void SessionTimerTick(object? sender, EventArgs e)
    {
        sessionTimeSpan = sessionTimeSpan.Add(TimeSpan.FromSeconds(1));
        SessionTime = sessionTimeSpan.ToString();
    }
}
