using LogTime.Api.Repository;
using LogTime.Api.UnitOfWorks;

namespace LogTime.Api.Dal;

public static class DalExtension
{
    public static IServiceCollection AddRepository(this IServiceCollection services)
    {
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ILogHistoryRepository, LogHistoryRepository>();
        services.AddScoped<ILogTimeUnitOfWork, LogTimeUnitOfWork>();
        services.AddScoped<IStatusHistoryRepository, StatusHistoryRepository>();
        services.AddScoped<IActiveLogRepository, ActiveLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }
}
