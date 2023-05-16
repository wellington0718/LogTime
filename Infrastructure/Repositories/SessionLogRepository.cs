using DataAccess;
using DataAccess.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class SessionLogRepository
    {
        private readonly DataBaseAccess dataBaseAccess;

        public SessionLogRepository(DataBaseAccess dataBaseAccess)
        {
            this.dataBaseAccess = dataBaseAccess;
        }

        public async Task<SessionLog> GetAsync(int id)
        {
            const string sql =
                @"SELECT *
                FROM LogHistory
                WHERE Id = @Id";
            var parameters = new { Id = id };

            return await dataBaseAccess.LoadFirstOrDefaultAsync<SessionLog, dynamic>(sql, parameters);
        }

        public async Task<int> AddAsync(SessionLog entity)
        {
            const string sql =
                @"INSERT INTO LogHistory(IdUser, Hostname, LogedOutBy, LastTimeConnectionAlive, LoginDate)
                VALUES(@IdUser, @Hostname, @LogedOutBy, @LastTimeConnectionAlive, @LoginDate);
                SELECT SCOPE_IDENTITY();";
            var parameters = new
            {
                entity.IdUser,
                entity.Hostname,
                entity.LogedOutBy,
                entity.LastTimeConnectionAlive,
                entity.LoginDate
            };

            return await dataBaseAccess.ExecuteScalarAsync<int, dynamic>(sql, parameters);
        }

        public async Task UpdateAsync(SessionLog entity)
        {
            const string sql =
                @"UPDATE LogHistory  
                SET 
                    IdUser = @IdUser,  
                    Hostname = @Hostname,  
                    LogedOutBy = @LogedOutBy,
                    LastTimeConnectionAlive = @LastTimeConnectionAlive,
                    LoginDate = @LoginDate,
                    LogoutDate = @LogoutDate
                WHERE 
                    Id = @Id;";
            var parameters = new
            {
                entity.IdUser,
                entity.Hostname,
                entity.LogedOutBy,
                entity.LastTimeConnectionAlive,
                entity.LoginDate,
                entity.LogoutDate,
                entity.Id
            };

            await dataBaseAccess.SaveDataAsync(sql, parameters);
        }

        public async Task UpdateLogoutDataAsync(IEnumerable<SessionLog> entities)
        {
            var entitiesIds = string.Join(",", entities.Select(e => e.Id));
            var logoutDate = entities.FirstOrDefault().LogoutDate;
            var loggedOutBy = entities.FirstOrDefault().LogedOutBy;

            const string sql =
                @"UPDATE LogHistory  
                SET
                    LogedOutBy = @loggedOutBy,                   
                    LogoutDate = @logoutDate
                WHERE 
                    Id IN (SELECT arrValue FROM dbo.fnArray(@entitiesIds, ','))";
            var parameters = new
            {
                logoutDate,
                loggedOutBy,
                entitiesIds
            };

            await dataBaseAccess.SaveDataAsync(sql, parameters);
        }

        public async Task<IEnumerable<SessionLog>> GetActiveByUserIdAsync(string userId)
        {
            const string sql =
                @"SELECT *
                FROM LogHistory
                WHERE IdUser = @IdUser
                AND LogoutDate IS NULL
                ORDER BY LoginDate DESC";
            var parameters = new { IdUser = userId };

            return await dataBaseAccess.LoadDataAsync<SessionLog, dynamic>(sql, parameters);
        }
        public async Task<IEnumerable<SessionLog>> GetUsersActiveLogIdAsync(string userIds)
        {
            const string sql =
                @"	SELECT
		                  LogHistory.Id Id
                    FROM  ActiveLog
	                    INNER JOIN LogHistory
		                    ON LogHistory.Id = ActiveLog.ActualLogHistoryId
	
	                    INNER JOIN	SynergiesSystem.dbo.Employees Employee
		                    ON Employee.UserId = ActiveLog.UserId
	
                    WHERE	(ActiveLog.UserId IN (SELECT arrValue FROM dbo.fnArray(@userIds, ',')))";
            var parameters = new { userIds };

            return await dataBaseAccess.LoadDataAsync<SessionLog, dynamic>(sql, parameters);
        }

        public async Task<IEnumerable<SessionLog>> GetActiveByUsersAsync(string usersIds)
        {
            const string sql =
                @"SELECT *
                FROM LogHistory
                WHERE LogoutDate IS NULL
                AND IdUser IN (SELECT arrValue FROM dbo.fnArray(@Ids, ','))";
            var parameters = new { Ids = usersIds };

            return await dataBaseAccess.LoadDataAsync<SessionLog, dynamic>(sql, parameters);
        }

        //public int UpdateLastTimeConnectionAlive(int logHistoryId)
        //{
        //    const string storedProcedure = nameof(StoredProcedureName.UpdateLastTimeConnectionAlive);

        //    var parameters = new { LastTimeConnectionAlive = DateTime.Now, logHistoryId };
        //    return Task.Run(() =>
        //         dataAccess.SaveDataAsync(storedProcedure, parameters, CommandType.StoredProcedure)).Result;
        //}



        //public async Task<int> CloseSession(string userId)
        //{
        //    var activeSessionActivities = await GetActiveSessionActivities(userId);

        //    if (!activeSessionActivities.Any()) return 0;

        //    foreach (var activeSessionActivity in activeSessionActivities)
        //    {
        //        var historySession = new HistorySessionModel
        //        {
        //            IdUser = userId,
        //            LoginDate = activeSessionActivity.LoginDate,
        //            Hostname = activeSessionActivity.Hostname,
        //            LogoutDate = activeSessionActivity.LastTimeConnectionAlive,
        //            LastTimeConnectionAlive = activeSessionActivity.LastTimeConnectionAlive,
        //            LogedOutBy = $"{userId}-Application Exit"
        //        };

        //        var historyActivity = new HistoryActivityModel
        //        {
        //            Id = activeSessionActivity.StatusId,
        //            SessionHistoryId = activeSessionActivity.LogId,
        //            StatusId = activeSessionActivity.StatusId,
        //            ActivityStartDateTime = activeSessionActivity.StatusStartTime,
        //            ActivityEndDateTime = activeSessionActivity.LastTimeConnectionAlive
        //        };

        //        SaveSessionHistory(historySession);
        //        SaveSessionStatusHistory(historyActivity);
        //    }

        //    return CloseUserActiveSessions(userId);
        //}

        //private int SaveSessionHistory(HistorySessionModel activeSessionModel)
        //{
        //    var sessionHistory = new
        //    {
        //        Id = activeSessionModel.LogId,
        //        activeSessionModel.IdUser,
        //        activeSessionModel.LoginDate,
        //        activeSessionModel.Hostname,
        //        activeSessionModel.LastTimeConnectionAlive,
        //        activeSessionModel.LogedOutBy,
        //        LogoutDate = activeSessionModel.LastTimeConnectionAlive,
        //    };

        //    const string storedProcedure = nameof(StoredProcedureName.SaveSessionHistory);
        //    var actualLogHistoryId = Task.Run(() => dataAccess.SaveDataAsync(storedProcedure, sessionHistory, CommandType.StoredProcedure)).Result;

        //    return actualLogHistoryId;
        //}

        //private int SaveSessionStatusHistory(HistoryActivityModel historyActivity)
        //{
        //    var sessionStatusHistory = new
        //    {
        //        historyActivity.Id,
        //        historyActivity.SessionHistoryId,
        //        StatusId = historyActivity.StatusId,
        //        StatusStartTime = historyActivity.ActivityStartDateTime,
        //        StatusEndTime = historyActivity.ActivityEndDateTime
        //    };

        //    const string storedProcedure = nameof(StoredProcedureName.SaveSessionStatusHistory);
        //    return Task.Run(() =>
        //         dataAccess.SaveDataAsync(storedProcedure, sessionStatusHistory, CommandType.StoredProcedure)).Result;
        //}

        //private int CloseUserActiveSessions(string userId)
        //{
        //    const string storedProcedure = nameof(StoredProcedureName.DeleteUserActiveSessions);
        //    return Task.Run(() =>
        //         dataAccess.SaveDataAsync(storedProcedure, new { userId }, CommandType.StoredProcedure)).Result;

        //}

        //private async Task<List<HistorySessionModel>> GetActiveSessionActivities(string userId)
        //{
        //    const string storedProcedure = nameof(StoredProcedureName.GetUserActiveSessionState);

        //    return await dataAccess.LoadDataAsync<HistorySessionModel, dynamic>(storedProcedure, new { userId }, CommandType.StoredProcedure);
        //}
    }
}