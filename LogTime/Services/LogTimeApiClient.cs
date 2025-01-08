using Polly;
using Polly.Retry;

namespace LogTime.Services;

public class LogTimeApiClient : ILogTimeApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly RetryStrategyOptions<HttpResponseMessage> options;
    private readonly ResiliencePipeline<HttpResponseMessage> pipeline;
    private readonly int maxRetryAttempts = 6;

    public LogTimeApiClient(HttpClient httpClient, ILoadingService loadingService)
    {
        _httpClient = httpClient;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        options = new RetryStrategyOptions<HttpResponseMessage>
        {
            MaxRetryAttempts = maxRetryAttempts,
            Delay = TimeSpan.FromSeconds(10),
            ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                           .Handle<Exception>(),

            OnRetry = (args) =>
            {
                loadingService.Show($"Ha ocurrido un error al precesar la solicitud. \n Iniciando reintento... Intento: {args.AttemptNumber + 1}/{maxRetryAttempts}");
                return default;
            }
        };

        pipeline = new ResiliencePipelineBuilder<HttpResponseMessage>()
             .AddTimeout(TimeSpan.FromSeconds(60))
             .AddRetry(options).Build();
    }

    private async Task<T> SendAsync<T>(string endpoint, object body)
    {
        var content = JsonSerializer.Serialize(body, _jsonOptions);

        var response = await pipeline.ExecuteAsync(async (ctx) =>
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            return await _httpClient.SendAsync(request, ctx);

        });

        response.EnsureSuccessStatusCode();

        var responseDataString = await response.Content.ReadAsStringAsync();
        var responseDataObj = JsonSerializer.Deserialize<T>(responseDataString, _jsonOptions)
                               ?? throw new InvalidOperationException("Failed to deserialize response.");
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
