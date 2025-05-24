using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Api.Models;
using BatteryPackingMES.Api.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 工艺参数管理控制器 V2.0
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/process-parameters")]
[Authorize]
[Produces("application/json")]
public class ProcessParameterController : ControllerBase
{
    private readonly IRepository<ProductionParameter> _parameterRepository;
    private readonly IRepository<Process> _processRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProcessParameterController> _logger;

    public ProcessParameterController(
        IRepository<ProductionParameter> parameterRepository,
        IRepository<Process> processRepository,
        IAuditService auditService,
        ILogger<ProcessParameterController> logger)
    {
        _parameterRepository = parameterRepository;
        _processRepository = processRepository;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询工艺参数
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="processId">工序ID筛选</param>
    /// <param name="parameterName">参数名称筛选</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>分页的工艺参数列表</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductionParameterDto>>), 200)]
    public async Task<IActionResult> GetPagedParameters(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] long? processId = null,
        [FromQuery] string? parameterName = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        try
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var (parameters, total) = await _parameterRepository.GetPagedAsync(
                pageIndex,
                pageSize,
                p => (processId == null || p.ProcessId == processId) &&
                     (string.IsNullOrEmpty(parameterName) || p.ParameterName.Contains(parameterName)) &&
                     (startTime == null || p.CollectTime >= startTime) &&
                     (endTime == null || p.CollectTime <= endTime));

            var parameterDtos = parameters.Select(p => new ProductionParameterDto
            {
                Id = p.Id,
                ProcessId = p.ProcessId,
                ParameterName = p.ParameterName,
                ParameterValue = decimal.TryParse(p.ParameterValue, out var val) ? val : 0,
                ParameterUnit = p.Unit,
                StandardValue = null, // 需要从别的地方获取标准值
                UpperLimit = p.UpperLimit,
                LowerLimit = p.LowerLimit,
                RecordTime = p.CollectTime,
                EquipmentId = null, // 需要从设备编号转换
                BatchNumber = p.BatchNumber,
                ProductBarcode = null, // 生产参数实体中没有这个字段
                IsQualified = p.IsQualified,
                CreatedTime = p.CreatedTime
            }).ToList();

            var result = new PagedResult<ProductionParameterDto>
            {
                Items = parameterDtos,
                Total = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(ApiResponse.OK(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询工艺参数失败");
            return BadRequest(ApiResponse.Fail("查询工艺参数失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 批量记录工艺参数
    /// </summary>
    /// <param name="request">参数记录请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("batch-record")]
    [ProducesResponseType(typeof(ApiResponse<BatchRecordResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> BatchRecordParameters([FromBody] BatchRecordParametersRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            var results = new List<ParameterRecordResultDto>();
            var successCount = 0;
            var failureCount = 0;

            foreach (var parameterRequest in request.Parameters)
            {
                try
                {
                    var parameter = new ProductionParameter
                    {
                        ProcessId = parameterRequest.ProcessId,
                        ParameterName = parameterRequest.ParameterName,
                        ParameterValue = parameterRequest.ParameterValue.ToString(),
                        Unit = parameterRequest.ParameterUnit,
                        UpperLimit = parameterRequest.UpperLimit,
                        LowerLimit = parameterRequest.LowerLimit,
                        CollectTime = parameterRequest.RecordTime ?? DateTime.UtcNow,
                        EquipmentCode = parameterRequest.EquipmentId?.ToString(),
                        BatchNumber = parameterRequest.BatchNumber ?? "",
                        IsQualified = IsParameterQualified(parameterRequest.ParameterValue, 
                            parameterRequest.LowerLimit, parameterRequest.UpperLimit),
                        CreatedBy = User.GetUserIdAsLong()
                    };

                    var parameterId = await _parameterRepository.AddAsync(parameter);

                    results.Add(new ParameterRecordResultDto
                    {
                        ParameterName = parameterRequest.ParameterName,
                        Success = true,
                        ParameterId = parameterId
                    });

                    successCount++;
                }
                catch (Exception ex)
                {
                    results.Add(new ParameterRecordResultDto
                    {
                        ParameterName = parameterRequest.ParameterName,
                        Success = false,
                        ErrorMessage = ex.Message
                    });

                    failureCount++;
                }
            }

            await _auditService.LogAsync("BatchRecordParameters", "ProductionParameter", 0,
                $"批量记录工艺参数: 成功 {successCount} 条，失败 {failureCount} 条");

            _logger.LogInformation("用户 {UserId} 批量记录工艺参数: 成功 {SuccessCount} 条，失败 {FailureCount} 条",
                User.GetUserIdAsLong(), successCount, failureCount);

            var response = new BatchRecordResultDto
            {
                Results = results,
                SuccessCount = successCount,
                FailureCount = failureCount,
                TotalCount = request.Parameters.Count
            };

            return Ok(ApiResponse.OK(response, "批量记录完成"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量记录工艺参数失败");
            return BadRequest(ApiResponse.Fail("批量记录失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取工艺参数统计分析
    /// </summary>
    /// <param name="processId">工序ID</param>
    /// <param name="parameterName">参数名称</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <returns>统计分析结果</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<ParameterStatisticsDto>), 200)]
    public async Task<IActionResult> GetParameterStatistics(
        [FromQuery, Required] long processId,
        [FromQuery, Required] string parameterName,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null)
    {
        try
        {
            var queryStartTime = startTime ?? DateTime.Now.AddDays(-7);
            var queryEndTime = endTime ?? DateTime.Now;

            var parameters = await _parameterRepository.GetListAsync(
                p => p.ProcessId == processId &&
                     p.ParameterName == parameterName &&
                     p.CollectTime >= queryStartTime &&
                     p.CollectTime <= queryEndTime);

            if (!parameters.Any())
            {
                return Ok(ApiResponse.OK(new ParameterStatisticsDto
                {
                    ProcessId = processId,
                    ParameterName = parameterName,
                    TotalCount = 0
                }));
            }

            var values = parameters.Select(p => decimal.TryParse(p.ParameterValue, out var val) ? val : 0).ToList();
            var qualifiedCount = parameters.Count(p => p.IsQualified);

            var statistics = new ParameterStatisticsDto
            {
                ProcessId = processId,
                ParameterName = parameterName,
                TotalCount = parameters.Count,
                QualifiedCount = qualifiedCount,
                UnqualifiedCount = parameters.Count - qualifiedCount,
                QualificationRate = (decimal)qualifiedCount / parameters.Count * 100,
                MinValue = values.Any() ? values.Min() : 0,
                MaxValue = values.Any() ? values.Max() : 0,
                AverageValue = values.Any() ? values.Average() : 0,
                StandardDeviation = CalculateStandardDeviation(values),
                ParameterUnit = parameters.First().Unit,
                StandardValue = null, // 需要从配置获取
                UpperLimit = parameters.First().UpperLimit,
                LowerLimit = parameters.First().LowerLimit,
                QueryStartTime = queryStartTime,
                QueryEndTime = queryEndTime,
                TrendData = GenerateTrendData(parameters)
            };

            return Ok(ApiResponse.OK(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工艺参数统计分析失败");
            return BadRequest(ApiResponse.Fail("获取统计分析失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取工艺参数实时数据
    /// </summary>
    /// <param name="processId">工序ID</param>
    /// <param name="parameterNames">参数名称列表</param>
    /// <param name="minutes">获取最近多少分钟的数据</param>
    /// <returns>实时数据</returns>
    [HttpGet("realtime")]
    [ProducesResponseType(typeof(ApiResponse<List<RealtimeParameterDto>>), 200)]
    public async Task<IActionResult> GetRealtimeParameters(
        [FromQuery, Required] long processId,
        [FromQuery] string[]? parameterNames = null,
        [FromQuery] int minutes = 30)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-minutes);

            var parameters = await _parameterRepository.GetListAsync(
                p => p.ProcessId == processId &&
                     p.CollectTime >= cutoffTime &&
                     (parameterNames == null || parameterNames.Length == 0 || parameterNames.Contains(p.ParameterName)));

            var realtimeData = parameters
                .GroupBy(p => p.ParameterName)
                .Select(g => new RealtimeParameterDto
                {
                    ParameterName = g.Key,
                    LatestValue = decimal.TryParse(g.OrderByDescending(p => p.CollectTime).First().ParameterValue, out var val) ? val : 0,
                    LatestRecordTime = g.Max(p => p.CollectTime),
                    ParameterUnit = g.First().Unit,
                    IsQualified = g.OrderByDescending(p => p.CollectTime).First().IsQualified,
                    ChangeRate = CalculateChangeRate(g.OrderByDescending(p => p.CollectTime).Take(2).ToList()),
                    DataPoints = g.OrderBy(p => p.CollectTime).Select(p => new ParameterDataPointDto
                    {
                        Value = decimal.TryParse(p.ParameterValue, out var value) ? value : 0,
                        Timestamp = p.CollectTime,
                        IsQualified = p.IsQualified
                    }).ToList()
                })
                .ToList();

            return Ok(ApiResponse.OK(realtimeData));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工艺参数实时数据失败");
            return BadRequest(ApiResponse.Fail("获取实时数据失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取参数预警信息
    /// </summary>
    /// <param name="processId">工序ID</param>
    /// <param name="hours">检查最近多少小时的数据</param>
    /// <returns>预警信息</returns>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(ApiResponse<List<ParameterAlertDto>>), 200)]
    public async Task<IActionResult> GetParameterAlerts(
        [FromQuery] long? processId = null,
        [FromQuery] int hours = 24)
    {
        try
        {
            var cutoffTime = DateTime.UtcNow.AddHours(-hours);

            var parameters = await _parameterRepository.GetListAsync(
                p => (processId == null || p.ProcessId == processId) &&
                     p.CollectTime >= cutoffTime &&
                     !p.IsQualified);

            var alerts = parameters
                .GroupBy(p => new { p.ProcessId, p.ParameterName })
                .Select(g => new ParameterAlertDto
                {
                    ProcessId = g.Key.ProcessId,
                    ParameterName = g.Key.ParameterName,
                    AlertCount = g.Count(),
                    LatestAlertTime = g.Max(p => p.CollectTime),
                    LatestValue = decimal.TryParse(g.OrderByDescending(p => p.CollectTime).First().ParameterValue, out var val) ? val : 0,
                    StandardValue = null, // 需要从配置获取
                    UpperLimit = g.First().UpperLimit,
                    LowerLimit = g.First().LowerLimit,
                    ParameterUnit = g.First().Unit,
                    AlertLevel = DetermineAlertLevel(
                        decimal.TryParse(g.OrderByDescending(p => p.CollectTime).First().ParameterValue, out var value) ? value : 0,
                        null, g.First().UpperLimit, g.First().LowerLimit)
                })
                .OrderByDescending(a => a.LatestAlertTime)
                .ToList();

            return Ok(ApiResponse.OK(alerts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取参数预警信息失败");
            return BadRequest(ApiResponse.Fail("获取预警信息失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 设置参数标准值和限制
    /// </summary>
    /// <param name="request">设置请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("set-standards")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> SetParameterStandards([FromBody] SetParameterStandardsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            // 更新指定工序和参数的标准值
            var parameters = await _parameterRepository.GetListAsync(
                p => p.ProcessId == request.ProcessId && p.ParameterName == request.ParameterName);

            foreach (var parameter in parameters)
            {
                // 这里只能更新上下限，标准值需要通过其他方式管理
                parameter.UpperLimit = request.UpperLimit;
                parameter.LowerLimit = request.LowerLimit;
                parameter.IsQualified = IsParameterQualified(
                    decimal.TryParse(parameter.ParameterValue, out var val) ? val : 0, 
                    request.LowerLimit, request.UpperLimit);
                parameter.UpdatedBy = User.GetUserIdAsLong();
                parameter.UpdatedTime = DateTime.UtcNow;

                await _parameterRepository.UpdateAsync(parameter);
            }

            await _auditService.LogAsync("SetParameterStandards", "ProductionParameter", 0,
                $"设置参数标准值: 工序ID {request.ProcessId}, 参数 {request.ParameterName}");

            _logger.LogInformation("用户 {UserId} 设置工序 {ProcessId} 参数 {ParameterName} 的标准值",
                User.GetUserIdAsLong(), request.ProcessId, request.ParameterName);

            return Ok(ApiResponse.OK("参数标准值设置成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置参数标准值失败");
            return BadRequest(ApiResponse.Fail("设置标准值失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 导出工艺参数数据
    /// </summary>
    /// <param name="processId">工序ID</param>
    /// <param name="startTime">开始时间</param>
    /// <param name="endTime">结束时间</param>
    /// <param name="format">导出格式</param>
    /// <returns>导出文件</returns>
    [HttpGet("export")]
    [ProducesResponseType(typeof(FileResult), 200)]
    public async Task<IActionResult> ExportParameters(
        [FromQuery] long? processId = null,
        [FromQuery] DateTime? startTime = null,
        [FromQuery] DateTime? endTime = null,
        [FromQuery] string format = "csv")
    {
        try
        {
            var queryStartTime = startTime ?? DateTime.Now.AddDays(-30);
            var queryEndTime = endTime ?? DateTime.Now;

            var parameters = await _parameterRepository.GetListAsync(
                p => (processId == null || p.ProcessId == processId) &&
                     p.CollectTime >= queryStartTime &&
                     p.CollectTime <= queryEndTime);

            var fileName = $"ProcessParameters_{DateTime.Now:yyyyMMdd_HHmmss}";

            switch (format.ToLower())
            {
                case "csv":
                    var csvContent = GenerateCsv(parameters);
                    var csvBytes = Encoding.UTF8.GetBytes(csvContent);
                    return File(csvBytes, "text/csv", $"{fileName}.csv");

                case "json":
                    var jsonContent = JsonSerializer.Serialize(parameters, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    var jsonBytes = Encoding.UTF8.GetBytes(jsonContent);
                    return File(jsonBytes, "application/json", $"{fileName}.json");

                default:
                    return BadRequest(ApiResponse.Fail("不支持的导出格式"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出工艺参数数据失败");
            return BadRequest(ApiResponse.Fail("导出数据失败: " + ex.Message));
        }
    }

    #region Private Methods

    /// <summary>
    /// 判断参数是否合格
    /// </summary>
    /// <param name="value">参数值</param>
    /// <param name="lowerLimit">下限</param>
    /// <param name="upperLimit">上限</param>
    /// <returns>是否合格</returns>
    private static bool IsParameterQualified(decimal value, decimal? lowerLimit, decimal? upperLimit)
    {
        if (lowerLimit.HasValue && value < lowerLimit.Value)
            return false;

        if (upperLimit.HasValue && value > upperLimit.Value)
            return false;

        return true;
    }

    /// <summary>
    /// 计算标准差
    /// </summary>
    /// <param name="values">数值列表</param>
    /// <returns>标准差</returns>
    private static decimal CalculateStandardDeviation(List<decimal> values)
    {
        if (values.Count < 2) return 0;

        var average = values.Average();
        var sum = values.Sum(d => (double)Math.Pow((double)(d - average), 2));
        return (decimal)Math.Sqrt(sum / (values.Count - 1));
    }

    /// <summary>
    /// 生成趋势数据
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <returns>趋势数据</returns>
    private static List<ParameterTrendPointDto> GenerateTrendData(List<ProductionParameter> parameters)
    {
        return parameters
            .OrderBy(p => p.CollectTime)
            .GroupBy(p => p.CollectTime.Date)
            .Select(g => new ParameterTrendPointDto
            {
                Date = g.Key,
                AverageValue = g.Select(p => decimal.TryParse(p.ParameterValue, out var val) ? val : 0).Average(),
                MinValue = g.Select(p => decimal.TryParse(p.ParameterValue, out var val) ? val : 0).Min(),
                MaxValue = g.Select(p => decimal.TryParse(p.ParameterValue, out var val) ? val : 0).Max(),
                Count = g.Count(),
                QualifiedCount = g.Count(p => p.IsQualified)
            })
            .ToList();
    }

    /// <summary>
    /// 计算变化率
    /// </summary>
    /// <param name="recentParameters">最近的参数</param>
    /// <returns>变化率</returns>
    private static decimal? CalculateChangeRate(List<ProductionParameter> recentParameters)
    {
        if (recentParameters.Count < 2) return null;

        var latest = decimal.TryParse(recentParameters[0].ParameterValue, out var latestVal) ? latestVal : 0;
        var previous = decimal.TryParse(recentParameters[1].ParameterValue, out var prevVal) ? prevVal : 0;

        if (previous == 0) return null;

        return (latest - previous) / previous * 100;
    }

    /// <summary>
    /// 确定告警级别
    /// </summary>
    /// <param name="currentValue">当前值</param>
    /// <param name="standardValue">标准值</param>
    /// <param name="upperLimit">上限</param>
    /// <param name="lowerLimit">下限</param>
    /// <returns>告警级别</returns>
    private static string DetermineAlertLevel(decimal currentValue, decimal? standardValue, 
        decimal? upperLimit, decimal? lowerLimit)
    {
        if (upperLimit.HasValue && currentValue > upperLimit.Value)
        {
            var exceedPercentage = (currentValue - upperLimit.Value) / upperLimit.Value * 100;
            return exceedPercentage > 20 ? "Critical" : "High";
        }

        if (lowerLimit.HasValue && currentValue < lowerLimit.Value)
        {
            var exceedPercentage = (lowerLimit.Value - currentValue) / lowerLimit.Value * 100;
            return exceedPercentage > 20 ? "Critical" : "High";
        }

        return "Medium";
    }

    /// <summary>
    /// 生成CSV内容
    /// </summary>
    /// <param name="parameters">参数列表</param>
    /// <returns>CSV内容</returns>
    private static string GenerateCsv(List<ProductionParameter> parameters)
    {
        var csv = new StringBuilder();
        csv.AppendLine("ProcessId,ParameterName,ParameterValue,ParameterUnit,UpperLimit,LowerLimit,CollectTime,EquipmentCode,BatchNumber,IsQualified");

        foreach (var param in parameters)
        {
            csv.AppendLine($"{param.ProcessId},{param.ParameterName},{param.ParameterValue},{param.Unit}," +
                          $"{param.UpperLimit},{param.LowerLimit},{param.CollectTime:yyyy-MM-dd HH:mm:ss}," +
                          $"{param.EquipmentCode},{param.BatchNumber},{param.IsQualified}");
        }

        return csv.ToString();
    }

    #endregion
}

#region Parameter DTO Models

/// <summary>
/// 工艺参数DTO
/// </summary>
public class ProductionParameterDto
{
    public long Id { get; set; }
    public long ProcessId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public decimal ParameterValue { get; set; }
    public string? ParameterUnit { get; set; }
    public decimal? StandardValue { get; set; }
    public decimal? UpperLimit { get; set; }
    public decimal? LowerLimit { get; set; }
    public DateTime RecordTime { get; set; }
    public long? EquipmentId { get; set; }
    public string? BatchNumber { get; set; }
    public string? ProductBarcode { get; set; }
    public bool IsQualified { get; set; }
    public DateTime CreatedTime { get; set; }
}

/// <summary>
/// 批量记录参数请求
/// </summary>
public class BatchRecordParametersRequest
{
    [Required(ErrorMessage = "参数列表不能为空")]
    public List<RecordParameterRequest> Parameters { get; set; } = new();
}

/// <summary>
/// 记录参数请求
/// </summary>
public class RecordParameterRequest
{
    [Required(ErrorMessage = "工序ID不能为空")]
    public long ProcessId { get; set; }

    [Required(ErrorMessage = "参数名称不能为空")]
    [StringLength(100, ErrorMessage = "参数名称长度不能超过100个字符")]
    public string ParameterName { get; set; } = string.Empty;

    [Required(ErrorMessage = "参数值不能为空")]
    public decimal ParameterValue { get; set; }

    [StringLength(20, ErrorMessage = "参数单位长度不能超过20个字符")]
    public string? ParameterUnit { get; set; }

    public decimal? StandardValue { get; set; }
    public decimal? UpperLimit { get; set; }
    public decimal? LowerLimit { get; set; }
    public DateTime? RecordTime { get; set; }
    public long? EquipmentId { get; set; }

    [StringLength(50, ErrorMessage = "批次号长度不能超过50个字符")]
    public string? BatchNumber { get; set; }

    [StringLength(100, ErrorMessage = "产品条码长度不能超过100个字符")]
    public string? ProductBarcode { get; set; }
}

/// <summary>
/// 批量记录结果DTO
/// </summary>
public class BatchRecordResultDto
{
    public List<ParameterRecordResultDto> Results { get; set; } = new();
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int TotalCount { get; set; }
}

/// <summary>
/// 参数记录结果DTO
/// </summary>
public class ParameterRecordResultDto
{
    public string ParameterName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public long? ParameterId { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 参数统计DTO
/// </summary>
public class ParameterStatisticsDto
{
    public long ProcessId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public int TotalCount { get; set; }
    public int QualifiedCount { get; set; }
    public int UnqualifiedCount { get; set; }
    public decimal QualificationRate { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public decimal AverageValue { get; set; }
    public decimal StandardDeviation { get; set; }
    public string? ParameterUnit { get; set; }
    public decimal? StandardValue { get; set; }
    public decimal? UpperLimit { get; set; }
    public decimal? LowerLimit { get; set; }
    public DateTime QueryStartTime { get; set; }
    public DateTime QueryEndTime { get; set; }
    public List<ParameterTrendPointDto> TrendData { get; set; } = new();
}

/// <summary>
/// 参数趋势点DTO
/// </summary>
public class ParameterTrendPointDto
{
    public DateTime Date { get; set; }
    public decimal AverageValue { get; set; }
    public decimal MinValue { get; set; }
    public decimal MaxValue { get; set; }
    public int Count { get; set; }
    public int QualifiedCount { get; set; }
}

/// <summary>
/// 实时参数DTO
/// </summary>
public class RealtimeParameterDto
{
    public string ParameterName { get; set; } = string.Empty;
    public decimal LatestValue { get; set; }
    public DateTime LatestRecordTime { get; set; }
    public string? ParameterUnit { get; set; }
    public bool IsQualified { get; set; }
    public decimal? ChangeRate { get; set; }
    public List<ParameterDataPointDto> DataPoints { get; set; } = new();
}

/// <summary>
/// 参数数据点DTO
/// </summary>
public class ParameterDataPointDto
{
    public decimal Value { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsQualified { get; set; }
}

/// <summary>
/// 参数预警DTO
/// </summary>
public class ParameterAlertDto
{
    public long ProcessId { get; set; }
    public string ParameterName { get; set; } = string.Empty;
    public int AlertCount { get; set; }
    public DateTime LatestAlertTime { get; set; }
    public decimal LatestValue { get; set; }
    public decimal? StandardValue { get; set; }
    public decimal? UpperLimit { get; set; }
    public decimal? LowerLimit { get; set; }
    public string? ParameterUnit { get; set; }
    public string AlertLevel { get; set; } = string.Empty;
}

/// <summary>
/// 设置参数标准值请求
/// </summary>
public class SetParameterStandardsRequest
{
    [Required(ErrorMessage = "工序ID不能为空")]
    public long ProcessId { get; set; }

    [Required(ErrorMessage = "参数名称不能为空")]
    [StringLength(100, ErrorMessage = "参数名称长度不能超过100个字符")]
    public string ParameterName { get; set; } = string.Empty;

    public decimal? StandardValue { get; set; }
    public decimal? UpperLimit { get; set; }
    public decimal? LowerLimit { get; set; }
}

#endregion 