using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 编码生成服务实现
/// </summary>
public class CodeGenerationService : ICodeGenerationService
{
    private readonly IRepository<CodeGenerationRule> _ruleRepository;
    private readonly IRepository<CodeGenerationHistory> _historyRepository;
    private readonly IRepository<ProductionBatch> _batchRepository;
    private readonly IRepository<ProductItem> _productItemRepository;
    private readonly ILogger<CodeGenerationService> _logger;
    private readonly object _lockObject = new();

    public CodeGenerationService(
        IRepository<CodeGenerationRule> ruleRepository,
        IRepository<CodeGenerationHistory> historyRepository,
        IRepository<ProductionBatch> batchRepository,
        IRepository<ProductItem> productItemRepository,
        ILogger<CodeGenerationService> logger)
    {
        _ruleRepository = ruleRepository;
        _historyRepository = historyRepository;
        _batchRepository = batchRepository;
        _productItemRepository = productItemRepository;
        _logger = logger;
    }

    /// <summary>
    /// 生成批次号
    /// </summary>
    public async Task<string> GenerateBatchCodeAsync(string productType, string? customPrefix = null)
    {
        lock (_lockObject)
        {
            return GenerateBatchCodeInternalAsync(productType, customPrefix).Result;
        }
    }

    private async Task<string> GenerateBatchCodeInternalAsync(string productType, string? customPrefix = null)
    {
        try
        {
            var rule = await GetGenerationRuleAsync("BatchCode", productType);
            if (rule == null)
            {
                // 使用默认规则
                rule = CreateDefaultBatchRule(productType);
                await SaveGenerationRuleAsync(rule);
            }

            var generatedCode = await GenerateCodeByRule(rule, customPrefix);
            
            // 记录生成历史
            await RecordGenerationHistory(rule.Id, generatedCode, "BatchCode", "ProductionBatch");
            
            _logger.LogInformation("生成批次号成功: {BatchCode} for {ProductType}", generatedCode, productType);
            return generatedCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成批次号失败: ProductType={ProductType}", productType);
            throw;
        }
    }

    /// <summary>
    /// 生成序列号
    /// </summary>
    public async Task<List<string>> GenerateSerialNumbersAsync(string batchNumber, string productType, int count = 1)
    {
        if (count <= 0 || count > 10000)
            throw new ArgumentException("生成数量必须在1-10000之间", nameof(count));

        lock (_lockObject)
        {
            return GenerateSerialNumbersInternalAsync(batchNumber, productType, count).Result;
        }
    }

    private async Task<List<string>> GenerateSerialNumbersInternalAsync(string batchNumber, string productType, int count)
    {
        try
        {
            var rule = await GetGenerationRuleAsync("SerialNumber", productType);
            if (rule == null)
            {
                rule = CreateDefaultSerialRule(productType);
                await SaveGenerationRuleAsync(rule);
            }

            var serialNumbers = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var serialNumber = await GenerateSerialNumberByRule(rule, batchNumber);
                serialNumbers.Add(serialNumber);
                
                // 记录生成历史
                await RecordGenerationHistory(rule.Id, serialNumber, "SerialNumber", "ProductItem");
            }

            _logger.LogInformation("生成序列号成功: 数量={Count}, 批次={BatchNumber}", count, batchNumber);
            return serialNumbers;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成序列号失败: BatchNumber={BatchNumber}, Count={Count}", batchNumber, count);
            throw;
        }
    }

    /// <summary>
    /// 生成条码
    /// </summary>
    public async Task<string> GenerateBarcodeAsync(string serialNumber, string codeType = "Code128")
    {
        try
        {
            // 对于大多数情况，条码就是序列号本身
            // 也可以根据需要添加校验码或特殊格式
            var barcode = codeType.ToUpper() switch
            {
                "CODE128" => GenerateCode128Barcode(serialNumber),
                "QR" => GenerateQRCodeData(serialNumber),
                "DATAMATRIX" => GenerateDataMatrixCode(serialNumber),
                _ => serialNumber
            };

            _logger.LogDebug("生成条码: {Barcode} from {SerialNumber}", barcode, serialNumber);
            return barcode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成条码失败: SerialNumber={SerialNumber}", serialNumber);
            throw;
        }
    }

    /// <summary>
    /// 验证编码格式
    /// </summary>
    public async Task<(bool IsValid, string? ErrorMessage)> ValidateCodeAsync(string code, string codeType)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(code))
                return (false, "编码不能为空");

            // 根据编码类型获取对应的验证规则
            var rules = await _ruleRepository.GetByConditionAsync(r => r.CodeType == codeType && r.IsEnabled);
            
            foreach (var rule in rules)
            {
                if (!string.IsNullOrEmpty(rule.ValidationPattern))
                {
                    if (!Regex.IsMatch(code, rule.ValidationPattern))
                        return (false, $"编码格式不符合规则: {rule.RuleName}");
                }

                // 验证模板格式
                if (!ValidateCodeTemplate(code, rule.Template))
                    return (false, $"编码不符合模板: {rule.Template}");
            }

            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证编码失败: Code={Code}", code);
            return (false, $"验证失败: {ex.Message}");
        }
    }

    /// <summary>
    /// 检查编码是否重复
    /// </summary>
    public async Task<bool> IsCodeDuplicateAsync(string code, string codeType)
    {
        try
        {
            // 检查历史记录中是否存在
            var historyExists = await _historyRepository.ExistsAsync(h => h.GeneratedCode == code && h.CodeType == codeType);
            if (historyExists) return true;

            // 根据编码类型检查具体表
            return codeType switch
            {
                "BatchCode" => await _batchRepository.ExistsAsync(b => b.BatchNumber == code),
                "SerialNumber" => await _productItemRepository.ExistsAsync(p => p.SerialNumber == code),
                "Barcode" => await _productItemRepository.ExistsAsync(p => p.Barcode == code),
                _ => false
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查编码重复失败: Code={Code}", code);
            return true; // 出错时返回true，避免生成重复编码
        }
    }

    /// <summary>
    /// 获取编码生成规则
    /// </summary>
    public async Task<CodeGenerationRule?> GetGenerationRuleAsync(string codeType, string productType)
    {
        try
        {
            return await _ruleRepository.GetFirstAsync(r => 
                r.CodeType == codeType && 
                r.ProductType == productType && 
                r.IsEnabled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取生成规则失败: CodeType={CodeType}, ProductType={ProductType}", codeType, productType);
            return null;
        }
    }

    /// <summary>
    /// 保存编码生成规则
    /// </summary>
    public async Task<long> SaveGenerationRuleAsync(CodeGenerationRule rule)
    {
        try
        {
            if (rule.Id > 0)
            {
                await _ruleRepository.UpdateAsync(rule);
                return rule.Id;
            }
            else
            {
                return await _ruleRepository.AddAsync(rule);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存生成规则失败: {RuleName}", rule.RuleName);
            throw;
        }
    }

    /// <summary>
    /// 批量预生成编码
    /// </summary>
    public async Task<List<string>> PreGenerateCodesAsync(string codeType, string productType, int count)
    {
        try
        {
            var rule = await GetGenerationRuleAsync(codeType, productType);
            if (rule == null)
                throw new InvalidOperationException($"未找到编码生成规则: {codeType}-{productType}");

            var codes = new List<string>();
            for (int i = 0; i < count; i++)
            {
                var code = await GenerateCodeByRule(rule);
                codes.Add(code);
                
                // 记录为预生成，未使用状态
                await RecordGenerationHistory(rule.Id, code, codeType, null, isUsed: false);
            }

            return codes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预生成编码失败: CodeType={CodeType}, Count={Count}", codeType, count);
            throw;
        }
    }

    /// <summary>
    /// 标记编码已使用
    /// </summary>
    public async Task<bool> MarkCodeAsUsedAsync(string code, string? entityType = null, long? entityId = null)
    {
        try
        {
            var history = await _historyRepository.GetFirstAsync(h => h.GeneratedCode == code && !h.IsUsed);
            if (history != null)
            {
                history.IsUsed = true;
                history.UsedAt = DateTime.UtcNow;
                history.EntityType = entityType;
                history.EntityId = entityId;
                
                await _historyRepository.UpdateAsync(history);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记编码已使用失败: Code={Code}", code);
            return false;
        }
    }

    /// <summary>
    /// 获取编码生成历史
    /// </summary>
    public async Task<List<CodeGenerationHistory>> GetGenerationHistoryAsync(string codeType, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var allHistory = await _historyRepository.GetByConditionAsync(h => h.CodeType == codeType);
            
            var filteredHistory = allHistory.AsEnumerable();
            
            if (startDate.HasValue)
                filteredHistory = filteredHistory.Where(h => h.GeneratedAt >= startDate.Value);
                
            if (endDate.HasValue)
                filteredHistory = filteredHistory.Where(h => h.GeneratedAt <= endDate.Value);

            return filteredHistory.OrderByDescending(h => h.GeneratedAt).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取生成历史失败: CodeType={CodeType}", codeType);
            throw;
        }
    }

    /// <summary>
    /// 解析编码信息
    /// </summary>
    public async Task<CodeInfo> ParseCodeAsync(string code)
    {
        try
        {
            var codeInfo = new CodeInfo { OriginalCode = code };

            // 尝试匹配不同的编码规则
            var rules = await _ruleRepository.GetByConditionAsync(r => r.IsEnabled);
            
            foreach (var rule in rules)
            {
                if (TryParseCodeWithRule(code, rule, out var parsedInfo))
                {
                    codeInfo = parsedInfo;
                    codeInfo.IsValid = true;
                    break;
                }
            }

            if (!codeInfo.IsValid)
            {
                codeInfo.ErrorMessage = "无法解析编码格式";
            }

            return codeInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解析编码失败: Code={Code}", code);
            return new CodeInfo 
            { 
                OriginalCode = code, 
                IsValid = false, 
                ErrorMessage = ex.Message 
            };
        }
    }

    #region 私有辅助方法

    /// <summary>
    /// 根据规则生成编码
    /// </summary>
    private async Task<string> GenerateCodeByRule(CodeGenerationRule rule, string? customPrefix = null)
    {
        // 检查是否需要重置序号
        await CheckAndResetSequence(rule);

        // 增加序号
        rule.CurrentSequence++;
        rule.LastGeneratedDate = DateTime.UtcNow;

        // 生成编码
        var code = BuildCodeFromTemplate(rule, customPrefix);

        // 确保唯一性
        var isDuplicate = await IsCodeDuplicateAsync(code, rule.CodeType);
        if (isDuplicate)
        {
            // 递归重试
            return await GenerateCodeByRule(rule, customPrefix);
        }

        // 更新规则
        await _ruleRepository.UpdateAsync(rule);

        return code;
    }

    /// <summary>
    /// 根据规则生成序列号
    /// </summary>
    private async Task<string> GenerateSerialNumberByRule(CodeGenerationRule rule, string batchNumber)
    {
        // 对于序列号，使用批次号+序号的格式
        var serialTemplate = $"{batchNumber}-{{Sequence}}";
        
        // 检查当前批次的最大序号
        var existingItems = await _productItemRepository.GetByConditionAsync(p => p.BatchNumber == batchNumber);
        var maxSequence = 0;
        
        foreach (var item in existingItems)
        {
            if (item.SerialNumber.Contains("-"))
            {
                var parts = item.SerialNumber.Split('-');
                if (parts.Length >= 2 && int.TryParse(parts[^1], out var seq))
                {
                    maxSequence = Math.Max(maxSequence, seq);
                }
            }
        }

        var sequence = maxSequence + 1;
        var serialNumber = $"{batchNumber}-{sequence.ToString($"D{rule.SequenceLength}")}";

        return serialNumber;
    }

    /// <summary>
    /// 检查并重置序号
    /// </summary>
    private async Task CheckAndResetSequence(CodeGenerationRule rule)
    {
        var now = DateTime.UtcNow;
        var lastGenerated = rule.LastGeneratedDate ?? DateTime.MinValue;

        var shouldReset = false;
        
        if (rule.ResetDaily && now.Date > lastGenerated.Date)
            shouldReset = true;
        else if (rule.ResetMonthly && (now.Year > lastGenerated.Year || now.Month > lastGenerated.Month))
            shouldReset = true;
        else if (rule.ResetYearly && now.Year > lastGenerated.Year)
            shouldReset = true;

        if (shouldReset)
        {
            rule.CurrentSequence = rule.StartNumber - 1;
            _logger.LogInformation("重置序号: Rule={RuleName}, NewSequence={Sequence}", rule.RuleName, rule.CurrentSequence);
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 根据模板构建编码
    /// </summary>
    private string BuildCodeFromTemplate(CodeGenerationRule rule, string? customPrefix = null)
    {
        var template = rule.Template;
        var now = DateTime.UtcNow;

        // 替换模板变量
        template = template.Replace("{Prefix}", customPrefix ?? rule.Prefix ?? "");
        template = template.Replace("{Suffix}", rule.Suffix ?? "");
        template = template.Replace("{Sequence}", rule.CurrentSequence.ToString($"D{rule.SequenceLength}"));
        
        // 处理日期格式
        if (!string.IsNullOrEmpty(rule.DateFormat))
        {
            template = template.Replace("{Date}", now.ToString(rule.DateFormat));
        }

        // 其他变量
        template = template.Replace("{Year}", now.Year.ToString());
        template = template.Replace("{Month}", now.Month.ToString("D2"));
        template = template.Replace("{Day}", now.Day.ToString("D2"));
        template = template.Replace("{ProductType}", rule.ProductType);

        return template;
    }

    /// <summary>
    /// 验证编码模板
    /// </summary>
    private bool ValidateCodeTemplate(string code, string template)
    {
        // 简化的模板验证逻辑
        // 实际应用中可以更复杂
        return !string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(template);
    }

    /// <summary>
    /// 尝试用规则解析编码
    /// </summary>
    private bool TryParseCodeWithRule(string code, CodeGenerationRule rule, out CodeInfo codeInfo)
    {
        codeInfo = new CodeInfo { OriginalCode = code };

        try
        {
            // 简化的解析逻辑
            if (!string.IsNullOrEmpty(rule.ValidationPattern))
            {
                var match = Regex.Match(code, rule.ValidationPattern);
                if (match.Success)
                {
                    codeInfo.CodeType = rule.CodeType;
                    codeInfo.ProductType = rule.ProductType;
                    codeInfo.Prefix = rule.Prefix;
                    codeInfo.Suffix = rule.Suffix;
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 生成Code128条码
    /// </summary>
    private string GenerateCode128Barcode(string data)
    {
        // 实际应用中可以使用专门的条码生成库
        return data;
    }

    /// <summary>
    /// 生成QR码数据
    /// </summary>
    private string GenerateQRCodeData(string data)
    {
        // 可以包含更多信息的JSON格式
        var qrData = new
        {
            SerialNumber = data,
            Timestamp = DateTime.UtcNow,
            System = "BatteryPackingMES"
        };
        return JsonSerializer.Serialize(qrData);
    }

    /// <summary>
    /// 生成DataMatrix码
    /// </summary>
    private string GenerateDataMatrixCode(string data)
    {
        return data;
    }

    /// <summary>
    /// 创建默认批次规则
    /// </summary>
    private CodeGenerationRule CreateDefaultBatchRule(string productType)
    {
        var prefix = productType.ToUpper() switch
        {
            "CELL" => "CELL",
            "MODULE" => "MOD",
            "PACK" => "PACK",
            "PALLET" => "PLT",
            _ => "BATCH"
        };

        return new CodeGenerationRule
        {
            RuleName = $"{productType}批次号规则",
            CodeType = "BatchCode",
            ProductType = productType,
            Prefix = prefix,
            DateFormat = "yyyyMMdd",
            SequenceLength = 3,
            StartNumber = 1,
            ResetDaily = true,
            Template = "{Prefix}{Date}{Sequence}",
            ValidationPattern = $@"^{prefix}\d{{8}}\d{{3}}$",
            IsEnabled = true,
            Description = $"{productType}产品批次号自动生成规则"
        };
    }

    /// <summary>
    /// 创建默认序列号规则
    /// </summary>
    private CodeGenerationRule CreateDefaultSerialRule(string productType)
    {
        return new CodeGenerationRule
        {
            RuleName = $"{productType}序列号规则",
            CodeType = "SerialNumber",
            ProductType = productType,
            SequenceLength = 6,
            StartNumber = 1,
            ResetDaily = false,
            Template = "{BatchNumber}-{Sequence}",
            ValidationPattern = @"^.+-\d{6}$",
            IsEnabled = true,
            Description = $"{productType}产品序列号自动生成规则"
        };
    }

    /// <summary>
    /// 记录生成历史
    /// </summary>
    private async Task RecordGenerationHistory(long ruleId, string code, string codeType, string? entityType = null, bool isUsed = true)
    {
        var history = new CodeGenerationHistory
        {
            RuleId = ruleId,
            GeneratedCode = code,
            CodeType = codeType,
            EntityType = entityType,
            GeneratedAt = DateTime.UtcNow,
            GenerationSource = "Auto",
            IsUsed = isUsed
        };

        await _historyRepository.AddAsync(history);
    }

    #endregion
} 