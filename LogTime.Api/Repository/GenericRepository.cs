using LogTime.Api.Contracts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq.Expressions;
using System.Threading;

namespace LogTime.Api.Repository;

public class GenericRepository<TEntity>(LogTimeDataContext dataContext) : IGenericRepository<TEntity>
    where TEntity : class
{

    public IQueryable<TEntity> GetQueryable() => dataContext.Set<TEntity>().AsNoTracking();

    public async Task<TEntity> CreateAsync(TEntity entity)
    {
        var result = await dataContext.AddAsync(entity);
        return result.Entity;
    }

    public async Task<bool> DeleteAsync(object key)
    {
        var entry = await FindAsync(key);
        var deletedEntity = dataContext.Remove(entry);
        return deletedEntity.State == EntityState.Deleted;
    }

    public void DeleteAsync(IEnumerable<TEntity> entities)
    {
        dataContext.RemoveRange(entities);
    }

    public async Task<bool> DeleteAsync(Expression<Func<TEntity, bool>> expression)
    {
        var entities = GetQueryable().Where(expression);
        var restult = await entities.ExecuteDeleteAsync();

        return restult > 0;
    }

    public Task<TEntity> FindAsync(Expression<Func<TEntity, bool>> expression)
    {
        var entity = GetQueryable().FirstOrDefaultAsync(expression);
        return entity;
    }

    public Task<TEntity> FindAsync(object key)
    {
        var entity = dataContext.Set<TEntity>().FindAsync(key).AsTask();
        return entity;
    }

    public async Task<IEnumerable<TEntity>> GetListAsync()
    {
        return await GetQueryable().ToListAsync();
    }

    public async Task<IEnumerable<TEntity>> GetListAsync(Expression<Func<TEntity, bool>> expression)
    {
        return await GetQueryable().Where(expression).ToListAsync();
    }

    public bool Update(TEntity entity)
    {
        var entry = dataContext.Update(entity);
        return entry.State == EntityState.Modified;
    }

    public void UpdateRange(IEnumerable<TEntity> entities)
    {
        dataContext.UpdateRange(entities);
    }

    public async Task<bool> SaveChangesAsync()
    {
        var result = await dataContext.SaveChangesAsync();

        return result > 0;
    }
}
