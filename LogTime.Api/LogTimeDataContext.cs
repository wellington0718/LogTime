namespace LogTime.Api;

public class LogTimeDataContext(DbContextOptions<LogTimeDataContext> options) : DbContext(options)
{
    public DbSet<LogHistory> LogHistory { get; set; }
    public DbSet<StatusHistory> StatusHistory { get; set; }
    public DbSet<ActiveLog> ActiveLog { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ActiveLog>()
            .HasOne(al => al.LogHistory)  // Navigation property
            .WithMany(lh => lh.ActiveLogs) // Collection in LogHistory
            .HasForeignKey(al => al.ActualLogHistoryId); // Specify FK

        modelBuilder.Entity<StatusHistory>()
            .HasOne(al => al.LogHistory)  // Navigation property
            .WithMany(lh => lh.StatusHistories) // Collection in LogHistory
            .HasForeignKey(al => al.LogId); // Specify FK
    }
}
