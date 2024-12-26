using LogTime.Api.Contracts;

namespace LogTime.Api.Repository;

public class StatusHistoryRepository(LogTimeDataContext context) : GenericRepository<StatusHistory>(context), IStatusHistoryRepository
{
}
