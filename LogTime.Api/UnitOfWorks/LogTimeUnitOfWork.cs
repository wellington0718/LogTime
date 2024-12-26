using LogTime.Api.Contracts;

namespace LogTime.Api.UnitOfWorks;

public class LogTimeUnitOfWork(LogTimeDataContext dataContext, IServiceProvider provider) : GenericUnitOfWork(dataContext), ILogTimeUnitOfWork
{
    public ILogHistoryRepository LogHistoryRepository => provider.GetRequiredService<ILogHistoryRepository>();

    public IStatusHistoryRepository StatusHistoryRepository => provider.GetRequiredService<IStatusHistoryRepository>();

    public IActiveLogRepository ActiveLogRepository => provider.GetRequiredService<IActiveLogRepository>();

    public IUserRepository UserRepository => provider.GetRequiredService<IUserRepository>();
}
