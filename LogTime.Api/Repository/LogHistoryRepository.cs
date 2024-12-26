using LogTime.Api.Contracts;

namespace LogTime.Api.Repository;

public class LogHistoryRepository(LogTimeDataContext dataContext) : GenericRepository<LogHistory>(dataContext), ILogHistoryRepository
{
}
