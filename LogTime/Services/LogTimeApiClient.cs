using System.Net.Sockets;

namespace LogTime.Services;

public class LogTimeApiClient : ILogTimeApiClient
{
    private readonly HttpClient _httpClient;
    private static IConfiguration? _configuration;
    private readonly JsonSerializerOptions _jsonOptions;

    public LogTimeApiClient(HttpClient httpClient, ILoadingService loadingService, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        _httpClient.BaseAddress = CanConnectToServer(isLocalhost:true);
    }

    private static Uri? CanConnectToServer(bool isLocalhost)
    {
        var hosts = _configuration?.GetSection("Host").Value?.Split(',');

        if (!isLocalhost && hosts?.Length > 0)
        {
            foreach (var host in hosts)
            {
                try
                {
                    using var tcpClient = new TcpClient();
                    tcpClient.ConnectAsync(host, 56848).Wait(5000);

                    if (tcpClient.Connected)
                    {
                        return new($"http://{host}:56848/logtime-3.0-api/api/");
                    }
                }
                catch (Exception)
                {
                }
            }

            return null;
        }
        else
        {
            return new("http://localhost:5208/api/");
        }

    }

    private async Task<T> SendAsync<T>(string endpoint, object body) where T : new()
    {
        var content = JsonSerializer.Serialize(body, _jsonOptions);
        var sourceToken = new CancellationTokenSource();

        var responseData = await RetryService.ExecuteWithRetryAsync(async () =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, sourceToken.Token);
            var responseDataString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(responseDataString);
            }

            var responseData = JsonSerializer.Deserialize<T>(responseDataString, _jsonOptions) ?? new();

            return responseData;

        }, retryCount: 2, retryDelay: 5, cancellationToken: sourceToken.Token);

        return responseData;
    }

    public async Task<BaseResponse> ValidateCredentialsAsync(ClientData clientData)
    {
        return await SendAsync<BaseResponse>("Session/ValidateCredentials", clientData);
    }

    public async Task<SessionData> OpenSessionAsync(ClientData clientData)
    {
        return await SendAsync<SessionData>("Session/Open", clientData);
    }

    public async Task<BaseResponse> CloseSessionAsync(SessionLogOutData sessionLogOutData)
    {
        return await SendAsync<BaseResponse>("Session/Close", sessionLogOutData);
    }

    public async Task<SessionAliveDate> UpdateSessionAliveDateAsync(int logHistoryId)
    {
        return await SendAsync<SessionAliveDate>("Session/Update", logHistoryId);
    }

    public async Task<FetchSessionData> FetchSessionAsync(string userId)
    {
        return await SendAsync<FetchSessionData>("Session/Fetch", userId);
    }

    public async Task<StatusHistoryChange> ChangeActivityAsync(StatusHistoryChange statusChange)
    {
        return await SendAsync<StatusHistoryChange>("Status/Change", statusChange);
    }

    public async Task<BaseResponse> IsUserNotAllowedToLoginAsync(string userId)
    {
        return await SendAsync<BaseResponse>("Session/IsUserAllowedToLogin", userId);
    }
}
