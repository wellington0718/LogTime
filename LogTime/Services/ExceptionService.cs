namespace LogTime.Services;

public class ExceptionService
{
    private static readonly List<string> errors = ["network-related", "No such host is known", "No connection could be made"];

    public static void Handle(string message)
    {
        if (errors.Any(message.Contains))
        {
            DialogBox.Show(Resource.RETRY_FAIL_CONEXION_MESSAGE, "LogTime - Error de conexión", alertType: AlertType.Error);
        }
        else
        {
            DialogBox.Show(Resource.UNKNOWN_ERROR, Resource.UNKNOWN_ERROR_TITLE, alertType: AlertType.Error);
        }

      // App.Restart();
    }
}
