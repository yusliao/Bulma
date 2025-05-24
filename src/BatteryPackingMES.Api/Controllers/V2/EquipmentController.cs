using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Enums;
using BatteryPackingMES.Api.Extensions;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 设备管理控制器 V2
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/equipment")]
[Authorize]
public class EquipmentController : ControllerBase
{
    private readonly IEquipmentService _equipmentService;
    private readonly IAuditService _auditService;
    private readonly ILogger<EquipmentController> _logger;

    public EquipmentController(
        IEquipmentService equipmentService,
        IAuditService auditService,
        ILogger<EquipmentController> logger)
    {
        _equipmentService = equipmentService;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询设备
    /// </summary>
    /// <param name="request">查询条件</param>
    /// <returns>分页的设备列表</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<Equipment>>), 200)]
    public async Task<IActionResult> GetEquipment([FromBody] EquipmentQueryRequest request)
    {
        try
        {
            var (items, total) = await _equipmentService.GetEquipmentsPagedAsync(request);
            var result = new PagedResult<Equipment>
            {
                Items = items,
                Total = total,
                PageIndex = request.PageIndex,
                PageSize = request.PageSize
            };

            return Ok(ApiResponse.OK(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询设备失败");
            return BadRequest(ApiResponse.Fail("查询设备失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 根据ID获取设备
    /// </summary>
    /// <param name="id">设备ID</param>
    /// <returns>设备信息</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<Equipment>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetEquipment(long id)
    {
        try
        {
            var equipment = await _equipmentService.GetEquipmentByIdAsync(id);
            if (equipment == null)
            {
                return NotFound(ApiResponse.Fail("设备不存在"));
            }

            return Ok(ApiResponse.OK(equipment));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备信息失败: {EquipmentId}", id);
            return BadRequest(ApiResponse.Fail("获取设备信息失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 创建设备
    /// </summary>
    /// <param name="equipment">设备信息</param>
    /// <returns>设备ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<long>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CreateEquipment([FromBody] Equipment equipment)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            equipment.CreatedBy = User.GetUserIdAsLong();
            equipment.CurrentStatus = EquipmentStatus.Offline;
            
            var equipmentId = await _equipmentService.CreateEquipmentAsync(equipment);

            await _auditService.LogAsync("CreateEquipment", "Equipment", equipmentId,
                $"创建设备: {equipment.EquipmentCode}");

            _logger.LogInformation("用户 {UserId} 创建设备 {EquipmentCode}", 
                User.GetUserIdAsLong(), equipment.EquipmentCode);

            return Ok(ApiResponse.OK(equipmentId, "设备创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建设备失败");
            return BadRequest(ApiResponse.Fail("创建设备失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 更新设备信息
    /// </summary>
    /// <param name="id">设备ID</param>
    /// <param name="equipment">设备信息</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> UpdateEquipment(long id, [FromBody] Equipment equipment)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            equipment.Id = id;
            equipment.UpdatedBy = User.GetUserIdAsLong();
            equipment.UpdatedTime = DateTime.UtcNow;

            var success = await _equipmentService.UpdateEquipmentAsync(equipment);
            
            if (success)
            {
                await _auditService.LogAsync("UpdateEquipment", "Equipment", id,
                    $"更新设备信息: {equipment.EquipmentCode}");

                _logger.LogInformation("用户 {UserId} 更新设备 {EquipmentId}", 
                    User.GetUserIdAsLong(), id);

                return Ok(ApiResponse.OK("设备信息更新成功"));
            }

            return BadRequest(ApiResponse.Fail("设备信息更新失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备信息失败: {EquipmentId}", id);
            return BadRequest(ApiResponse.Fail("更新设备信息失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 更新设备状态
    /// </summary>
    /// <param name="id">设备ID</param>
    /// <param name="request">状态更新请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> UpdateEquipmentStatus(long id, [FromBody] UpdateEquipmentStatusRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            var statusUpdateRequest = new EquipmentStatusUpdateRequest
            {
                EquipmentId = id,
                NewStatus = request.Status,
                Reason = request.Remark,
                OperatorId = User.GetUserIdAsLong(),
                OperatorName = User.GetUserName()
            };

            var success = await _equipmentService.UpdateEquipmentStatusAsync(statusUpdateRequest);
            
            if (success)
            {
                await _auditService.LogAsync("UpdateEquipmentStatus", "Equipment", id,
                    $"更新设备状态为: {request.Status}, 备注: {request.Remark}");

                _logger.LogInformation("用户 {UserId} 更新设备 {EquipmentId} 状态为 {Status}", 
                    User.GetUserIdAsLong(), id, request.Status);

                return Ok(ApiResponse.OK("设备状态更新成功"));
            }

            return BadRequest(ApiResponse.Fail("设备状态更新失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备状态失败: {EquipmentId}", id);
            return BadRequest(ApiResponse.Fail("更新设备状态失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取设备维护记录
    /// </summary>
    /// <param name="id">设备ID</param>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <returns>维护记录列表</returns>
    [HttpGet("{id}/maintenance")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<EquipmentMaintenanceRecord>>), 200)]
    public async Task<IActionResult> GetMaintenanceRecords(
        long id,
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20)
    {
        try
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var request = new MaintenanceQueryRequest
            {
                EquipmentId = id,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            var (items, total) = await _equipmentService.GetMaintenanceRecordsPagedAsync(request);
            var result = new PagedResult<EquipmentMaintenanceRecord>
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
            _logger.LogError(ex, "获取设备维护记录失败: {EquipmentId}", id);
            return BadRequest(ApiResponse.Fail("获取设备维护记录失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 创建维护记录
    /// </summary>
    /// <param name="record">维护记录</param>
    /// <returns>记录ID</returns>
    [HttpPost("maintenance")]
    [ProducesResponseType(typeof(ApiResponse<long>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CreateMaintenanceRecord([FromBody] EquipmentMaintenanceRecord record)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            record.CreatedBy = User.GetUserIdAsLong();
            
            var recordId = await _equipmentService.CreateMaintenancePlanAsync(record);

            await _auditService.LogAsync("CreateMaintenanceRecord", "MaintenanceRecord", recordId,
                $"创建设备 {record.EquipmentId} 维护记录");

            _logger.LogInformation("用户 {UserId} 创建设备 {EquipmentId} 维护记录", 
                User.GetUserIdAsLong(), record.EquipmentId);

            return Ok(ApiResponse.OK(recordId, "维护记录创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建维护记录失败");
            return BadRequest(ApiResponse.Fail("创建维护记录失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取设备统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<EquipmentMonitoringDashboard>), 200)]
    public async Task<IActionResult> GetEquipmentStatistics()
    {
        try
        {
            var statistics = await _equipmentService.GetMonitoringDashboardAsync();
            return Ok(ApiResponse.OK(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备统计信息失败");
            return BadRequest(ApiResponse.Fail("获取设备统计信息失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取设备性能数据
    /// </summary>
    /// <param name="id">设备ID</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>性能数据</returns>
    [HttpGet("{id}/performance")]
    [ProducesResponseType(typeof(ApiResponse<EquipmentOeeStatistics>), 200)]
    public async Task<IActionResult> GetEquipmentPerformance(
        long id,
        [FromQuery, Required] DateTime startDate,
        [FromQuery, Required] DateTime endDate)
    {
        try
        {
            var performance = await _equipmentService.GetEquipmentOeeStatisticsAsync(id, startDate, endDate);
            return Ok(ApiResponse.OK(performance));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备性能数据失败: {EquipmentId}", id);
            return BadRequest(ApiResponse.Fail("获取设备性能数据失败: " + ex.Message));
        }
    }
}

#region 请求和响应模型

/// <summary>
/// 更新设备状态请求
/// </summary>
public class UpdateEquipmentStatusRequest
{
    /// <summary>
    /// 新状态
    /// </summary>
    [Required]
    public EquipmentStatus Status { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    public string? Remark { get; set; }
}

#endregion 