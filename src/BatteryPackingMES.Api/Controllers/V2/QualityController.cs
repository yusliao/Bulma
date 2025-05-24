using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Enums;
using BatteryPackingMES.Api.Models;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 质量管理控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/quality")]
[ApiVersion("2.0")]
[Authorize]
public class QualityController : ControllerBase
{
    private readonly IRepository<ProductItem> _productItemRepository;
    private readonly IRepository<ProductTraceability> _traceabilityRepository;
    private readonly IRepository<ProductionBatch> _batchRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<QualityController> _logger;

    public QualityController(
        IRepository<ProductItem> productItemRepository,
        IRepository<ProductTraceability> traceabilityRepository,
        IRepository<ProductionBatch> batchRepository,
        IAuditService auditService,
        ILogger<QualityController> logger)
    {
        _productItemRepository = productItemRepository;
        _traceabilityRepository = traceabilityRepository;
        _batchRepository = batchRepository;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// 质量检测
    /// </summary>
    /// <param name="request">检测请求</param>
    /// <returns>检测结果</returns>
    [HttpPost("inspection")]
    [ProducesResponseType(typeof(ApiResponse<QualityInspectionResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<QualityInspectionResultDto>>> PerformInspection([FromBody] QualityInspectionRequest request)
    {
        try
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(request.SerialNumber))
                return BadRequest(ApiResponse<QualityInspectionResultDto>.Fail("序列号不能为空", "INVALID_SERIAL_NUMBER"));

            // 查找产品项
            var productItem = await _productItemRepository.GetFirstOrDefaultAsync(p => p.SerialNumber == request.SerialNumber);
            if (productItem == null)
                return NotFound(ApiResponse<QualityInspectionResultDto>.Fail("产品不存在", "PRODUCT_NOT_FOUND"));

            // 更新产品质量信息
            productItem.QualityGrade = request.QualityGrade;
            productItem.QualityNotes = request.QualityNotes;
            productItem.ItemStatus = request.QualityGrade == QualityGrade.Defective ? 
                ProductItemStatus.Defective : ProductItemStatus.QualityChecked;

            await _productItemRepository.UpdateAsync(productItem);

            // 创建追溯记录
            var traceability = new ProductTraceability
            {
                ProductItemId = productItem.Id,
                SerialNumber = productItem.SerialNumber,
                BatchNumber = productItem.BatchNumber,
                ProcessStep = "QualityInspection",
                OperationType = "QualityCheck",
                OperationResult = request.QualityGrade.ToString(),
                Operator = request.InspectorName ?? User.Identity?.Name ?? "Unknown",
                WorkstationCode = request.WorkstationCode,
                OperationTime = DateTime.UtcNow,
                TestParameters = request.TestData != null ? System.Text.Json.JsonSerializer.Serialize(request.TestData) : null,
                QualityParameters = request.QualityNotes,
                IsQualityCheckpoint = true
            };

            await _traceabilityRepository.AddAsync(traceability);

            // 记录审计日志
            await _auditService.LogAsync("QualityInspection", "ProductItem", productItem.Id,
                $"质量检测: {request.SerialNumber}, 等级: {request.QualityGrade}, 检查员: {traceability.Operator}");

            var result = new QualityInspectionResultDto
            {
                SerialNumber = productItem.SerialNumber,
                QualityGrade = productItem.QualityGrade,
                QualityNotes = productItem.QualityNotes,
                InspectionTime = traceability.OperationTime,
                Inspector = traceability.Operator,
                TraceabilityId = traceability.Id
            };

            return Ok(ApiResponse<QualityInspectionResultDto>.Ok(result, "质量检测完成"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "质量检测失败: SerialNumber={SerialNumber}", request.SerialNumber);
            return StatusCode(500, ApiResponse<QualityInspectionResultDto>.Fail("质量检测失败", "INSPECTION_ERROR"));
        }
    }

    /// <summary>
    /// 批量质量检测
    /// </summary>
    /// <param name="request">批量检测请求</param>
    /// <returns>检测结果</returns>
    [HttpPost("batch-inspection")]
    [ProducesResponseType(typeof(ApiResponse<List<QualityInspectionResultDto>>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<List<QualityInspectionResultDto>>>> PerformBatchInspection([FromBody] BatchQualityInspectionRequest request)
    {
        try
        {
            if (request.Items == null || !request.Items.Any())
                return BadRequest(ApiResponse<List<QualityInspectionResultDto>>.Fail("检测项目不能为空", "INVALID_ITEMS"));

            var results = new List<QualityInspectionResultDto>();

            foreach (var item in request.Items)
            {
                var productItem = await _productItemRepository.GetFirstOrDefaultAsync(p => p.SerialNumber == item.SerialNumber);
                if (productItem == null)
                {
                    _logger.LogWarning("产品不存在: {SerialNumber}", item.SerialNumber);
                    continue;
                }

                // 更新产品质量信息
                productItem.QualityGrade = item.QualityGrade;
                productItem.QualityNotes = item.QualityNotes;
                productItem.ItemStatus = item.QualityGrade == QualityGrade.Defective ? 
                    ProductItemStatus.Defective : ProductItemStatus.QualityChecked;

                await _productItemRepository.UpdateAsync(productItem);

                // 创建追溯记录
                var traceability = new ProductTraceability
                {
                    ProductItemId = productItem.Id,
                    SerialNumber = productItem.SerialNumber,
                    BatchNumber = productItem.BatchNumber,
                    ProcessStep = "BatchQualityInspection",
                    OperationType = "BatchQualityCheck",
                    OperationResult = item.QualityGrade.ToString(),
                    Operator = request.InspectorName ?? User.Identity?.Name ?? "Unknown",
                    WorkstationCode = request.WorkstationCode,
                    OperationTime = DateTime.UtcNow,
                    QualityParameters = item.QualityNotes,
                    IsQualityCheckpoint = true
                };

                await _traceabilityRepository.AddAsync(traceability);

                results.Add(new QualityInspectionResultDto
                {
                    SerialNumber = productItem.SerialNumber,
                    QualityGrade = productItem.QualityGrade,
                    QualityNotes = productItem.QualityNotes,
                    InspectionTime = traceability.OperationTime,
                    Inspector = traceability.Operator,
                    TraceabilityId = traceability.Id
                });
            }

            // 记录审计日志
            await _auditService.LogAsync("BatchQualityInspection", "Quality", null,
                $"批量质量检测: 数量={results.Count}, 检查员={request.InspectorName}");

            return Ok(ApiResponse<List<QualityInspectionResultDto>>.Ok(results, $"批量质量检测完成，处理{results.Count}个产品"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量质量检测失败");
            return StatusCode(500, ApiResponse<List<QualityInspectionResultDto>>.Fail("批量质量检测失败", "BATCH_INSPECTION_ERROR"));
        }
    }

    /// <summary>
    /// 产品追溯查询
    /// </summary>
    /// <param name="serialNumber">序列号</param>
    /// <returns>追溯信息</returns>
    [HttpGet("traceability/{serialNumber}")]
    [ProducesResponseType(typeof(ApiResponse<ProductTraceabilityDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<ProductTraceabilityDto>>> GetProductTraceability(string serialNumber)
    {
        try
        {
            // 查找产品项
            var productItem = await _productItemRepository.GetFirstOrDefaultAsync(p => p.SerialNumber == serialNumber);
            if (productItem == null)
                return NotFound(ApiResponse<ProductTraceabilityDto>.Fail("产品不存在", "PRODUCT_NOT_FOUND"));

            // 获取批次信息
            var batch = await _batchRepository.GetByIdAsync(productItem.BatchId);

            // 获取追溯记录
            var traceabilityRecords = await _traceabilityRepository.GetByConditionAsync(t => t.SerialNumber == serialNumber);

            var result = new ProductTraceabilityDto
            {
                ProductInfo = new ProductInfoDto
                {
                    SerialNumber = productItem.SerialNumber,
                    Barcode = productItem.Barcode,
                    BatchNumber = productItem.BatchNumber,
                    ProductType = productItem.ProductType,
                    ProductionDate = productItem.ProductionDate,
                    QualityGrade = productItem.QualityGrade,
                    QualityNotes = productItem.QualityNotes,
                    ItemStatus = productItem.ItemStatus
                },
                BatchInfo = batch != null ? new BatchInfoDto
                {
                    BatchNumber = batch.BatchNumber,
                    ProductType = batch.ProductType,
                    PlannedQuantity = batch.PlannedQuantity,
                    ActualQuantity = batch.ActualQuantity,
                    BatchStatus = batch.BatchStatus,
                    WorkOrder = batch.WorkOrder,
                    CustomerOrder = batch.CustomerOrder
                } : null,
                TraceabilityRecords = traceabilityRecords
                    .OrderBy(t => t.OperationTime)
                    .Select(t => new TraceabilityRecordDto
                    {
                        Id = t.Id,
                        ProcessStep = t.ProcessStep,
                        OperationType = t.OperationType,
                        OperationResult = t.OperationResult,
                        Operator = t.Operator,
                        WorkstationCode = t.WorkstationCode,
                        OperationTime = t.OperationTime,
                        TestParameters = t.TestParameters,
                        QualityParameters = t.QualityParameters,
                        EnvironmentalData = t.EnvironmentalData,
                        IsQualityCheckpoint = t.IsQualityCheckpoint
                    }).ToList()
            };

            return Ok(ApiResponse<ProductTraceabilityDto>.Ok(result, "获取产品追溯信息成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取产品追溯信息失败: SerialNumber={SerialNumber}", serialNumber);
            return StatusCode(500, ApiResponse<ProductTraceabilityDto>.Fail("获取产品追溯信息失败", "TRACEABILITY_ERROR"));
        }
    }

    /// <summary>
    /// 批次质量统计
    /// </summary>
    /// <param name="batchNumber">批次号</param>
    /// <returns>质量统计</returns>
    [HttpGet("batch-statistics/{batchNumber}")]
    [ProducesResponseType(typeof(ApiResponse<BatchQualityStatisticsDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<ActionResult<ApiResponse<BatchQualityStatisticsDto>>> GetBatchQualityStatistics(string batchNumber)
    {
        try
        {
            // 查找批次
            var batch = await _batchRepository.GetFirstOrDefaultAsync(b => b.BatchNumber == batchNumber);
            if (batch == null)
                return NotFound(ApiResponse<BatchQualityStatisticsDto>.Fail("批次不存在", "BATCH_NOT_FOUND"));

            // 获取批次下的所有产品
            var productItems = await _productItemRepository.GetByConditionAsync(p => p.BatchNumber == batchNumber);

            var statistics = new BatchQualityStatisticsDto
            {
                BatchNumber = batchNumber,
                TotalProducts = productItems.Count,
                GradeACount = productItems.Count(p => p.QualityGrade == QualityGrade.A),
                GradeBCount = productItems.Count(p => p.QualityGrade == QualityGrade.B),
                GradeCCount = productItems.Count(p => p.QualityGrade == QualityGrade.C),
                DefectiveCount = productItems.Count(p => p.QualityGrade == QualityGrade.Defective),
                UngradedCount = productItems.Count(p => p.QualityGrade == QualityGrade.Ungraded),
                QualityRate = productItems.Count > 0 ? 
                    (double)productItems.Count(p => p.QualityGrade == QualityGrade.A || p.QualityGrade == QualityGrade.B) / productItems.Count * 100 : 0,
                DefectiveRate = productItems.Count > 0 ? 
                    (double)productItems.Count(p => p.QualityGrade == QualityGrade.Defective) / productItems.Count * 100 : 0,
                CompletionRate = batch.PlannedQuantity > 0 ? 
                    (double)productItems.Count / batch.PlannedQuantity * 100 : 0
            };

            return Ok(ApiResponse<BatchQualityStatisticsDto>.Ok(statistics, "获取批次质量统计成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取批次质量统计失败: BatchNumber={BatchNumber}", batchNumber);
            return StatusCode(500, ApiResponse<BatchQualityStatisticsDto>.Fail("获取批次质量统计失败", "STATISTICS_ERROR"));
        }
    }

    /// <summary>
    /// 不合格品管理
    /// </summary>
    /// <param name="request">处理请求</param>
    /// <returns>处理结果</returns>
    [HttpPost("defective-handling")]
    [ProducesResponseType(typeof(ApiResponse<DefectiveHandlingResultDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<ActionResult<ApiResponse<DefectiveHandlingResultDto>>> HandleDefectiveProduct([FromBody] DefectiveHandlingRequest request)
    {
        try
        {
            var productItem = await _productItemRepository.GetFirstOrDefaultAsync(p => p.SerialNumber == request.SerialNumber);
            if (productItem == null)
                return NotFound(ApiResponse<DefectiveHandlingResultDto>.Fail("产品不存在", "PRODUCT_NOT_FOUND"));

            if (productItem.QualityGrade != QualityGrade.Defective)
                return BadRequest(ApiResponse<DefectiveHandlingResultDto>.Fail("产品不是不合格品", "NOT_DEFECTIVE"));

            // 更新产品状态
            productItem.ItemStatus = request.HandlingAction switch
            {
                DefectiveHandlingAction.Rework => ProductItemStatus.Rework,
                DefectiveHandlingAction.Scrap => ProductItemStatus.Scrapped,
                DefectiveHandlingAction.Downgrade => ProductItemStatus.Downgraded,
                _ => productItem.ItemStatus
            };

            productItem.QualityNotes += $"\n处理方式: {request.HandlingAction}, 处理人: {request.HandlerName}, 处理时间: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}, 处理说明: {request.HandlingNotes}";

            await _productItemRepository.UpdateAsync(productItem);

            // 创建追溯记录
            var traceability = new ProductTraceability
            {
                ProductItemId = productItem.Id,
                SerialNumber = productItem.SerialNumber,
                BatchNumber = productItem.BatchNumber,
                ProcessStep = "DefectiveHandling",
                OperationType = request.HandlingAction.ToString(),
                OperationResult = "Handled",
                Operator = request.HandlerName ?? User.Identity?.Name ?? "Unknown",
                OperationTime = DateTime.UtcNow,
                QualityParameters = request.HandlingNotes,
                IsQualityCheckpoint = true
            };

            await _traceabilityRepository.AddAsync(traceability);

            // 记录审计日志
            await _auditService.LogAsync("DefectiveHandling", "ProductItem", productItem.Id,
                $"不合格品处理: {request.SerialNumber}, 处理方式: {request.HandlingAction}");

            var result = new DefectiveHandlingResultDto
            {
                SerialNumber = productItem.SerialNumber,
                HandlingAction = request.HandlingAction,
                NewStatus = productItem.ItemStatus,
                HandledBy = traceability.Operator,
                HandledTime = traceability.OperationTime,
                TraceabilityId = traceability.Id
            };

            return Ok(ApiResponse<DefectiveHandlingResultDto>.Ok(result, "不合格品处理完成"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "不合格品处理失败: SerialNumber={SerialNumber}", request.SerialNumber);
            return StatusCode(500, ApiResponse<DefectiveHandlingResultDto>.Fail("不合格品处理失败", "HANDLING_ERROR"));
        }
    }

    /// <summary>
    /// 质量报表查询
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="productType">产品类型</param>
    /// <returns>质量报表</returns>
    [HttpGet("quality-report")]
    [ProducesResponseType(typeof(ApiResponse<QualityReportDto>), 200)]
    public async Task<ActionResult<ApiResponse<QualityReportDto>>> GetQualityReport(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? productType = null)
    {
        try
        {
            var start = startDate ?? DateTime.Today.AddDays(-30);
            var end = endDate ?? DateTime.Today.AddDays(1);

            var productItems = await _productItemRepository.GetByConditionAsync(p => 
                p.ProductionDate >= start && 
                p.ProductionDate <= end &&
                (string.IsNullOrEmpty(productType) || p.ProductType == productType));

            var report = new QualityReportDto
            {
                ReportPeriod = new ReportPeriodDto
                {
                    StartDate = start,
                    EndDate = end,
                    ProductType = productType
                },
                Summary = new QualitySummaryDto
                {
                    TotalProducts = productItems.Count,
                    QualifiedProducts = productItems.Count(p => p.QualityGrade == QualityGrade.A || p.QualityGrade == QualityGrade.B),
                    DefectiveProducts = productItems.Count(p => p.QualityGrade == QualityGrade.Defective),
                    QualityRate = productItems.Count > 0 ? 
                        (double)productItems.Count(p => p.QualityGrade == QualityGrade.A || p.QualityGrade == QualityGrade.B) / productItems.Count * 100 : 0,
                    DefectiveRate = productItems.Count > 0 ? 
                        (double)productItems.Count(p => p.QualityGrade == QualityGrade.Defective) / productItems.Count * 100 : 0
                },
                GradeDistribution = new Dictionary<QualityGrade, int>
                {
                    { QualityGrade.A, productItems.Count(p => p.QualityGrade == QualityGrade.A) },
                    { QualityGrade.B, productItems.Count(p => p.QualityGrade == QualityGrade.B) },
                    { QualityGrade.C, productItems.Count(p => p.QualityGrade == QualityGrade.C) },
                    { QualityGrade.Defective, productItems.Count(p => p.QualityGrade == QualityGrade.Defective) },
                    { QualityGrade.Ungraded, productItems.Count(p => p.QualityGrade == QualityGrade.Ungraded) }
                },
                DailyTrends = productItems
                    .GroupBy(p => p.ProductionDate.Date)
                    .OrderBy(g => g.Key)
                    .Select(g => new DailyQualityTrendDto
                    {
                        Date = g.Key,
                        TotalProducts = g.Count(),
                        QualifiedProducts = g.Count(p => p.QualityGrade == QualityGrade.A || p.QualityGrade == QualityGrade.B),
                        DefectiveProducts = g.Count(p => p.QualityGrade == QualityGrade.Defective),
                        QualityRate = g.Count() > 0 ? 
                            (double)g.Count(p => p.QualityGrade == QualityGrade.A || p.QualityGrade == QualityGrade.B) / g.Count() * 100 : 0
                    }).ToList()
            };

            return Ok(ApiResponse<QualityReportDto>.Ok(report, "获取质量报表成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取质量报表失败");
            return StatusCode(500, ApiResponse<QualityReportDto>.Fail("获取质量报表失败", "REPORT_ERROR"));
        }
    }
}

#region 请求和响应模型

/// <summary>
/// 质量检测请求
/// </summary>
public class QualityInspectionRequest
{
    /// <summary>
    /// 序列号
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 质量等级
    /// </summary>
    public QualityGrade QualityGrade { get; set; }

    /// <summary>
    /// 质量备注
    /// </summary>
    public string? QualityNotes { get; set; }

    /// <summary>
    /// 检查员姓名
    /// </summary>
    public string? InspectorName { get; set; }

    /// <summary>
    /// 工作站代码
    /// </summary>
    public string? WorkstationCode { get; set; }

    /// <summary>
    /// 测试数据
    /// </summary>
    public Dictionary<string, object>? TestData { get; set; }
}

/// <summary>
/// 批量质量检测请求
/// </summary>
public class BatchQualityInspectionRequest
{
    /// <summary>
    /// 检查员姓名
    /// </summary>
    public string? InspectorName { get; set; }

    /// <summary>
    /// 工作站代码
    /// </summary>
    public string? WorkstationCode { get; set; }

    /// <summary>
    /// 检测项目
    /// </summary>
    public List<QualityInspectionItem> Items { get; set; } = new();
}

/// <summary>
/// 质量检测项目
/// </summary>
public class QualityInspectionItem
{
    /// <summary>
    /// 序列号
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 质量等级
    /// </summary>
    public QualityGrade QualityGrade { get; set; }

    /// <summary>
    /// 质量备注
    /// </summary>
    public string? QualityNotes { get; set; }
}

/// <summary>
/// 不合格品处理请求
/// </summary>
public class DefectiveHandlingRequest
{
    /// <summary>
    /// 序列号
    /// </summary>
    public string SerialNumber { get; set; } = string.Empty;

    /// <summary>
    /// 处理方式
    /// </summary>
    public DefectiveHandlingAction HandlingAction { get; set; }

    /// <summary>
    /// 处理人员
    /// </summary>
    public string? HandlerName { get; set; }

    /// <summary>
    /// 处理说明
    /// </summary>
    public string? HandlingNotes { get; set; }
}

#endregion 