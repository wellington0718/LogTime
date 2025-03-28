﻿namespace LogTime.Api.UnitOfWorks;

public class LogTimeUnitOfWork(LogTimeDataContext dataContext, IServiceProvider provider) : GenericUnitOfWork(dataContext), ILogTimeUnitOfWork
{
    public ILogHistoryRepository LogHistoryRepository => provider.GetRequiredService<ILogHistoryRepository>();

    public IStatusHistoryRepository StatusHistoryRepository => provider.GetRequiredService<IStatusHistoryRepository>();

    public IActiveLogRepository ActiveLogRepository => provider.GetRequiredService<IActiveLogRepository>();

    public IUserRepository UserRepository => provider.GetRequiredService<IUserRepository>();

    public async Task<BaseResponse> CloseActiveSessionsAsync(string loggedOutBy, string userIds)
    {
        var userIdsList = string.IsNullOrEmpty(userIds) ? [] : userIds.Split(',').ToList();
        List<LogHistory> logHistories;

        var logHistoryIdsByActiveLogs = ActiveLogRepository.GetQueryable()
                                       .Where(activeLog => userIdsList.Contains(activeLog.UserId))
                                       .Select(activeLog => activeLog.ActualLogHistoryId);

        if(!logHistoryIdsByActiveLogs.Any())
        {
            logHistories = [.. LogHistoryRepository.GetQueryable()
                                   .Where(logHistory => userIdsList.Contains(logHistory.IdUser) && logHistory.LogoutDate == null)
                                   .Include(log => log.ActiveLogs)
                                   .Include(log => log.StatusHistories)];
        }
        else
        {
            logHistories = [.. LogHistoryRepository.GetQueryable()
                                   .Where(logHistory => logHistoryIdsByActiveLogs.Contains(logHistory.Id))
                                   .Include(log => log.ActiveLogs)
                                   .Include(log => log.StatusHistories)];
        }

        if (logHistories.Any(l => l.LogoutDate.HasValue))
        {
            logHistories.ForEach(logHistory =>
            {
                if (logHistory.LogoutDate.HasValue)
                    ActiveLogRepository.DeleteAsync(logHistory.ActiveLogs);
            });

            await SaveChangesAsync();
          
            return new BaseResponse
            {
                Code = StatusCodes.Status200OK,
                Title = nameof(StatusMessage.Ok),
                IsSessionAlreadyClose = true
            };
        }

        var currentDate = DateTime.Now;

        logHistories.ForEach(logHistory =>
        {
            var logoutDate = DetermineLogoutDate(currentDate, logHistory.LastTimeConnectionAlive);
            logHistory.LogoutDate = logoutDate;
            logHistory.LastTimeConnectionAlive = logoutDate;
            logHistory.LogedOutBy = string.IsNullOrEmpty(loggedOutBy) ? "New session" : loggedOutBy;

            var statusHistories = logHistory.StatusHistories.Where(statusHistory => statusHistory.StatusEndTime == null);

            foreach (var statusHistory in statusHistories)
            {
                statusHistory.StatusEndTime = logoutDate;
            }

            StatusHistoryRepository.UpdateRange(logHistory.StatusHistories);
            ActiveLogRepository.DeleteAsync(logHistory.ActiveLogs);
        });

        LogHistoryRepository.UpdateRange(logHistories);
        await SaveChangesAsync();

        return new BaseResponse
        {
            Code = StatusCodes.Status200OK,
            Message = nameof(StatusMessage.Success),
            Title = nameof(StatusMessage.Ok),
        };
    }

    public async Task<BaseResponse> UpdateLogHistoyAsync(int logHistoryId)
    {
        var logHistory = await LogHistoryRepository.FindAsync(logHistoryId);

        if (logHistory.LogoutDate.HasValue)
        {
            ActiveLogRepository.DeleteAsync(logHistory.ActiveLogs);
            await SaveChangesAsync();

            return new BaseResponse
            {
                Code = StatusCodes.Status200OK,
                Title = nameof(StatusMessage.Ok),
                IsSessionAlreadyClose = true,
                Message = nameof(StatusMessage.Success)
            };
        }

        logHistory.LastTimeConnectionAlive = DateTime.Now;
        LogHistoryRepository.Update(logHistory);
        await SaveChangesAsync();

        return new SessionAliveDate { LastDate = logHistory.LastTimeConnectionAlive };
    }

    public async Task<BaseResponse> FetchActiveSessionAsync(string userId)
    {
        var foundLogHistory = await LogHistoryRepository.FindAsync(logHistory => logHistory.IdUser.Equals(userId) && logHistory.LogoutDate == null);

        var fetchSessionData = foundLogHistory == null
            ? new FetchSessionData { IsAlreadyOpened = false, IsSessionAlreadyClose = true }
            : new FetchSessionData { IsAlreadyOpened = true, CurrentRemoteHost = foundLogHistory.Hostname };

        return fetchSessionData;
    }

    public async Task<BaseResponse> ChangeStatusAsync(int newActivityId, int currentStatusHistoryId)
    {
        var currentStatusHistory = await StatusHistoryRepository.FindAsync(currentStatusHistoryId);
        var currentActiveLog = await ActiveLogRepository.FindAsync(activeLog => activeLog.ActualStatusHistoryId == currentStatusHistory.Id);
       
        if (currentActiveLog == null)
        {
            return new BaseResponse
            {
                Code = StatusCodes.Status200OK,
                Title = nameof(StatusMessage.Ok),
                IsSessionAlreadyClose = true
            };
        }

        var logHistory = await LogHistoryRepository.FindAsync(currentActiveLog.ActualLogHistoryId);

        if (logHistory.LogoutDate.HasValue)
        {
            await ActiveLogRepository.DeleteAsync(currentActiveLog);
            await SaveChangesAsync();

            return new BaseResponse
            {
                Code = StatusCodes.Status200OK,
                Title = nameof(StatusMessage.Ok),
                IsSessionAlreadyClose = true
            };
        }

        var currentDateTime = DateTime.Now;

        logHistory.LastTimeConnectionAlive = currentDateTime;
        currentStatusHistory.StatusEndTime = currentDateTime;
        LogHistoryRepository.Update(logHistory);
        StatusHistoryRepository.Update(currentStatusHistory);
        await SaveChangesAsync();

        var newStatusHistory = new StatusHistory
        {
            LogId = currentStatusHistory.LogId,
            StatusId = newActivityId,
            StatusStartTime = currentDateTime
        };

        var createdStatusHistory = await StatusHistoryRepository.CreateAsync(newStatusHistory);
        await SaveChangesAsync();

        currentActiveLog.ActualStatusHistoryId = createdStatusHistory.Id;
        ActiveLogRepository.Update(currentActiveLog);

        await SaveChangesAsync();

        var newStatusHistoryChange = new StatusHistoryChange
        {
            Id = createdStatusHistory.Id,
            StartTime = currentDateTime,
            HasError = false,
            IsSessionAlreadyClose = false,
            Code = StatusCodes.Status200OK,
            Title = nameof(StatusMessage.Ok),
        };

        return newStatusHistoryChange;
    }

    public async Task<BaseResponse> ValidateCredentialsAsync(string userId, string password)
    {
        var userExists = await UserRepository.ValidateCredentialsAsync(userId, password);

        if (!userExists)
        {
            return new BaseResponse
            {
                Code = StatusCodes.Status401Unauthorized,
                Title = nameof(StatusMessage.Unauthorized),
                Message = nameof(StatusMessage.Success),
            };
        }

        var isNotAllowed = await UserRepository.IsUserNotAllowedAsync(userId);

        if (isNotAllowed)
        {
            return new BaseResponse
            {
                Code = StatusCodes.Status401Unauthorized,
                Title = nameof(StatusMessage.NotAllowed),
                Message = nameof(StatusMessage.Success)
            };
        }

        return new BaseResponse
        {
            Code = StatusCodes.Status200OK,
            Title = nameof(StatusMessage.Ok),
            Message = nameof(StatusMessage.Success)
        };
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
