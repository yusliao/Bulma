using BatteryPackingMES.Core.Entities;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 编码生成服务接口
/// </summary>
public interface ICodeGenerationService
{
    /// <summary>
    /// 生成批次号
    /// </summary>
    /// <param name="productType">产品类型</param>
    /// <param name="customPrefix">自定义前缀</param>
    /// <returns>批次号</returns>
    Task<string> GenerateBatchCodeAsync(string productType, string? customPrefix = null);

    /// <summary>
    /// 生成序列号
    /// </summary>
    /// <param name="batchNumber">批次号</param>
    /// <param name="productType">产品类型</param>
    /// <param name="count">生成数量</param>
    /// <returns>序列号列表</returns>
    Task<List<string>> GenerateSerialNumbersAsync(string batchNumber, string productType, int count = 1);

    /// <summary>
    /// 生成条码
    /// </summary>
    /// <param name="serialNumber">序列号</param>
    /// <param name="codeType">条码类型</param>
    /// <returns>条码</returns>
    Task<string> GenerateBarcodeAsync(string serialNumber, string codeType = "Code128");

    /// <summary>
    /// 验证编码格式
    /// </summary>
    /// <param name="code">编码</param>
    /// <param name="codeType">编码类型</param>
    /// <returns>验证结果</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateCodeAsync(string code, string codeType);

    /// <summary>
    /// 检查编码是否重复
    /// </summary>
    /// <param name="code">编码</param>
    /// <param name="codeType">编码类型</param>
    /// <returns>是否重复</returns>
    Task<bool> IsCodeDuplicateAsync(string code, string codeType);

    /// <summary>
    /// 获取编码生成规则
    /// </summary>
    /// <param name="codeType">编码类型</param>
    /// <param name="productType">产品类型</param>
    /// <returns>生成规则</returns>
    Task<CodeGenerationRule?> GetGenerationRuleAsync(string codeType, string productType);

    /// <summary>
    /// 创建或更新编码生成规则
    /// </summary>
    /// <param name="rule">生成规则</param>
    /// <returns>规则ID</returns>
    Task<long> SaveGenerationRuleAsync(CodeGenerationRule rule);

    /// <summary>
    /// 批量预生成编码
    /// </summary>
    /// <param name="codeType">编码类型</param>
    /// <param name="productType">产品类型</param>
    /// <param name="count">生成数量</param>
    /// <returns>预生成的编码列表</returns>
    Task<List<string>> PreGenerateCodesAsync(string codeType, string productType, int count);

    /// <summary>
    /// 标记编码已使用
    /// </summary>
    /// <param name="code">编码</param>
    /// <param name="entityType">关联实体类型</param>
    /// <param name="entityId">关联实体ID</param>
    /// <returns>操作结果</returns>
    Task<bool> MarkCodeAsUsedAsync(string code, string? entityType = null, long? entityId = null);

    /// <summary>
    /// 获取编码生成历史
    /// </summary>
    /// <param name="codeType">编码类型</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>生成历史记录</returns>
    Task<List<CodeGenerationHistory>> GetGenerationHistoryAsync(string codeType, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 解析编码信息
    /// </summary>
    /// <param name="code">编码</param>
    /// <returns>编码信息</returns>
    Task<CodeInfo> ParseCodeAsync(string code);
}

/// <summary>
/// 编码信息
/// </summary>
public class CodeInfo
{
    /// <summary>
    /// 编码类型
    /// </summary>
    public string CodeType { get; set; } = string.Empty;

    /// <summary>
    /// 产品类型
    /// </summary>
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// 生产日期
    /// </summary>
    public DateTime? ProductionDate { get; set; }

    /// <summary>
    /// 序号
    /// </summary>
    public int? Sequence { get; set; }

    /// <summary>
    /// 前缀
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// 后缀
    /// </summary>
    public string? Suffix { get; set; }

    /// <summary>
    /// 原始编码
    /// </summary>
    public string OriginalCode { get; set; } = string.Empty;

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 解析错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
} 