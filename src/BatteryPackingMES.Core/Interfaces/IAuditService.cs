using BatteryPackingMES.Core.Entities;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 审计服务接口
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// 记录审计日志
    /// </summary>
    /// <param name="actionType">操作类型</param>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="description">操作描述</param>
    /// <param name="oldValues">旧值</param>
    /// <param name="newValues">新值</param>
    /// <param name="changedFields">变更字段</param>
    /// <returns></returns>
    Task LogAsync(string actionType, string? entityType = null, long? entityId = null, 
        string? description = null, object? oldValues = null, object? newValues = null, 
        List<string>? changedFields = null);

    /// <summary>
    /// 记录用户登录日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <param name="success">是否成功</param>
    /// <param name="errorMessage">错误信息</param>
    /// <returns></returns>
    Task LogLoginAsync(long userId, string userName, bool success, string? errorMessage = null);

    /// <summary>
    /// 记录用户登出日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="userName">用户名</param>
    /// <returns></returns>
    Task LogLogoutAsync(long userId, string userName);

    /// <summary>
    /// 记录实体创建日志
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体对象</param>
    /// <param name="description">操作描述</param>
    /// <returns></returns>
    Task LogCreateAsync<T>(T entity, string? description = null) where T : BaseEntity;

    /// <summary>
    /// 记录实体更新日志
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="oldEntity">更新前实体</param>
    /// <param name="newEntity">更新后实体</param>
    /// <param name="description">操作描述</param>
    /// <returns></returns>
    Task LogUpdateAsync<T>(T oldEntity, T newEntity, string? description = null) where T : BaseEntity;

    /// <summary>
    /// 记录实体删除日志
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">实体对象</param>
    /// <param name="description">操作描述</param>
    /// <returns></returns>
    Task LogDeleteAsync<T>(T entity, string? description = null) where T : BaseEntity;

    /// <summary>
    /// 获取审计日志
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="actionType">操作类型</param>
    /// <param name="userId">用户ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns></returns>
    Task<(List<AuditLog> logs, int totalCount)> GetAuditLogsAsync(
        string? entityType = null, long? entityId = null, string? actionType = null, 
        long? userId = null, DateTime? startTime = null, DateTime? endTime = null,
        int pageIndex = 1, int pageSize = 50);

    /// <summary>
    /// 获取实体审计历史
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <returns></returns>
    Task<List<AuditLog>> GetEntityAuditHistoryAsync(string entityType, long entityId);

    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns></returns>
    Task<(List<AuditLog> logs, int totalCount)> GetUserAuditLogsAsync(
        long userId, DateTime? startTime = null, DateTime? endTime = null,
        int pageIndex = 1, int pageSize = 50);

    /// <summary>
    /// 获取审计统计报告
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns></returns>
    Task<Dictionary<string, object>> GetAuditReportAsync(DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// 清理过期审计日志
    /// </summary>
    /// <returns></returns>
    Task CleanupExpiredLogsAsync();

    /// <summary>
    /// 导出审计日志
    /// </summary>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="entityType">实体类型</param>
    /// <param name="format">导出格式（CSV, Excel, JSON）</param>
    /// <returns></returns>
    Task<byte[]> ExportAuditLogsAsync(DateTime startTime, DateTime endTime, 
        string? entityType = null, string format = "CSV");
} 