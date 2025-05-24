using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Api.Models;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 编码生成管理控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/code-generation")]
[ApiVersion("2.0")]
[Authorize]
public class CodeGenerationController : ControllerBase
{
    private readonly ICodeGenerationService _codeGenerationService;
    private readonly IAuditService _auditService;
    private readonly ILogger<CodeGenerationController> _logger;

    public CodeGenerationController(
        ICodeGenerationService codeGenerationService,
        IAuditService auditService,
        ILogger<CodeGenerationController> logger)
    {
        _codeGenerationService = codeGenerationService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// 生成批次编码
    /// </summary>
    /// <param name="productType">产品类型</param>
    /// <param name="customPrefix">自定义前缀</param>
    /// <returns>批次编码</returns>
    [HttpPost("batch-code")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<ActionResult<ApiResponse<string>>> GenerateBatchCode([FromQuery] string productType, [FromQuery] string? customPrefix = null)
    {
        try
        {
            var batchCode = await _codeGenerationService.GenerateBatchCodeAsync(productType, customPrefix);
            
            await _auditService.LogAsync("GenerateBatchCode", "CodeGeneration", 0,
                $"生成批次编码: {batchCode}, 产品类型: {productType}");

            return Ok(ApiResponse<string>.Ok(batchCode, "批次编码生成成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成批次编码失败: ProductType={ProductType}", productType);
            return StatusCode(500, ApiResponse<string>.Fail("生成批次编码失败", "GENERATE_ERROR"));
        }
    }

    /// <summary>
    /// 生成序列号
    /// </summary>
    /// <param name="batchNumber">批次号</param>
    /// <param name="productType">产品类型</param>
    /// <param name="count">生成数量</param>
    /// <returns>序列号列表</returns>
    [HttpPost("serial-numbers")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), 200)]
    public async Task<ActionResult<ApiResponse<List<string>>>> GenerateSerialNumbers(
        [FromQuery] string batchNumber,
        [FromQuery] string productType,
        [FromQuery] int count)
    {
        try
        {
            if (count <= 0 || count > 10000)
            {
                return BadRequest(ApiResponse<List<string>>.Fail("生成数量必须在1-10000之间", "INVALID_COUNT"));
            }

            var serialNumbers = await _codeGenerationService.GenerateSerialNumbersAsync(batchNumber, productType, count);
            
            await _auditService.LogAsync("GenerateSerialNumbers", "CodeGeneration", 0,
                $"生成序列号: 批次={batchNumber}, 数量={count}");

            return Ok(ApiResponse<List<string>>.Ok(serialNumbers, $"成功生成{serialNumbers.Count}个序列号"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成序列号失败: BatchNumber={BatchNumber}, Count={Count}", batchNumber, count);
            return StatusCode(500, ApiResponse<List<string>>.Fail("生成序列号失败", "GENERATE_ERROR"));
        }
    }

    /// <summary>
    /// 生成条形码
    /// </summary>
    /// <param name="serialNumber">序列号</param>
    /// <returns>条形码</returns>
    [HttpPost("barcode")]
    [ProducesResponseType(typeof(ApiResponse<string>), 200)]
    public async Task<ActionResult<ApiResponse<string>>> GenerateBarcode([FromQuery] string serialNumber)
    {
        try
        {
            var barcode = await _codeGenerationService.GenerateBarcodeAsync(serialNumber);
            
            await _auditService.LogAsync("GenerateBarcode", "CodeGeneration", 0,
                $"生成条形码: 序列号={serialNumber}, 条形码={barcode}");

            return Ok(ApiResponse<string>.Ok(barcode, "条形码生成成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成条形码失败: SerialNumber={SerialNumber}", serialNumber);
            return StatusCode(500, ApiResponse<string>.Fail("生成条形码失败", "GENERATE_ERROR"));
        }
    }

    /// <summary>
    /// 标记编码已使用
    /// </summary>
    /// <param name="request">标记请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("mark-used")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    public async Task<ActionResult<ApiResponse>> MarkCodeUsed([FromBody] MarkCodeUsedRequest request)
    {
        try
        {
            var success = await _codeGenerationService.MarkCodeAsUsedAsync(request.Code, request.EntityType, request.EntityId);
            
            if (success)
            {
                await _auditService.LogAsync("MarkCodeUsed", "CodeGeneration", 0,
                    $"标记编码已使用: {request.Code}");
                return Ok(ApiResponse.Ok("编码标记成功"));
            }

            return BadRequest(ApiResponse.Fail("编码标记失败", "MARK_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "标记编码已使用失败: Code={Code}", request.Code);
            return StatusCode(500, ApiResponse.Fail("标记编码已使用失败", "MARK_ERROR"));
        }
    }

    /// <summary>
    /// 获取编码生成历史
    /// </summary>
    /// <param name="codeType">编码类型</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>生成历史列表</returns>
    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<CodeGenerationHistory>>), 200)]
    public async Task<ActionResult<ApiResponse<PagedResult<CodeGenerationHistory>>>> GetGenerationHistory(
        [FromQuery] string codeType,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var items = await _codeGenerationService.GetGenerationHistoryAsync(codeType, startDate, endDate);
            
            // 简单的内存分页
            var total = items.Count;
            var pagedItems = items.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            var result = new PagedResult<CodeGenerationHistory>
            {
                Items = pagedItems,
                Total = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<CodeGenerationHistory>>.Ok(result, "获取生成历史成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取编码生成历史失败: CodeType={CodeType}", codeType);
            return StatusCode(500, ApiResponse<PagedResult<CodeGenerationHistory>>.Fail("获取编码生成历史失败", "QUERY_ERROR"));
        }
    }

    /// <summary>
    /// 验证编码
    /// </summary>
    /// <param name="code">编码</param>
    /// <returns>验证结果</returns>
    [HttpGet("validate/{code}")]
    [ProducesResponseType(typeof(ApiResponse<CodeValidationResult>), 200)]
    public async Task<ActionResult<ApiResponse<CodeValidationResult>>> ValidateCode(string code)
    {
        try
        {
            var (isValid, errorMessage) = await _codeGenerationService.ValidateCodeAsync(code, "Auto");
            var result = new CodeValidationResult
            {
                Code = code,
                IsValid = isValid,
                ErrorMessage = errorMessage
            };
            return Ok(ApiResponse<CodeValidationResult>.Ok(result, "编码验证完成"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "编码验证失败: Code={Code}", code);
            return StatusCode(500, ApiResponse<CodeValidationResult>.Fail("编码验证失败", "VALIDATE_ERROR"));
        }
    }
}

#region 请求和响应模型

/// <summary>
/// 标记编码已使用请求
/// </summary>
public class MarkCodeUsedRequest
{
    /// <summary>
    /// 编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 关联实体类型
    /// </summary>
    public string? EntityType { get; set; }

    /// <summary>
    /// 关联实体ID
    /// </summary>
    public long? EntityId { get; set; }
}

/// <summary>
/// 编码验证结果
/// </summary>
public class CodeValidationResult
{
    /// <summary>
    /// 编码
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// 是否有效
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// 错误信息
    /// </summary>
    public string? ErrorMessage { get; set; }
}

#endregion 