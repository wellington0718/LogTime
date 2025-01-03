namespace LogTime.Api.UnitOfWorks;

public abstract class GenericUnitOfWork(LogTimeDataContext context) : IGenericUnitOfWork, IDisposable
{
    private IDbContextTransaction transaction = context.Database.BeginTransaction();

    public async Task<bool> SaveChangesAsync()
    {
        var result = await context.SaveChangesAsync();
        return result > 0;
    }

    public async Task CommitAsync()
    {
        try
        {
            await transaction.CommitAsync();
        }
        catch (Exception)
        {
            await transaction.RollbackAsync();
            throw;
        }
        finally
        {
            await transaction.DisposeAsync();
            transaction = await context.Database.BeginTransactionAsync();
        }
    }

    public void Dispose()
    {
        if (transaction != null)
        {
            transaction.Dispose();
            transaction = null;
        }
    }
}
