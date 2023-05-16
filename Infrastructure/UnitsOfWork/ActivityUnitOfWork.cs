using DataAccess;
using Infrastructure.Repositories;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;

namespace Infrastructure.UnitsOfWork
{
    public interface IActivityUnitOfWork
    {
        ActivityLogRepository ActivityLogRepository { get; }
        ActiveSessionRepository ActiveSessionRepository { get; }

        void Commit();
    }

    public class ActivityUnitOfWork : GenericUnitOfWork, IActivityUnitOfWork
    {
        public ActivityUnitOfWork(IConfiguration configuration)
            : base(new SqlConnection(configuration.GetConnectionString(nameof(ConnectionStringName.LogTime))))
        {
        }

        private ActivityLogRepository activityLogRepository;
        public ActivityLogRepository ActivityLogRepository =>
            activityLogRepository ??= new ActivityLogRepository(dataBaseAccess);

        private ActiveSessionRepository activeSessionRepository;

        public ActiveSessionRepository ActiveSessionRepository =>
            activeSessionRepository ??= new ActiveSessionRepository(dataBaseAccess);

        protected override void ResetRepositories()
        {
            activityLogRepository = null;
            activeSessionRepository = null;
        }
    }
}
