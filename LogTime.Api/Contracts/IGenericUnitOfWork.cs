namespace LogTime.Api.Contracts;

public interface IGenericUnitOfWork
{
    Task<bool> SaveChangesAsync();
    Task CommitAsync();
}
