namespace LogTime.Contracts;

public interface ILogTimeApiClient
{
    Task<BaseResponse> ValidateCredentialsAsync(ClientData clientData);
    Task<SessionData> OpenSessionAsync(ClientData clientData);
    Task<BaseResponse> CloseSessionAsync(SessionLogOutData sessionLogOutData);
    Task<SessionAliveDate> UpdateSessionAliveDateAsync(int logHistoryId);
    Task<FetchSessionData> FetchSessionAsync(string userId);
    Task<StatusHistoryChange> ChangeActivityAsync(StatusHistoryChange statusChange);
    Task<BaseResponse> IsUserNotAllowedToLoginAsync(string userId);

}
