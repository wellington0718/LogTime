using LogTime.Api.Services;

namespace LogTime.Api.Dal;

public static class DalExtension
{
    public static IServiceCollection AddRepository(this IServiceCollection services)
    {
        services.AddScoped<ILogHistoryRepository, LogHistoryRepository>();
        services.AddScoped<ILogTimeUnitOfWork, LogTimeUnitOfWork>();
        services.AddScoped<IStatusHistoryRepository, StatusHistoryRepository>();
        services.AddScoped<IActiveLogRepository, ActiveLogRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IActiveSessionService, ActiveSessionService>();

        return services;
    }
}
