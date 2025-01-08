namespace LogTime.Services;

public class ExceptionService
{
    public static void Handle(string message)
    {
        if (message.Contains("Network-related") || message.Contains("No such host is known"))
        {
            DialogBox.Show(Resource.RETRY_FAIL_CONEXION_MESSAGE, "LogTime - Error de conexión", alertType: AlertType.Error);
        }
        else
        {
            DialogBox.Show(Resource.UNKNOWN_ERROR, Resource.UNKNOWN_ERROR_TITLE, alertType: AlertType.Error);
        }
    }
}
