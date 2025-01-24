﻿namespace LogTime.Services;

public class ExceptionService
{
    private static readonly List<string> netWorkErrors = ["network-related", "No such host is known", "No connection could be made"];

    public static void Handle(string message)
    {
        if (netWorkErrors.Any(message.Contains))
        {
            DialogBox.Show(Resource.RETRY_FAIL_CONEXION_MESSAGE, "LogTime - Error de conexión", alertType: AlertType.Error);
        }
        else if(message.Contains("A task was canceled."))
        {
          DialogBox.Show("La operación ha sido cancelada por el usuario.", "Operación cancelada", alertType: AlertType.Information);
        }
        else
        {
            DialogBox.Show(Resource.UNKNOWN_ERROR, Resource.UNKNOWN_ERROR_TITLE, alertType: AlertType.Error);
        }

      // App.Restart();
    }
}
