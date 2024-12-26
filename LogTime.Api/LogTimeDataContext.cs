namespace LogTime.Api;

public class LogTimeDataContext(DbContextOptions<LogTimeDataContext> options) : DbContext(options)
{
    public DbSet<LogHistory> LogHistory { get; set; }
    public DbSet<StatusHistory> StatusHistory { get; set; }
    public DbSet<ActiveLog> ActiveLog { get; set; }
}
