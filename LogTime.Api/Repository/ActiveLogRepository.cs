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

    public async Task<bool> CloseActiveSessionsAsync(string loggedOutBy, string userId)
    {
        var idList = userId.Split(',').ToList();
        var logHistories = await logHistoryRepository.GetListAsync(log => log.LogoutDate == null && idList.Contains(log.IdUser));

        if (!logHistories.Any())
            return await Task.FromResult(true);

        var currentDate = DateTime.Now;

        var logOutDate = currentDate;

        foreach (var logHistory in logHistories)
        {
            if (logHistory.LastTimeConnectionAlive.HasValue
                && RelevantTimeDifference(currentDate, logHistory.LastTimeConnectionAlive.Value))
            {
                logOutDate = logHistory.LastTimeConnectionAlive.Value;
            }

            logHistory.LogoutDate = logOutDate;
            logHistory.LogedOutBy = string.IsNullOrEmpty(loggedOutBy) ? "New session" : loggedOutBy;

            var activityLogs = await statusHistoryRepository.GetListAsync(a => a.LogId == logHistory.Id && a.StatusEndTime == null);

            foreach (var activityLog in activityLogs)
            {
                activityLog.StatusEndTime = logHistory.LogoutDate;
            }

            statusHistoryRepository.UpdateRange(activityLogs);
        }

        logHistoryRepository.UpdateRange(logHistories);

        var activeLogs = await GetListAsync(activeLog => idList.Contains(activeLog.UserId));
        DeleteAsync(activeLogs);

        var result = await SaveChangesAsync();

        return result;
    }

    private static bool RelevantTimeDifference(DateTime currentTime, DateTime lastConnectionAliveTime)
    {
        return (currentTime - lastConnectionAliveTime).TotalMinutes is > 2 or < 0;
    }
}
