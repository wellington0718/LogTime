namespace LogTime.Api.Repository;

public class ActiveLogRepository(LogTimeDataContext dataContext, IUserRepository userRepository, IStatusHistoryRepository statusHistoryRepository,
    ILogHistoryRepository logHistoryRepository)
    : GenericRepository<ActiveLog>(dataContext), IActiveLogRepository
{
    public async Task<SessionData> CreateNewSessionAsync(ClientData clientData)
    {
        var startDate = DateTime.Now;

        var logHistory = new LogHistory
        {
            LoginDate = startDate,
            LastTimeConnectionAlive = startDate,
            IdUser = clientData.User,
            Hostname = clientData.HostName,
            ClientVersion = clientData.ClientVersion
        };

        var createdLogHistory = await logHistoryRepository.CreateAsync(logHistory);

        await SaveChangesAsync();

        var statusHistory = new StatusHistory
        {
            StatusStartTime = createdLogHistory.LoginDate,
            LogId = createdLogHistory.Id,
            StatusId = 1
        };

        var createdStatusHistory = await statusHistoryRepository.CreateAsync(statusHistory);

        await SaveChangesAsync();

        var activeLog = new ActiveLog
        {
            ActualLogHistoryId = createdStatusHistory.LogId,
            StatusId = 1,
            UserId = clientData.User,
            ActualStatusHistoryId = createdStatusHistory.Id,
        };

        var user = await userRepository.GetInfo(clientData.User);
        await CreateAsync(activeLog);

        await SaveChangesAsync();

        var newSessionData = new SessionData
        {
            User = user,
            LogHistory = createdLogHistory,
            ActiveLog = activeLog,
        };

        return newSessionData;
    }

    public async Task<bool> CloseActiveSessionsAsync(SessionLogOutData sessionLogOutData)
    {
        var idList = string.IsNullOrEmpty(sessionLogOutData.UserIds)
            ? []
            : sessionLogOutData.UserIds.Split(',').ToList();

        IEnumerable<LogHistory> logHistories = idList.Count != 0
            ? await logHistoryRepository.GetListAsync(log => log.LogoutDate == null && idList.Contains(log.IdUser))
            : await logHistoryRepository.GetListAsync(log => log.LogoutDate == null && log.Id == sessionLogOutData.Id);

        if (!logHistories.Any())
            return true;

        var currentDate = DateTime.Now;

        foreach (var logHistory in logHistories)
        {
            logHistory.LogoutDate = DetermineLogoutDate(currentDate, logHistory.LastTimeConnectionAlive);
            logHistory.LogedOutBy = string.IsNullOrEmpty(sessionLogOutData.LoggedOutBy)
                ? "New session"
                : sessionLogOutData.LoggedOutBy;

            var activityLogs = await statusHistoryRepository.GetListAsync(activityLog =>
                activityLog.LogId == logHistory.Id && activityLog.StatusEndTime == null);

            foreach (var activityLog in activityLogs)
            {
                activityLog.StatusEndTime = logHistory.LogoutDate;
            }

            statusHistoryRepository.UpdateRange(activityLogs);
        }

        logHistoryRepository.UpdateRange(logHistories);

        var activeLogs = await GetListAsync(activeLog => idList.Contains(activeLog.UserId));
        DeleteAsync(activeLogs);

        return await SaveChangesAsync();
    }

    private static DateTime DetermineLogoutDate(DateTime currentDate, DateTime? lastTimeConnectionAlive)
    {
        if (lastTimeConnectionAlive.HasValue && (currentDate - lastTimeConnectionAlive.Value).TotalMinutes is > 2 or < 0)
        {
            return lastTimeConnectionAlive.Value;
        }

        return currentDate;
    }
}
