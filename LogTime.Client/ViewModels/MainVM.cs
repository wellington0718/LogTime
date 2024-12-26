using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Domain.Models;
using LogTime.Client.Contracts;
using LogTime.Client.Properties;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Windows;
using System.Windows.Threading;

namespace LogTime.Client.ViewModels;

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
    const int MinimumBreakDurationMinutes = 2;

    public SessionData SessionData { get; }

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
    }

    [RelayCommand]
    public void ChangeActivity(object parameter)
    {
        try
        {
            var selectedStatusIndex = (int)parameter;

            if (IsEarlyBreakChange())
            {
                if (ShowBreakWarning())
                {
                    UpdateStatus(selectedStatusIndex);
                }
                else
                {
                    RevertToPreviousStatus();
                }
            }
            else if (CurrentStatusIndex == (int)SharedStatus.Lunch)
            {
                UpdateStatus(selectedStatusIndex);
            }
            else
            {
                UpdateStatus(selectedStatusIndex);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("An error occurred while changing the activity.", ex);
        }
    }

    [RelayCommand]
    public async Task CloseSession(bool isShuttingDown)
    {
        var shownErrorMessage = "";

        var logEntry = new LogEntry
        {
            ClassName = nameof(LoginVM),
            MethodName = nameof(CloseSession),
            LogMessage = "El usuario hizo clic en cerrar sesión.",
            UserId = GlobalData.SessionData.User?.Id,
        };

        try
        {
            var result = MessageBox.Show("¿Seguro que deseas cerrar la sesión?", "LogTime - Cerrar sesión", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                logEntry.LogMessage = "El usuario decidio abortar el cierre de sesión.";
                logService.Log(logEntry);
                return;
            }

            await HandleCloseSession(logEntry);

            if (!isShuttingDown)
                RestartApp();
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

    private async Task HandleCloseSession(LogEntry logEntry)
    {
        loadingService.Show("Cerrando sesión, por favor espere...");
        logEntry.LogMessage = "Cerrando sesión.";
        logService.Log(logEntry);

        var updateSessionAliveDateResponse = await logTimeApiClient.UpdateSessionAliveDateAsync(GlobalData.SessionData.LogHistory.Id);

        if (HandleSessionAlreadyClosed(updateSessionAliveDateResponse, logEntry))
            return;

        if (HandleErrorResponse(updateSessionAliveDateResponse, logEntry))
            return;

        var userId = GlobalData.SessionData.User.Id;
        var closeSessionResponse = await logTimeApiClient.CloseSessionAsync(new SessionLogOutData { UserIds = userId, LoggedOutBy = userId });

        if (HandleSessionAlreadyClosed(closeSessionResponse, logEntry))
            return;

        if (HandleErrorResponse(closeSessionResponse, logEntry))
            return;

        logEntry.LogMessage = "Sesión cerrada.";
        logService.Log(logEntry);
    }

    private bool HandleSessionAlreadyClosed(BaseResponse response, LogEntry logEntry)
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

    private bool HandleErrorResponse(BaseResponse response, LogEntry logEntry)
    {
        if (response.HasError)
        {
            logEntry.LogMessage = response.Message;
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


    private static void RestartApp()
    {
        string? executablePath = Environment.ProcessPath;

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

    private void UpdateStatus(int newStatusIndex)
    {
        ResetActivityTime();
        CurrentStatusIndex = newStatusIndex;
        _previousStatusId = CurrentStatusIndex;
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
