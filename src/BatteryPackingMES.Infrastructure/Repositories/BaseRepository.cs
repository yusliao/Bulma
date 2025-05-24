using System.Linq.Expressions;
using SqlSugar;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Infrastructure.Data;

namespace BatteryPackingMES.Infrastructure.Repositories;

/// <summary>
/// 基础仓储实现
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public class BaseRepository<T> : IRepository<T> where T : BaseEntity, new()
{
    protected readonly MESDbContext _dbContext;

    public BaseRepository(MESDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    public async Task<T?> GetByIdAsync(long id)
    {
        var db = _dbContext.GetSlaveDb();
        return await db.Queryable<T>().FirstAsync(x => x.Id == id);
    }

    /// <summary>
    /// 获取所有实体
    /// </summary>
    public async Task<List<T>> GetAllAsync()
    {
        var db = _dbContext.GetSlaveDb();
        return await db.Queryable<T>().ToListAsync();
    }

    /// <summary>
    /// 根据条件获取所有实体
    /// </summary>
    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate)
    {
        var db = _dbContext.GetSlaveDb();
        return await db.Queryable<T>().Where(predicate).ToListAsync();
    }

    /// <summary>
    /// 根据条件获取第一个或默认实体
    /// </summary>
    public async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        var db = _dbContext.GetSlaveDb();
        return await db.Queryable<T>().Where(predicate).FirstAsync();
    }

    /// <summary>
    /// 根据条件获取第一个实体
    /// </summary>
    public async Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate)
    {
        var db = _dbContext.GetSlaveDb();
        return await db.Queryable<T>().Where(predicate).FirstAsync();
    }

    /// <summary>
    /// 根据条件查询
    /// </summary>
    public async Task<List<T>> GetByConditionAsync(Expression<Func<T, bool>> predicate)
    {
        var db = _dbContext.GetSlaveDb();
        return await db.Queryable<T>().Where(predicate).ToListAsync();
    }

    /// <summary>
    /// 分页查询
    /// </summary>
    public async Task<(List<T> Items, int Total)> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? predicate = null)
    {
        var db = _dbContext.GetSlaveDb();
        var query = db.Queryable<T>();
        
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        var total = await query.CountAsync();
        var items = await query.ToPageListAsync(pageIndex, pageSize);

        return (items, total);
    }

    /// <summary>
    /// 添加实体
    /// </summary>
    public async Task<long> AddAsync(T entity)
    {
        var db = _dbContext.GetMasterDb();
        entity.CreatedTime = DateTime.Now;
        entity.Version = 1;
        
        var result = await db.Insertable(entity).ExecuteReturnSnowflakeIdAsync();
        return result;
    }

    /// <summary>
    /// 批量添加
    /// </summary>
    public async Task<bool> AddRangeAsync(List<T> entities)
    {
        var db = _dbContext.GetMasterDb();
        var now = DateTime.Now;
        
        foreach (var entity in entities)
        {
            entity.CreatedTime = now;
            entity.Version = 1;
        }

        var result = await db.Insertable(entities).ExecuteCommandAsync();
        return result > 0;
    }

    /// <summary>
    /// 更新实体
    /// </summary>
    public async Task<bool> UpdateAsync(T entity)
    {
        var db = _dbContext.GetMasterDb();
        entity.UpdatedTime = DateTime.Now;
        entity.Version++;

        var result = await db.Updateable(entity)
            .Where(x => x.Id == entity.Id && x.Version == entity.Version - 1)
            .ExecuteCommandAsync();
        
        return result > 0;
    }

    /// <summary>
    /// 软删除
    /// </summary>
    public async Task<bool> DeleteAsync(long id)
    {
        var db = _dbContext.GetMasterDb();
        var result = await db.Updateable<T>()
            .SetColumns(x => new T { IsDeleted = true, UpdatedTime = DateTime.Now })
            .Where(x => x.Id == id)
            .ExecuteCommandAsync();

        return result > 0;
    }

    /// <summary>
    /// 硬删除实体
    /// </summary>
    public async Task<bool> DeleteAsync(T entity)
    {
        var db = _dbContext.GetMasterDb();
        var result = await db.Deleteable(entity).ExecuteCommandAsync();
        return result > 0;
    }

    /// <summary>
    /// 批量软删除
    /// </summary>
    public async Task<bool> DeleteRangeAsync(List<long> ids)
    {
        var db = _dbContext.GetMasterDb();
        var result = await db.Updateable<T>()
            .SetColumns(x => new T { IsDeleted = true, UpdatedTime = DateTime.Now })
            .Where(x => ids.Contains(x.Id))
            .ExecuteCommandAsync();

        return result > 0;
    }

    /// <summary>
    /// 检查是否存在
    /// </summary>
    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        var db = _dbContext.GetSlaveDb();
        return await db.Queryable<T>().AnyAsync(predicate);
    }

    /// <summary>
    /// 获取数量
    /// </summary>
    public async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        var db = _dbContext.GetSlaveDb();
        var query = db.Queryable<T>();
        
        if (predicate != null)
        {
            query = query.Where(predicate);
        }

        return await query.CountAsync();
    }
} 