namespace LogTime.Api.Contracts;

public interface ILogTimeUnitOfWork
{
    ILogHistoryRepository LogHistoryRepository { get; }
    IStatusHistoryRepository StatusHistoryRepository { get; }
    IActiveLogRepository ActiveLogRepository { get; }
    IUserRepository UserRepository { get; }
    Task<BaseResponse> CloseActiveSessionsAsync(string loggedOutBy, string userIds);
    Task<BaseResponse> UpdateLogHistoyAsync(int logHistoryId);
    Task<BaseResponse> FetchActiveSessionAsync(string userId);
    Task<BaseResponse> ValidateCredentialsAsync(string userId, string password);
    Task<BaseResponse> ChangeStatusAsync(int newStatusId);
    Task<bool> SaveChangesAsync();
    Task CommitAsync();
}
