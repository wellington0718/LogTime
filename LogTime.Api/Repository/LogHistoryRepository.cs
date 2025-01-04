using Domain.Models;
using LogTime.Api.UnitOfWorks;

namespace LogTime.Api.Repository;

public class LogHistoryRepository(LogTimeDataContext dataContext) : GenericRepository<LogHistory>(dataContext), ILogHistoryRepository
{
  
}
