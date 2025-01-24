using MySql.Data.MySqlClient;
using System.Data;

namespace LogTime.Api.Repository;

public class UserRepository(LogTimeDataContext dataContext) : IUserRepository
{
    private readonly LogTimeDataContext dataContext = dataContext;

    public async Task<User> GetInfo(string userId)
    {
        var user = dataContext.Database
                          .SqlQueryRaw<User>("EXEC [dbo].[GetUserInfo] @id = {0}", userId)
                          .AsEnumerable().FirstOrDefault();

        user.ProjectGroup = await GetProjectGroup(userId);

        user.Project = user.ProjectGroup == null
            ? await GetProject(userId)
            : await GetProject(userId, user.ProjectGroup.Id);

        user.RoleId = await GetRoleId(userId);

        return await Task.FromResult(user);
    }

    public async Task<int> GetRoleId(string userId)
    {
        var userRoleId = dataContext.Database
                          .SqlQueryRaw<int>("EXEC [dbo].[GetUserPermision] @userId = {0}", userId)
                          .AsEnumerable().FirstOrDefault();

        return await Task.FromResult(userRoleId);
    }

    public async Task<Project> GetProject(string userId, int? groupId = null)
    {
        var userProject = dataContext.Database
                          .SqlQueryRaw<Project>("EXEC [dbo].[GetUserProject] @userId = {0}", userId)
                          .AsEnumerable().FirstOrDefault();

        if (userProject == null
            || string.IsNullOrEmpty(userProject.Project_Ini))
        {
            userProject = new Project(await GetActivities("ALL"));
        }
        else
        {
            userProject.Statuses = await GetActivities(userProject.Project_Ini, groupId);
        }

        return await Task.FromResult(userProject);
    }

    public async Task<IEnumerable<Status>> GetActivities(string projectId, int? projectGroupId = null)
    {
        var projectActivities = dataContext.Database
                         .SqlQueryRaw<Status>("EXEC [dbo].[GetProjectActivities] @Project = {0}, @GroupId = {1}", projectId, projectGroupId)
                         .AsEnumerable();

        return await Task.FromResult(projectActivities);
    }

    public async Task<ProjectGroup> GetProjectGroup(string userId)
    {
        var userGroup = dataContext.Database
                         .SqlQueryRaw<ProjectGroup>("EXEC [dbo].[GetUserGroup] @userId = {0}", userId)
                         .AsEnumerable().FirstOrDefault();

        return await Task.FromResult(userGroup);
    }

    public async Task<bool> ValidateCredentialsAsync(string user, string password)
    {
        var result = dataContext.Database.SqlQueryRaw<int>("EXEC [dbo].[ValidateCredential] @user = {0}, @password = {1}", user, password)
            .AsEnumerable().FirstOrDefault();

        return await Task.FromResult(result) > 0;
    }

    public async Task<bool> IsUserNotAllowedAsync(string userId)
    {
        var connectionString = "Server=thor;Database=syn_erp;Uid=syn_system;Pwd=ScaZfHJJQV82sD7G; Connect Timeout=1000;";

        var query = $@"SELECT EXISTS (
                            SELECT 1
                            FROM (
                                SELECT emp_id as EmployeeId, empleado as FullName, supervisor as Supervisor, fecha_inicio as StartTime, fecha_termino as EndTime, fecha_regreso as ReturnTime, estado as Status, 'vacaciones' as Reason
                                FROM erm_emps_vacaciones
                                WHERE estado COLLATE utf8mb4_unicode_ci NOT IN ('Cancelado', 'Completado')
                                UNION ALL
                                SELECT emp_id as EmployeeId, empleado as FullName, supervisor as Supervisor, fecha as StartTime, fecha_hasta as EndTime, '' as ReturnTime, estado as Status, 'permisos' as Reason
                                FROM erm_emps_permisos
                                WHERE estado COLLATE utf8mb4_unicode_ci NOT IN ('Cancelado', 'Completado')
                                UNION ALL 
                                SELECT emp_id as EmployeeId, empleado as FullName, supervisor as Supervisor, fecha_desde as StartTime, fecha_hasta as EndTime, '' as ReturnTime, estado as Status, 'licencias' as Reason
                                FROM erm_emps_licencias
                                WHERE estado COLLATE utf8mb4_unicode_ci NOT IN ('Cancelado', 'Completado')
                                UNION ALL
                                SELECT emp_id as EmployeeId, empleado as FullName, supervisor as Supervisor, DATE_ADD(fecha_salida, INTERVAL 1 DAY) AS StartTime, DATE_ADD(CURDATE(), INTERVAL 1 DAY) AS EndTime, '' as ReturnTime, estado as Status, 'salida' as Reason
                                FROM erm_emps_salidas
                                WHERE estado COLLATE utf8mb4_unicode_ci NOT IN ('Cancelado', 'Procesado')
                            ) AS fuera
                            WHERE EndTime >= CURDATE() AND StartTime <= CURDATE() AND EmployeeId = {userId}) AS IsNotAllowed;";
        try
        {
            EmployeeLeaveStatus employee = new();
            var IsNotAllowed = false;

            using (MySqlConnection connection = new(connectionString))
            {
                connection.Open();

                using MySqlCommand command = new(query, connection);
                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    IsNotAllowed = (long)reader["IsNotAllowed"] > 0;
                }
            }

            return IsNotAllowed;
        }
        catch
        {
            throw;
        }
    }
}

