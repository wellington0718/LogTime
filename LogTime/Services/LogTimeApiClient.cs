namespace LogTime.Services;

public class LogTimeApiClient : ILogTimeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public LogTimeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://localhost:5208/api/");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task<T> SendAsync<T>(string endpoint, object? body = null)
    {
        Console.WriteLine("SendAsync: Starting request...");

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (body != null)
        {
            var content = JsonSerializer.Serialize(body, _jsonOptions);
            request.Content = new StringContent(content, Encoding.UTF8, "application/json");
        }

        Console.WriteLine("SendAsync: Sending request...");
        using var response = await _httpClient.SendAsync(request);
        Console.WriteLine("SendAsync: Request completed.");

        response.EnsureSuccessStatusCode();

        Console.WriteLine("SendAsync: Reading response...");
        var responseDataString = await response.Content.ReadAsStringAsync();

        Console.WriteLine("SendAsync: Deserializing response...");
        var responseDataObj = JsonSerializer.Deserialize<T>(responseDataString, _jsonOptions) ?? throw new InvalidOperationException("Failed to deserialize response.");

        Console.WriteLine("SendAsync: Retur");
        return responseDataObj;
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
