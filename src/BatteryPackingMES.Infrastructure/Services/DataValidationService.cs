using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Reflection;
using System.Linq.Expressions;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 数据验证服务实现
/// </summary>
public class DataValidationService : IDataValidationService
{
    private readonly IRepository<DataValidationRule> _ruleRepository;
    private readonly IRepository<DataValidationResult> _resultRepository;
    private readonly ILogger<DataValidationService> _logger;

    public DataValidationService(
        IRepository<DataValidationRule> ruleRepository,
        IRepository<DataValidationResult> resultRepository,
        ILogger<DataValidationService> logger)
    {
        _ruleRepository = ruleRepository;
        _resultRepository = resultRepository;
        _logger = logger;
    }

    /// <summary>
    /// 验证实体数据
    /// </summary>
    public async Task<List<DataValidationResult>> ValidateEntityAsync<T>(T entity) where T : BaseEntity
    {
        var results = new List<DataValidationResult>();
        var entityType = typeof(T).Name;

        try
        {
            var rules = await GetValidationRulesAsync(entityType);
            
            foreach (var rule in rules.Where(r => r.IsEnabled))
            {
                var result = await ValidateEntityWithRuleAsync(entity, rule);
                if (result != null)
                {
                    results.Add(result);
                    await _resultRepository.AddAsync(result);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证实体数据失败: {EntityType}:{EntityId}", entityType, entity.Id);
        }

        return results;
    }

    /// <summary>
    /// 验证实体列表
    /// </summary>
    public async Task<List<DataValidationResult>> ValidateEntitiesAsync<T>(List<T> entities) where T : BaseEntity
    {
        var allResults = new List<DataValidationResult>();

        foreach (var entity in entities)
        {
            var results = await ValidateEntityAsync(entity);
            allResults.AddRange(results);
        }

        return allResults;
    }

    /// <summary>
    /// 添加验证规则
    /// </summary>
    public async Task<DataValidationRule> AddValidationRuleAsync(DataValidationRule rule)
    {
        try
        {
            await _ruleRepository.AddAsync(rule);
            _logger.LogInformation("添加验证规则成功: {RuleName}", rule.RuleName);
            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加验证规则失败: {RuleName}", rule.RuleName);
            throw;
        }
    }

    /// <summary>
    /// 更新验证规则
    /// </summary>
    public async Task<DataValidationRule> UpdateValidationRuleAsync(DataValidationRule rule)
    {
        try
        {
            await _ruleRepository.UpdateAsync(rule);
            _logger.LogInformation("更新验证规则成功: {RuleName}", rule.RuleName);
            return rule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新验证规则失败: {RuleName}", rule.RuleName);
            throw;
        }
    }

    /// <summary>
    /// 删除验证规则
    /// </summary>
    public async Task<bool> DeleteValidationRuleAsync(long ruleId)
    {
        try
        {
            var result = await _ruleRepository.DeleteAsync(ruleId);
            _logger.LogInformation("删除验证规则成功: {RuleId}", ruleId);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除验证规则失败: {RuleId}", ruleId);
            throw;
        }
    }

    /// <summary>
    /// 获取实体类型的验证规则
    /// </summary>
    public async Task<List<DataValidationRule>> GetValidationRulesAsync(string entityType)
    {
        try
        {
            return await _ruleRepository.GetByConditionAsync(r => r.EntityType == entityType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验证规则失败: {EntityType}", entityType);
            return new List<DataValidationRule>();
        }
    }

    /// <summary>
    /// 获取验证结果
    /// </summary>
    public async Task<List<DataValidationResult>> GetValidationResultsAsync(
        string? entityType = null, long? entityId = null, 
        DateTime? startTime = null, DateTime? endTime = null)
    {
        try
        {
            Expression<Func<DataValidationResult, bool>> predicate = r => true;

            if (!string.IsNullOrEmpty(entityType))
                predicate = CombineExpressions(predicate, r => r.EntityType == entityType);

            if (entityId.HasValue)
                predicate = CombineExpressions(predicate, r => r.EntityId == entityId.Value);

            if (startTime.HasValue)
                predicate = CombineExpressions(predicate, r => r.ValidatedAt >= startTime.Value);

            if (endTime.HasValue)
                predicate = CombineExpressions(predicate, r => r.ValidatedAt <= endTime.Value);

            return await _resultRepository.GetByConditionAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取验证结果失败");
            return new List<DataValidationResult>();
        }
    }

    /// <summary>
    /// 获取数据质量报告
    /// </summary>
    public async Task<Dictionary<string, object>> GetDataQualityReportAsync(
        string? entityType = null, DateTime? startTime = null, DateTime? endTime = null)
    {
        var report = new Dictionary<string, object>();

        try
        {
            var results = await GetValidationResultsAsync(entityType, null, startTime, endTime);

            // 总体统计
            var totalValidations = results.Count;
            var passed = results.Count(r => r.Status == "Passed");
            var failed = results.Count(r => r.Status == "Failed");
            var warnings = results.Count(r => r.Status == "Warning");

            report["total_validations"] = totalValidations;
            report["passed"] = passed;
            report["failed"] = failed;
            report["warnings"] = warnings;
            report["pass_rate"] = totalValidations > 0 ? (double)passed / totalValidations * 100 : 100;

            // 按实体类型分组统计
            var byEntityType = results.GroupBy(r => r.EntityType)
                .ToDictionary(g => g.Key, g => new
                {
                    total = g.Count(),
                    passed = g.Count(r => r.Status == "Passed"),
                    failed = g.Count(r => r.Status == "Failed"),
                    warnings = g.Count(r => r.Status == "Warning")
                });

            report["by_entity_type"] = byEntityType;

            // 时间趋势
            if (results.Any())
            {
                var timeGroups = results.GroupBy(r => r.ValidatedAt.Date)
                    .OrderBy(g => g.Key)
                    .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => new
                    {
                        total = g.Count(),
                        passed = g.Count(r => r.Status == "Passed"),
                        failed = g.Count(r => r.Status == "Failed")
                    });

                report["time_trend"] = timeGroups;
            }

            report["generated_at"] = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成数据质量报告失败");
            report["error"] = ex.Message;
        }

        return report;
    }

    /// <summary>
    /// 运行数据质量检查作业
    /// </summary>
    public async Task RunDataQualityJobAsync(string? entityType = null)
    {
        try
        {
            _logger.LogInformation("开始运行数据质量检查作业: {EntityType}", entityType ?? "全部");

            // 这里可以实现定期的数据质量检查逻辑
            // 例如检查所有实体或特定类型的实体

            _logger.LogInformation("数据质量检查作业完成: {EntityType}", entityType ?? "全部");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "运行数据质量检查作业失败: {EntityType}", entityType);
            throw;
        }
    }

    /// <summary>
    /// 使用规则验证实体
    /// </summary>
    private async Task<DataValidationResult?> ValidateEntityWithRuleAsync<T>(T entity, DataValidationRule rule) where T : BaseEntity
    {
        try
        {
            var isValid = await EvaluateValidationRule(entity, rule);
            var status = isValid ? "Passed" : "Failed";

            if (status == "Failed" || rule.Severity == "Warning")
            {
                return new DataValidationResult
                {
                    RuleId = rule.Id,
                    EntityId = entity.Id,
                    EntityType = typeof(T).Name,
                    Status = status,
                    Message = isValid ? null : rule.ErrorMessage,
                    ValidatedAt = DateTime.UtcNow,
                    ValidatedValue = GetEntityFieldValue(entity, rule.FieldName)?.ToString()
                };
            }

            return null; // 验证通过且不需要记录
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证规则执行失败: {RuleName}", rule.RuleName);
            return new DataValidationResult
            {
                RuleId = rule.Id,
                EntityId = entity.Id,
                EntityType = typeof(T).Name,
                Status = "Failed",
                Message = $"验证规则执行失败: {ex.Message}",
                ValidatedAt = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// 评估验证规则
    /// </summary>
    private async Task<bool> EvaluateValidationRule<T>(T entity, DataValidationRule rule) where T : BaseEntity
    {
        // 简单的验证规则评估实现
        var fieldValue = GetEntityFieldValue(entity, rule.FieldName);

        return rule.ValidationType switch
        {
            "NotNull" => fieldValue != null,
            "NotEmpty" => fieldValue != null && !string.IsNullOrEmpty(fieldValue.ToString()),
            "Range" => EvaluateRangeValidation(fieldValue, rule.ValidationExpression),
            "Regex" => EvaluateRegexValidation(fieldValue?.ToString(), rule.ValidationExpression),
            "Custom" => await EvaluateCustomValidation(entity, rule.ValidationExpression),
            _ => true
        };
    }

    /// <summary>
    /// 获取实体字段值
    /// </summary>
    private object? GetEntityFieldValue<T>(T entity, string fieldName)
    {
        var property = typeof(T).GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);
        return property?.GetValue(entity);
    }

    /// <summary>
    /// 评估范围验证
    /// </summary>
    private bool EvaluateRangeValidation(object? value, string expression)
    {
        if (value == null) return false;

        try
        {
            var range = JsonSerializer.Deserialize<Dictionary<string, double>>(expression);
            if (range == null) return false;

            var numValue = Convert.ToDouble(value);
            var min = range.ContainsKey("min") ? range["min"] : double.MinValue;
            var max = range.ContainsKey("max") ? range["max"] : double.MaxValue;

            return numValue >= min && numValue <= max;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 评估正则表达式验证
    /// </summary>
    private bool EvaluateRegexValidation(string? value, string pattern)
    {
        if (string.IsNullOrEmpty(value)) return false;

        try
        {
            return System.Text.RegularExpressions.Regex.IsMatch(value, pattern);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 评估自定义验证
    /// </summary>
    private async Task<bool> EvaluateCustomValidation<T>(T entity, string expression)
    {
        // 这里可以实现更复杂的自定义验证逻辑
        await Task.CompletedTask;
        return true;
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
}

/// <summary>
/// 表达式访问器
/// </summary>
public class ReplaceExpressionVisitor : ExpressionVisitor
{
    private readonly Expression _oldValue;
    private readonly Expression _newValue;

    public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
    {
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public override Expression? Visit(Expression? node)
    {
        return node == _oldValue ? _newValue : base.Visit(node);
    }
} 