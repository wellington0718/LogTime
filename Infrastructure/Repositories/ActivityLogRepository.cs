using DataAccess;
using DataAccess.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class ActivityLogRepository
    {
        private readonly DataBaseAccess dataBaseAccess;

        public ActivityLogRepository(DataBaseAccess dataBaseAccess)
        {
            this.dataBaseAccess = dataBaseAccess;
        }

        public async Task<int> AddAsync(ActivityLog entity)
        {
            const string sql =
                @"INSERT INTO StatusHistory(LogId, StatusId, StatusStartTime, StatusEndTime)
                VALUES(@LogId, @StatusId, @StatusStartTime, @StatusEndTime);
                SELECT SCOPE_IDENTITY();";
            var parameters = new
            {
                entity.LogId,
                entity.StatusId,
                entity.StatusStartTime,
                entity.StatusEndTime
            };

            return await dataBaseAccess.ExecuteScalarAsync<int, dynamic>(sql, parameters);
        }

        public async Task<ActivityLog> GetAsync(int activityLogId)
        {
            const string sql =
                @"SELECT * 
                FROM StatusHistory
                WHERE Id = @activityLogId";
            var parameters = new { activityLogId };

            return await dataBaseAccess.LoadFirstOrDefaultAsync<ActivityLog, dynamic>(sql, parameters);
        }

        public async Task<IEnumerable<ActivityLog>> GetUnfinishedAsync(int sessionLogId)
        {
            const string sql =
                @"SELECT * 
                FROM StatusHistory
                WHERE LogId = @sessionLogId
                AND StatusEndTime IS NULL";
            var parameters = new { sessionLogId };

            return await dataBaseAccess.LoadDataAsync<ActivityLog, dynamic>(sql, parameters);
        }

        public async Task UpdateAsync(ActivityLog entity)
        {
            const string sql =
                @"UPDATE StatusHistory  
                SET 
                    LogId = @LogId,  
                    StatusId = @StatusId,  
                    StatusStartTime = @StatusStartTime,
                    StatusEndTime = @StatusEndTime
                WHERE 
                    Id = @Id;";
            var parameters = new
            {
                entity.LogId,
                entity.StatusId,
                entity.StatusStartTime,
                entity.StatusEndTime,
                entity.Id
            };

            await dataBaseAccess.SaveDataAsync(sql, parameters);
        }

        public async Task UpdateEndTimeAsync(IEnumerable<ActivityLog> entities)
        {
            var entitiesIds = string.Join(",", entities.Select(e => e.Id));
            var endTime = entities.FirstOrDefault().StatusEndTime;

            const string sql =
                @"UPDATE StatusHistory  
                SET                     
                    StatusEndTime = @endTime
                WHERE 
                    Id IN (SELECT arrValue FROM dbo.fnArray(@entitiesIds, ','))";
            var parameters = new
            {
                endTime,
                entitiesIds
            };

            await dataBaseAccess.SaveDataAsync(sql, parameters);
        }

    }
}
