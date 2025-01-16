namespace LogTime.Services;
public class RetryService
{

    public static event EventHandler? OnCancelRetry;


    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int retryCount = 6,
        int retryDelay = 10,
        CancellationToken cancellationToken = default)
    {
        int attempt = 0;

        var loadingService = App.ServiceProvider?.GetRequiredService<ILoadingService>();

        do
        {
            try
            {
                if (attempt == 1)
                {
                    MainVM.HandleGeneralTimerTickOnRetry(true);
                }

                cancellationToken.ThrowIfCancellationRequested();
                var response = await operation();
                loadingService?.Close();
                MainVM.HandleGeneralTimerTickOnRetry(false);
                return response;
            }
            catch (Exception)
            {
                if (attempt >= retryCount)
                {
                    throw;
                }

                attempt++;
                loadingService?.Show($"Ha ocurrido un error al precesar la solicitud. \n Iniciando reintento... Intento: {attempt}/{retryCount}");

                await Task.Delay(TimeSpan.FromSeconds(retryDelay), cancellationToken);
            }

        } while (true);
    }

    public static void CancelRetry()
    {
        OnCancelRetry?.Invoke(null, EventArgs.Empty);
    }
}
