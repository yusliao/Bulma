using BatteryPackingMES.Core.Entities;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 数据验证服务接口
/// </summary>
public interface IDataValidationService
{
    /// <summary>
    /// 验证实体数据
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entity">要验证的实体</param>
    /// <returns>验证结果列表</returns>
    Task<List<DataValidationResult>> ValidateEntityAsync<T>(T entity) where T : BaseEntity;

    /// <summary>
    /// 验证实体列表
    /// </summary>
    /// <typeparam name="T">实体类型</typeparam>
    /// <param name="entities">要验证的实体列表</param>
    /// <returns>验证结果列表</returns>
    Task<List<DataValidationResult>> ValidateEntitiesAsync<T>(List<T> entities) where T : BaseEntity;

    /// <summary>
    /// 添加验证规则
    /// </summary>
    /// <param name="rule">验证规则</param>
    /// <returns></returns>
    Task<DataValidationRule> AddValidationRuleAsync(DataValidationRule rule);

    /// <summary>
    /// 更新验证规则
    /// </summary>
    /// <param name="rule">验证规则</param>
    /// <returns></returns>
    Task<DataValidationRule> UpdateValidationRuleAsync(DataValidationRule rule);

    /// <summary>
    /// 删除验证规则
    /// </summary>
    /// <param name="ruleId">规则ID</param>
    /// <returns></returns>
    Task<bool> DeleteValidationRuleAsync(long ruleId);

    /// <summary>
    /// 获取实体类型的验证规则
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <returns>验证规则列表</returns>
    Task<List<DataValidationRule>> GetValidationRulesAsync(string entityType);

    /// <summary>
    /// 获取验证结果
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="entityId">实体ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>验证结果列表</returns>
    Task<List<DataValidationResult>> GetValidationResultsAsync(string? entityType = null, long? entityId = null, DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// 获取数据质量报告
    /// </summary>
    /// <param name="entityType">实体类型</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>数据质量报告</returns>
    Task<Dictionary<string, object>> GetDataQualityReportAsync(string? entityType = null, DateTime? startTime = null, DateTime? endTime = null);

    /// <summary>
    /// 运行数据质量检查作业
    /// </summary>
    /// <param name="entityType">实体类型（可选）</param>
    /// <returns></returns>
    Task RunDataQualityJobAsync(string? entityType = null);
} 