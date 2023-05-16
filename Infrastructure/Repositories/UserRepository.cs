using DataAccess;
using DataAccess.Models;
using DataAccess.SQL;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class UserRepository
    {
        private readonly DataBaseAccess dataBaseAccess;
        public UserRepository(DataBaseAccess dataBaseAccess)
        {
            this.dataBaseAccess = dataBaseAccess;
        }

        public async Task<User> GetInfo(string userId)
        {
            const string sql = nameof(StoredProcedureName.GetUserInfo);
            var parameters = new
            {
                id = userId
            };

            var user = await dataBaseAccess.LoadFirstOrDefaultAsync<User, dynamic>(sql, parameters, CommandType.StoredProcedure);

            user.ProjectGroup = await GetProjectGroup(userId);

            user.Project = user.ProjectGroup == null
                ? await GetProject(userId)
                : await GetProject(userId, user.ProjectGroup.Id);

            user.RoleId = await GetRoleId(userId);

            return user;
        }

        public async Task<int> GetRoleId(string userId)
        {
            const string sql = nameof(StoredProcedureName.GetUserPermision);
            var parameters = new
            {
                userId
            };

            return await dataBaseAccess.ExecuteScalarAsync<int, dynamic>(sql, parameters, CommandType.StoredProcedure);
        }

        public async Task<Project> GetProject(string userId, int? groupId = null)
        {
            const string sql = nameof(StoredProcedureName.GetUserProject);
            var parameters = new
            {
                userId
            };

            var project = await dataBaseAccess.LoadFirstOrDefaultAsync<Project, dynamic>(sql, parameters, CommandType.StoredProcedure);

            if (project == null
                || string.IsNullOrEmpty(project.Project_Ini))
            {
                project = new Project(await GetActivities("ALL"));
            }
            else
            {
                project.AvailableActivities = await GetActivities(project.Project_Ini, groupId);
            }

            return project;
        }

        public async Task<IEnumerable<Status>> GetActivities(string projectId, int? projectGroupId = null)
        {
            const string sql = nameof(StoredProcedureName.GetProjectActivities);
            var parameters = new
            {
                GroupId = projectGroupId,
                Project = projectId
            };

            return await dataBaseAccess.LoadDataAsync<Status, dynamic>(sql, parameters, CommandType.StoredProcedure);
        }

        public async Task<ProjectGroup> GetProjectGroup(string userId)
        {
            const string sql = nameof(StoredProcedureName.GetUserGroup);
            var parameters = new
            {
                userId
            };

            return await dataBaseAccess.LoadFirstOrDefaultAsync<ProjectGroup, dynamic>(sql, parameters, CommandType.StoredProcedure);
        }
    }
}
