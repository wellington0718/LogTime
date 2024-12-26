namespace LogTime.Api.Contracts;

public interface ILogTimeUnitOfWork
{
    ILogHistoryRepository LogHistoryRepository { get; }
    IStatusHistoryRepository StatusHistoryRepository { get; }
    IActiveLogRepository ActiveLogRepository { get; }
    IUserRepository UserRepository { get; }
    Task<bool> SaveChangesAsync();
    Task CommitAsync();
}
