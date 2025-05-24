using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 审计服务实现
/// </summary>
public class AuditService : IAuditService
{
    private readonly IRepository<AuditLog> _auditRepository;
    private readonly IRepository<AuditConfiguration> _configRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditService> _logger;

    public AuditService(
        IRepository<AuditLog> auditRepository,
        IRepository<AuditConfiguration> configRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditService> logger)
    {
        _auditRepository = auditRepository;
        _configRepository = configRepository;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// 记录审计日志
    /// </summary>
    public async Task LogAsync(string actionType, string? entityType = null, long? entityId = null,
        string? description = null, object? oldValues = null, object? newValues = null,
        List<string>? changedFields = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                ActionType = actionType,
                EntityType = entityType,
                EntityId = entityId,
                Description = description,
                OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
                NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
                ChangedFields = changedFields != null ? string.Join(",", changedFields) : null,
                ActionTime = DateTime.UtcNow,
                Result = "Success"
            };

            // 设置用户信息
            SetUserInfo(auditLog);

            // 设置HTTP上下文信息
            SetHttpContextInfo(auditLog);

            // 评估风险等级
            auditLog.RiskLevel = EvaluateRiskLevel(actionType, entityType);

            await _auditRepository.AddAsync(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录审计日志失败: {ActionType}", actionType);
        }
    }

    /// <summary>
    /// 记录用户登录日志
    /// </summary>
    public async Task LogLoginAsync(long userId, string userName, bool success, string? errorMessage = null)
    {
        try
        {
            var auditLog = new AuditLog
            {
                ActionType = "Login",
                UserId = userId,
                UserName = userName,
                Description = success ? "用户登录成功" : "用户登录失败",
                Result = success ? "Success" : "Failed",
                ErrorMessage = errorMessage,
                ActionTime = DateTime.UtcNow,
                RiskLevel = success ? "Low" : "Medium"
            };

            SetHttpContextInfo(auditLog);
            await _auditRepository.AddAsync(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录登录审计日志失败: {UserName}", userName);
        }
    }

    /// <summary>
    /// 记录用户登出日志
    /// </summary>
    public async Task LogLogoutAsync(long userId, string userName)
    {
        await LogAsync("Logout", description: $"用户 {userName} 登出系统");
    }

    /// <summary>
    /// 记录实体创建日志
    /// </summary>
    public async Task LogCreateAsync<T>(T entity, string? description = null) where T : BaseEntity
    {
        var entityType = typeof(T).Name;
        var entityJson = JsonSerializer.Serialize(entity);
        
        await LogAsync("Create", entityType, entity.Id, 
            description ?? $"创建{entityType}", null, entity);
    }

    /// <summary>
    /// 记录实体更新日志
    /// </summary>
    public async Task LogUpdateAsync<T>(T oldEntity, T newEntity, string? description = null) where T : BaseEntity
    {
        var entityType = typeof(T).Name;
        var changedFields = GetChangedFields(oldEntity, newEntity);
        
        if (changedFields.Any())
        {
            await LogAsync("Update", entityType, newEntity.Id,
                description ?? $"更新{entityType}", oldEntity, newEntity, changedFields);
        }
    }

    /// <summary>
    /// 记录实体删除日志
    /// </summary>
    public async Task LogDeleteAsync<T>(T entity, string? description = null) where T : BaseEntity
    {
        var entityType = typeof(T).Name;
        
        await LogAsync("Delete", entityType, entity.Id,
            description ?? $"删除{entityType}", entity, null);
    }

    /// <summary>
    /// 获取审计日志
    /// </summary>
    public async Task<(List<AuditLog> logs, int totalCount)> GetAuditLogsAsync(
        string? entityType = null, long? entityId = null, string? actionType = null,
        long? userId = null, DateTime? startTime = null, DateTime? endTime = null,
        int pageIndex = 1, int pageSize = 50)
    {
        try
        {
            Expression<Func<AuditLog, bool>> predicate = a => true;

            if (!string.IsNullOrEmpty(entityType))
                predicate = CombineExpressions(predicate, a => a.EntityType == entityType);

            if (entityId.HasValue)
                predicate = CombineExpressions(predicate, a => a.EntityId == entityId.Value);

            if (!string.IsNullOrEmpty(actionType))
                predicate = CombineExpressions(predicate, a => a.ActionType == actionType);

            if (userId.HasValue)
                predicate = CombineExpressions(predicate, a => a.UserId == userId.Value);

            if (startTime.HasValue)
                predicate = CombineExpressions(predicate, a => a.ActionTime >= startTime.Value);

            if (endTime.HasValue)
                predicate = CombineExpressions(predicate, a => a.ActionTime <= endTime.Value);

            var (logs, total) = await _auditRepository.GetPagedAsync(pageIndex, pageSize, predicate);
            return (logs.OrderByDescending(a => a.ActionTime).ToList(), total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取审计日志失败");
            return (new List<AuditLog>(), 0);
        }
    }

    /// <summary>
    /// 获取实体审计历史
    /// </summary>
    public async Task<List<AuditLog>> GetEntityAuditHistoryAsync(string entityType, long entityId)
    {
        try
        {
            var logs = await _auditRepository.GetByConditionAsync(a => 
                a.EntityType == entityType && a.EntityId == entityId);
            
            return logs.OrderByDescending(a => a.ActionTime).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实体审计历史失败: {EntityType}:{EntityId}", entityType, entityId);
            return new List<AuditLog>();
        }
    }

    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    public async Task<(List<AuditLog> logs, int totalCount)> GetUserAuditLogsAsync(
        long userId, DateTime? startTime = null, DateTime? endTime = null,
        int pageIndex = 1, int pageSize = 50)
    {
        return await GetAuditLogsAsync(null, null, null, userId, startTime, endTime, pageIndex, pageSize);
    }

    /// <summary>
    /// 获取审计统计报告
    /// </summary>
    public async Task<Dictionary<string, object>> GetAuditReportAsync(DateTime? startTime = null, DateTime? endTime = null)
    {
        var report = new Dictionary<string, object>();

        try
        {
            Expression<Func<AuditLog, bool>> predicate = a => true;

            if (startTime.HasValue)
                predicate = CombineExpressions(predicate, a => a.ActionTime >= startTime.Value);

            if (endTime.HasValue)
                predicate = CombineExpressions(predicate, a => a.ActionTime <= endTime.Value);

            var logs = await _auditRepository.GetByConditionAsync(predicate);

            // 基本统计
            report["total_operations"] = logs.Count;
            report["unique_users"] = logs.GroupBy(a => a.UserId).Count();
            report["success_rate"] = logs.Count > 0 ? (double)logs.Count(a => a.Result == "Success") / logs.Count * 100 : 100;

            // 按操作类型统计
            var byAction = logs.GroupBy(a => a.ActionType)
                .ToDictionary(g => g.Key, g => g.Count());
            report["by_action_type"] = byAction;

            // 按实体类型统计
            var byEntity = logs.Where(a => !string.IsNullOrEmpty(a.EntityType))
                .GroupBy(a => a.EntityType!)
                .ToDictionary(g => g.Key, g => g.Count());
            report["by_entity_type"] = byEntity;

            // 按风险等级统计
            var byRisk = logs.GroupBy(a => a.RiskLevel)
                .ToDictionary(g => g.Key, g => g.Count());
            report["by_risk_level"] = byRisk;

            // 时间分布
            if (logs.Any())
            {
                var timeGroups = logs.GroupBy(a => a.ActionTime.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());
                report["time_distribution"] = timeGroups;
            }

            // 最活跃用户
            var topUsers = logs.GroupBy(a => new { a.UserId, a.UserName })
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key.UserName, g => g.Count());
            report["top_users"] = topUsers;

            report["generated_at"] = DateTime.UtcNow;
            report["period"] = new { start = startTime, end = endTime };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成审计统计报告失败");
            report["error"] = ex.Message;
        }

        return report;
    }

    /// <summary>
    /// 清理过期审计日志
    /// </summary>
    public async Task CleanupExpiredLogsAsync()
    {
        try
        {
            var configs = await _configRepository.GetAllAsync();
            
            foreach (var config in configs)
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-config.RetentionDays);
                var expiredLogs = await _auditRepository.GetByConditionAsync(a => 
                    a.EntityType == config.EntityType && a.ActionTime < cutoffDate);

                foreach (var log in expiredLogs)
                {
                    await _auditRepository.DeleteAsync(log.Id);
                }

                _logger.LogInformation("清理过期审计日志: {EntityType}, 清理数量: {Count}", 
                    config.EntityType, expiredLogs.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "清理过期审计日志失败");
        }
    }

    /// <summary>
    /// 导出审计日志
    /// </summary>
    public async Task<byte[]> ExportAuditLogsAsync(DateTime startTime, DateTime endTime,
        string? entityType = null, string format = "CSV")
    {
        try
        {
            var (logs, _) = await GetAuditLogsAsync(entityType, null, null, null, startTime, endTime, 1, int.MaxValue);

            return format.ToUpper() switch
            {
                "CSV" => ExportToCsv(logs),
                "JSON" => ExportToJson(logs),
                "EXCEL" => ExportToExcel(logs),
                _ => ExportToCsv(logs)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出审计日志失败");
            throw;
        }
    }

    #region 私有方法

    /// <summary>
    /// 设置用户信息
    /// </summary>
    private void SetUserInfo(AuditLog auditLog)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                var userIdClaim = httpContext.User.FindFirst("UserId")?.Value;
                var userNameClaim = httpContext.User.FindFirst("UserName")?.Value ?? 
                                  httpContext.User.Identity.Name ?? "Unknown";

                if (long.TryParse(userIdClaim, out var userId))
                {
                    auditLog.UserId = userId;
                }
                auditLog.UserName = userNameClaim;
            }
            else
            {
                auditLog.UserId = 0; // 系统操作
                auditLog.UserName = "System";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "设置审计日志用户信息失败");
            auditLog.UserId = 0;
            auditLog.UserName = "Unknown";
        }
    }

    /// <summary>
    /// 设置HTTP上下文信息
    /// </summary>
    private void SetHttpContextInfo(AuditLog auditLog)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext != null)
            {
                auditLog.IpAddress = GetClientIpAddress(httpContext);
                auditLog.UserAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
                auditLog.SessionId = httpContext.Session?.Id;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "设置审计日志HTTP上下文信息失败");
        }
    }

    /// <summary>
    /// 获取客户端IP地址
    /// </summary>
    private string? GetClientIpAddress(HttpContext context)
    {
        var ipAddress = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        }
        if (string.IsNullOrEmpty(ipAddress))
        {
            ipAddress = context.Connection.RemoteIpAddress?.ToString();
        }
        return ipAddress;
    }

    /// <summary>
    /// 评估风险等级
    /// </summary>
    private string EvaluateRiskLevel(string actionType, string? entityType)
    {
        return actionType switch
        {
            "Delete" => "High",
            "Login" when entityType == "Admin" => "Medium",
            "Update" when new[] { "User", "Role", "Permission" }.Contains(entityType) => "Medium",
            "Create" when new[] { "User", "Role", "Permission" }.Contains(entityType) => "Medium",
            _ => "Low"
        };
    }

    /// <summary>
    /// 获取变更字段
    /// </summary>
    private List<string> GetChangedFields<T>(T oldEntity, T newEntity) where T : BaseEntity
    {
        var changedFields = new List<string>();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            if (property.Name == "UpdatedTime" || property.Name == "UpdatedBy" || property.Name == "Version")
                continue;

            var oldValue = property.GetValue(oldEntity);
            var newValue = property.GetValue(newEntity);

            if (!Equals(oldValue, newValue))
            {
                changedFields.Add(property.Name);
            }
        }

        return changedFields;
    }

    /// <summary>
    /// 组合表达式
    /// </summary>
    private Expression<Func<T, bool>> CombineExpressions<T>(
        Expression<Func<T, bool>> first,
        Expression<Func<T, bool>> second)
    {
        var parameter = Expression.Parameter(typeof(T));
        var leftVisitor = new ReplaceExpressionVisitor(first.Parameters[0], parameter);
        var left = leftVisitor.Visit(first.Body);

        var rightVisitor = new ReplaceExpressionVisitor(second.Parameters[0], parameter);
        var right = rightVisitor.Visit(second.Body);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left!, right!), parameter);
    }

    /// <summary>
    /// 导出为CSV格式
    /// </summary>
    private byte[] ExportToCsv(List<AuditLog> logs)
    {
        var csv = new StringBuilder();
        csv.AppendLine("ActionTime,ActionType,EntityType,EntityId,UserId,UserName,IpAddress,Description,Result,RiskLevel");

        foreach (var log in logs)
        {
            csv.AppendLine($"{log.ActionTime:yyyy-MM-dd HH:mm:ss},{log.ActionType},{log.EntityType},{log.EntityId},{log.UserId},{log.UserName},{log.IpAddress},\"{log.Description}\",{log.Result},{log.RiskLevel}");
        }

        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    /// <summary>
    /// 导出为JSON格式
    /// </summary>
    private byte[] ExportToJson(List<AuditLog> logs)
    {
        var json = JsonSerializer.Serialize(logs, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        return Encoding.UTF8.GetBytes(json);
    }

    /// <summary>
    /// 导出为Excel格式
    /// </summary>
    private byte[] ExportToExcel(List<AuditLog> logs)
    {
        // 这里可以使用EPPlus或其他Excel库来生成Excel文件
        // 为了简化，这里返回CSV格式
        return ExportToCsv(logs);
    }

    #endregion
} 