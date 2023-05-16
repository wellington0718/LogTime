using DataAccess;
using DataAccess.Models;
using Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.UnitsOfWork
{
    public interface ISessionUnitOfWork
    {
        SessionLogRepository SessionLogRepository { get; }
        ActivityLogRepository ActivityLogRepository { get; }
        ActiveSessionRepository ActiveSessionRepository { get; }
        CredentialRepository CredentialRepository { get; }
        UserRepository UserRepository { get; }
        Task<ActiveSession> CreateSession(ClientData newSessionData);
        Task CloseExistingSessions(string paddedUserId, string loggedOutBy = "");
        void Commit();
    }
    public class SessionUnitOfWork : GenericUnitOfWork, ISessionUnitOfWork
    {

        public SessionUnitOfWork(IConfiguration configuration)
            : base(new SqlConnection(configuration.GetConnectionString(nameof(ConnectionStringName.LogTime))))
        {
        }

        private SessionLogRepository sessionLogRepository;
        public SessionLogRepository SessionLogRepository =>
            sessionLogRepository ??= new SessionLogRepository(dataBaseAccess);

        private ActivityLogRepository activityLogRepository;
        public ActivityLogRepository ActivityLogRepository =>
            activityLogRepository ??= new ActivityLogRepository(dataBaseAccess);

        private ActiveSessionRepository activeSessionRepository;

        public ActiveSessionRepository ActiveSessionRepository =>
            activeSessionRepository ??= new ActiveSessionRepository(dataBaseAccess);

        private CredentialRepository credentialRepository;
        public CredentialRepository CredentialRepository =>
            credentialRepository ??= new CredentialRepository(dataBaseAccess);

        private UserRepository userRepository;
        public UserRepository UserRepository =>
            userRepository ??= new UserRepository(dataBaseAccess);

        protected override void ResetRepositories()
        {
            sessionLogRepository = null;
            activityLogRepository = null;
            activeSessionRepository = null;
            credentialRepository = null;
            userRepository = null;
        }

        public async Task<ActiveSession> CreateSession(ClientData newSessionData)
        {
            var newSessionLog =
                await CreateSessionLog(newSessionData.Credential.User, newSessionData.HostName);

            var newActivityLog = await CreateActivityLog(newSessionLog);

            return await CreateActiveSession(newActivityLog, newSessionData.Credential.User);
        }

        public async Task<IEnumerable<SessionLog>> GetActiveSessions(string usersIds)
        {
            var ids = usersIds.Split(',');

            if (ids.Length > 1)
            {
                return (await SessionLogRepository.GetActiveByUsersAsync(usersIds))
                    .ToList();
            }
            else if (ids.Length == 1)
            {
                return (await SessionLogRepository.GetActiveByUserIdAsync(usersIds))
                    .ToList();
            }

            return new List<SessionLog>();
        }

        public async Task CloseExistingSessions(string userId, string loggedOutBy = "")
        {
            var openedSessions = await GetActiveSessions(userId);

            if (!openedSessions.Any())
            {
                return;
            }

            var currentDate = DateTime.Now;

            var logOutDate = currentDate;

            foreach (var sessionLog in openedSessions)
            {
                if (sessionLog.LastTimeConnectionAlive.HasValue
                    && RelevantTimeDifference(currentDate, sessionLog.LastTimeConnectionAlive.Value))
                {
                    logOutDate = sessionLog.LastTimeConnectionAlive.Value;
                }

                sessionLog.LogoutDate = logOutDate;
                sessionLog.LogedOutBy = string.IsNullOrEmpty(loggedOutBy) ? "New session" : loggedOutBy;

                var activityLogs = await ActivityLogRepository.GetUnfinishedAsync(sessionLog.Id);

                foreach (var activityLog in activityLogs)
                {
                    activityLog.StatusEndTime = sessionLog.LogoutDate;                    
                }
                await ActivityLogRepository.UpdateEndTimeAsync(activityLogs);                               
            }
            await SessionLogRepository.UpdateLogoutDataAsync(openedSessions);
            await ActiveSessionRepository.RemoveAsync(openedSessions);
        }                

        private async Task<ActiveSession> CreateActiveSession(ActivityLog newActivityLog, string paddedUserId)
        {
            var activeSession = new ActiveSession
            {
                ActualLogHistoryId = newActivityLog.LogId,
                StatusId = 1,
                UserId = paddedUserId,
                ActualStatusHistoryId = newActivityLog.Id,
                StartDate = DateTime.Now
            };
            activeSession.Id = await ActiveSessionRepository.AddAsync(activeSession);

            return activeSession;
        }

        private async Task<ActivityLog> CreateActivityLog(SessionLog newSessionLog)
        {
            var activityLog = new ActivityLog
            {
                StatusStartTime = newSessionLog.LoginDate,
                LogId = newSessionLog.Id,
                StatusId = 1
            };
            activityLog.Id = await ActivityLogRepository.AddAsync(activityLog);

            return activityLog;
        }

        private async Task<SessionLog> CreateSessionLog(string paddedUserId, string hostName)
        {
            var startDate = DateTime.Now;
            var sessionLog = new SessionLog
            {
                LoginDate = startDate,
                LastTimeConnectionAlive = startDate,
                IdUser = paddedUserId,
                Hostname = hostName              
            };
            sessionLog.Id = await SessionLogRepository.AddAsync(sessionLog);

            return sessionLog;
        }

        private static bool RelevantTimeDifference(DateTime currentTime, DateTime lastConnectionAliveTime)
        {
            return (currentTime - lastConnectionAliveTime).TotalMinutes is > 2 or < 0;
        }
    }
}
