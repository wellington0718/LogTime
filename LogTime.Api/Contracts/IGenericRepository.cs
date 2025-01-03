namespace LogTime.Api.Contracts;

public interface IGenericRepository<TEntity> where TEntity : class
{
    IQueryable<TEntity> GetQueryable();
    Task<TEntity> CreateAsync(TEntity entity);
    Task<TEntity> FindAsync(object key);
    Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> expression);
    Task<IEnumerable<TEntity>> GetListAsync();
    Task<IEnumerable<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> expression);
    Task<bool> DeleteAsync(object key);
    Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> expression);
    void DeleteAsync(IEnumerable<TEntity> entities);
    bool Update(TEntity entity);
    void UpdateRange(IEnumerable<TEntity> entities);
    Task<bool> SaveChangesAsync();
}
