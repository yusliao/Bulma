using System.Linq.Expressions;
using BatteryPackingMES.Core.Entities;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 基础仓储接口
/// </summary>
/// <typeparam name="T">实体类型</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    /// <summary>
    /// 根据ID获取实体
    /// </summary>
    Task<T?> GetByIdAsync(long id);

    /// <summary>
    /// 获取所有实体
    /// </summary>
    Task<List<T>> GetAllAsync();

    /// <summary>
    /// 根据条件获取所有实体
    /// </summary>
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 根据条件获取第一个或默认实体
    /// </summary>
    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 根据条件获取第一个实体
    /// </summary>
    Task<T?> GetFirstAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 根据条件查询
    /// </summary>
    Task<List<T>> GetByConditionAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 分页查询
    /// </summary>
    Task<(List<T> Items, int Total)> GetPagedAsync(int pageIndex, int pageSize, Expression<Func<T, bool>>? predicate = null);

    /// <summary>
    /// 添加实体
    /// </summary>
    Task<long> AddAsync(T entity);

    /// <summary>
    /// 批量添加
    /// </summary>
    Task<bool> AddRangeAsync(List<T> entities);

    /// <summary>
    /// 更新实体
    /// </summary>
    Task<bool> UpdateAsync(T entity);

    /// <summary>
    /// 软删除
    /// </summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>
    /// 硬删除实体
    /// </summary>
    Task<bool> DeleteAsync(T entity);

    /// <summary>
    /// 批量软删除
    /// </summary>
    Task<bool> DeleteRangeAsync(List<long> ids);

    /// <summary>
    /// 检查是否存在
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// 获取数量
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);
} 