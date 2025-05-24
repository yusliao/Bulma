using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Enums;
using BatteryPackingMES.Api.Extensions;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 生产批次管理控制器 V2
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/production-batches")]
[Authorize]
public class ProductionBatchController : ControllerBase
{
    private readonly IRepository<ProductionBatch> _batchRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProductionBatchController> _logger;

    public ProductionBatchController(
        IRepository<ProductionBatch> batchRepository,
        IAuditService auditService,
        ILogger<ProductionBatchController> logger)
    {
        _batchRepository = batchRepository;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询生产批次
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="status">状态筛选</param>
    /// <returns>分页的生产批次列表</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProductionBatch>>), 200)]
    public async Task<IActionResult> GetBatches(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] BatchStatus? status = null)
    {
        try
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var (items, total) = await _batchRepository.GetPagedAsync(
                pageIndex, 
                pageSize, 
                b => status == null || b.BatchStatus == status);

            var result = new PagedResult<ProductionBatch>
            {
                Items = items,
                Total = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(ApiResponse.OK(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询生产批次失败");
            return BadRequest(ApiResponse.Fail("查询生产批次失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 根据ID获取生产批次
    /// </summary>
    /// <param name="id">批次ID</param>
    /// <returns>生产批次信息</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProductionBatch>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetBatch(long id)
    {
        try
        {
            var batch = await _batchRepository.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.Fail("批次不存在"));
            }

            return Ok(ApiResponse.OK(batch));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取生产批次失败: {BatchId}", id);
            return BadRequest(ApiResponse.Fail("获取生产批次失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 创建生产批次
    /// </summary>
    /// <param name="batch">批次信息</param>
    /// <returns>批次ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<long>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CreateBatch([FromBody] ProductionBatch batch)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            batch.CreatedBy = User.GetUserIdAsLong();
            batch.BatchStatus = BatchStatus.Created;
            
            var batchId = await _batchRepository.AddAsync(batch);

            await _auditService.LogAsync("CreateBatch", "ProductionBatch", batchId,
                $"创建生产批次: {batch.BatchNumber}");

            _logger.LogInformation("用户 {UserId} 创建生产批次 {BatchNumber}", 
                User.GetUserIdAsLong(), batch.BatchNumber);

            return Ok(ApiResponse.OK(batchId, "批次创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建生产批次失败");
            return BadRequest(ApiResponse.Fail("创建生产批次失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 更新批次状态
    /// </summary>
    /// <param name="id">批次ID</param>
    /// <param name="request">状态更新请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> UpdateBatchStatus(long id, [FromBody] UpdateBatchStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            var batch = await _batchRepository.GetByIdAsync(id);
            if (batch == null)
            {
                return NotFound(ApiResponse.Fail("批次不存在"));
            }

            batch.BatchStatus = request.Status;
            batch.Remarks = request.Remark;
            batch.UpdatedBy = User.GetUserIdAsLong();
            batch.UpdatedTime = DateTime.UtcNow;

            var success = await _batchRepository.UpdateAsync(batch);
            
            if (success)
            {
                await _auditService.LogAsync("UpdateBatchStatus", "ProductionBatch", id,
                    $"更新批次状态为: {request.Status}, 备注: {request.Remark}");

                _logger.LogInformation("用户 {UserId} 更新批次 {BatchId} 状态为 {Status}", 
                    User.GetUserIdAsLong(), id, request.Status);

                return Ok(ApiResponse.OK("批次状态更新成功"));
            }

            return BadRequest(ApiResponse.Fail("批次状态更新失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新批次状态失败: {BatchId}", id);
            return BadRequest(ApiResponse.Fail("更新批次状态失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取批次统计信息
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<BatchStatisticsSummary>), 200)]
    public async Task<IActionResult> GetBatchStatistics(
        [FromQuery, Required] DateTime startDate,
        [FromQuery, Required] DateTime endDate)
    {
        try
        {
            var batches = await _batchRepository.GetAllAsync(
                b => b.CreatedTime >= startDate && b.CreatedTime <= endDate);

            var statistics = new BatchStatisticsSummary
            {
                TotalBatches = batches.Count,
                PlannedBatches = batches.Count(b => b.BatchStatus == BatchStatus.Planned),
                InProgressBatches = batches.Count(b => b.BatchStatus == BatchStatus.InProgress),
                CompletedBatches = batches.Count(b => b.BatchStatus == BatchStatus.Completed),
                TotalPlannedQuantity = batches.Sum(b => b.PlannedQuantity),
                TotalActualQuantity = batches.Sum(b => b.ActualQuantity),
                CompletionRate = batches.Count > 0 ? 
                    (decimal)batches.Count(b => b.BatchStatus == BatchStatus.Completed) / batches.Count * 100 : 0
            };

            return Ok(ApiResponse.OK(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取批次统计信息失败");
            return BadRequest(ApiResponse.Fail("获取批次统计信息失败: " + ex.Message));
        }
    }
}

#region 请求和响应模型

/// <summary>
/// 更新批次状态请求
/// </summary>
public class UpdateBatchStatusRequest
{
    /// <summary>
    /// 新状态
    /// </summary>
    [Required]
    public BatchStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 批次统计汇总
/// </summary>
public class BatchStatisticsSummary
{
    public int TotalBatches { get; set; }
    public int PlannedBatches { get; set; }
    public int InProgressBatches { get; set; }
    public int CompletedBatches { get; set; }
    public int TotalPlannedQuantity { get; set; }
    public int TotalActualQuantity { get; set; }
    public decimal CompletionRate { get; set; }
}

#endregion 