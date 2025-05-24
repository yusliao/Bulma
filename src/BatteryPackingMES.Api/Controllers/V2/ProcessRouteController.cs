using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Enums;
using BatteryPackingMES.Api.Models;
using BatteryPackingMES.Api.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 工艺路线管理控制器 V2.0
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/process-routes")]
[Authorize]
[Produces("application/json")]
public class ProcessRouteController : ControllerBase
{
    private readonly IRepository<ProcessRoute> _routeRepository;
    private readonly IRepository<ProcessRouteStep> _stepRepository;
    private readonly IRepository<Process> _processRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProcessRouteController> _logger;

    public ProcessRouteController(
        IRepository<ProcessRoute> routeRepository,
        IRepository<ProcessRouteStep> stepRepository,
        IRepository<Process> processRepository,
        IAuditService auditService,
        ILogger<ProcessRouteController> logger)
    {
        _routeRepository = routeRepository;
        _stepRepository = stepRepository;
        _processRepository = processRepository;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// 分页查询工艺路线
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="productType">产品类型筛选</param>
    /// <param name="isEnabled">启用状态筛选</param>
    /// <param name="keyword">关键词搜索</param>
    /// <returns>分页的工艺路线列表</returns>
    [HttpGet("paged")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<ProcessRouteDto>>), 200)]
    public async Task<IActionResult> GetPagedRoutes(
        [FromQuery] int pageIndex = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? productType = null,
        [FromQuery] bool? isEnabled = null,
        [FromQuery] string? keyword = null)
    {
        try
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100;

            var (routes, total) = await _routeRepository.GetPagedAsync(
                pageIndex,
                pageSize,
                r => (string.IsNullOrEmpty(productType) || r.ProductType == productType) &&
                     (isEnabled == null || r.IsEnabled == isEnabled) &&
                     (string.IsNullOrEmpty(keyword) || 
                      r.RouteCode.Contains(keyword) || 
                      r.RouteName.Contains(keyword)));

            var routeDtos = new List<ProcessRouteDto>();
            foreach (var route in routes)
            {
                var steps = await _stepRepository.GetListAsync(s => s.ProcessRouteId == route.Id);
                routeDtos.Add(new ProcessRouteDto
                {
                    Id = route.Id,
                    RouteCode = route.RouteCode,
                    RouteName = route.RouteName,
                    ProductType = route.ProductType,
                    Description = route.Description,
                    IsEnabled = route.IsEnabled,
                    VersionNumber = route.VersionNumber,
                    StepCount = steps.Count,
                    CreatedTime = route.CreatedTime,
                    UpdatedTime = route.UpdatedTime
                });
            }

            var result = new PagedResult<ProcessRouteDto>
            {
                Items = routeDtos,
                Total = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(ApiResponse.OK(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询工艺路线失败");
            return BadRequest(ApiResponse.Fail("查询工艺路线失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 根据ID获取工艺路线详情
    /// </summary>
    /// <param name="id">路线ID</param>
    /// <returns>工艺路线详细信息</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProcessRouteDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetRouteDetail(long id)
    {
        try
        {
            var route = await _routeRepository.GetByIdAsync(id);
            if (route == null)
            {
                return NotFound(ApiResponse.Fail("工艺路线不存在"));
            }

            var steps = await _stepRepository.GetListAsync(s => s.ProcessRouteId == id);
            var stepDetails = new List<ProcessRouteStepDetailDto>();

            foreach (var step in steps.OrderBy(s => s.StepOrder))
            {
                var process = await _processRepository.GetByIdAsync(step.ProcessId);
                stepDetails.Add(new ProcessRouteStepDetailDto
                {
                    Id = step.Id,
                    ProcessId = step.ProcessId,
                    ProcessCode = process?.ProcessCode ?? "",
                    ProcessName = process?.ProcessName ?? "",
                    ProcessType = process?.ProcessType ?? ProcessType.CellPacking,
                    StepOrder = step.StepOrder,
                    IsRequired = step.IsRequired,
                    StepConfig = step.StepConfig,
                    StandardTime = process?.StandardTime ?? 0
                });
            }

            var detail = new ProcessRouteDetailDto
            {
                Id = route.Id,
                RouteCode = route.RouteCode,
                RouteName = route.RouteName,
                ProductType = route.ProductType,
                Description = route.Description,
                IsEnabled = route.IsEnabled,
                VersionNumber = route.VersionNumber,
                RouteConfig = route.RouteConfig,
                Steps = stepDetails,
                CreatedTime = route.CreatedTime,
                UpdatedTime = route.UpdatedTime
            };

            return Ok(ApiResponse.OK(detail));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工艺路线详情失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("获取工艺路线详情失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 创建工艺路线
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <returns>路线ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<long>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CreateRoute([FromBody] CreateProcessRouteRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            // 检查路线编码是否已存在
            var exists = await _routeRepository.ExistsAsync(r => r.RouteCode == request.RouteCode);
            if (exists)
            {
                return BadRequest(ApiResponse.Fail("工艺路线编码已存在"));
            }

            var route = new ProcessRoute
            {
                RouteCode = request.RouteCode,
                RouteName = request.RouteName,
                ProductType = request.ProductType,
                Description = request.Description,
                IsEnabled = request.IsEnabled,
                VersionNumber = request.VersionNumber ?? "1.0",
                RouteConfig = request.RouteConfig,
                CreatedBy = User.GetUserIdAsLong()
            };

            var routeId = await _routeRepository.AddAsync(route);

            // 创建工艺路线步骤
            if (request.Steps != null && request.Steps.Any())
            {
                foreach (var stepRequest in request.Steps)
                {
                    var step = new ProcessRouteStep
                    {
                        ProcessRouteId = routeId,
                        ProcessId = stepRequest.ProcessId,
                        StepOrder = stepRequest.StepOrder,
                        IsRequired = stepRequest.IsRequired,
                        StepConfig = stepRequest.StepConfig,
                        CreatedBy = User.GetUserIdAsLong()
                    };

                    await _stepRepository.AddAsync(step);
                }
            }

            await _auditService.LogAsync("CreateRoute", "ProcessRoute", routeId,
                $"创建工艺路线: {request.RouteCode}");

            _logger.LogInformation("用户 {UserId} 创建工艺路线 {RouteCode}",
                User.GetUserIdAsLong(), request.RouteCode);

            return Ok(ApiResponse.OK(routeId, "工艺路线创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建工艺路线失败");
            return BadRequest(ApiResponse.Fail("创建工艺路线失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 更新工艺路线基本信息
    /// </summary>
    /// <param name="id">路线ID</param>
    /// <param name="request">更新请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> UpdateRoute(long id, [FromBody] UpdateProcessRouteRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            var route = await _routeRepository.GetByIdAsync(id);
            if (route == null)
            {
                return NotFound(ApiResponse.Fail("工艺路线不存在"));
            }

            // 检查路线编码是否与其他路线重复
            var duplicateExists = await _routeRepository.ExistsAsync(
                r => r.RouteCode == request.RouteCode && r.Id != id);
            if (duplicateExists)
            {
                return BadRequest(ApiResponse.Fail("工艺路线编码已存在"));
            }

            var oldRouteInfo = JsonSerializer.Serialize(route);

            route.RouteCode = request.RouteCode;
            route.RouteName = request.RouteName;
            route.ProductType = request.ProductType;
            route.Description = request.Description;
            route.IsEnabled = request.IsEnabled;
            route.VersionNumber = request.VersionNumber ?? route.VersionNumber;
            route.RouteConfig = request.RouteConfig;
            route.UpdatedBy = User.GetUserIdAsLong();
            route.UpdatedTime = DateTime.UtcNow;

            var success = await _routeRepository.UpdateAsync(route);

            if (success)
            {
                await _auditService.LogAsync("UpdateRoute", "ProcessRoute", id,
                    $"更新工艺路线: {request.RouteCode}");

                _logger.LogInformation("用户 {UserId} 更新工艺路线 {RouteId}",
                    User.GetUserIdAsLong(), id);

                return Ok(ApiResponse.OK("工艺路线更新成功"));
            }

            return BadRequest(ApiResponse.Fail("工艺路线更新失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新工艺路线失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("更新工艺路线失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 删除工艺路线
    /// </summary>
    /// <param name="id">路线ID</param>
    /// <returns>操作结果</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> DeleteRoute(long id)
    {
        try
        {
            var route = await _routeRepository.GetByIdAsync(id);
            if (route == null)
            {
                return NotFound(ApiResponse.Fail("工艺路线不存在"));
            }

            // 检查是否有相关的生产批次在使用此路线
            // 这里可以添加业务规则检查

            // 删除工艺路线步骤
            var steps = await _stepRepository.GetListAsync(s => s.ProcessRouteId == id);
            foreach (var step in steps)
            {
                await _stepRepository.DeleteAsync(step.Id);
            }

            // 删除工艺路线
            var success = await _routeRepository.DeleteAsync(id);

            if (success)
            {
                await _auditService.LogAsync("DeleteRoute", "ProcessRoute", id,
                    $"删除工艺路线: {route.RouteCode}");

                _logger.LogInformation("用户 {UserId} 删除工艺路线 {RouteId}",
                    User.GetUserIdAsLong(), id);

                return Ok(ApiResponse.OK("工艺路线删除成功"));
            }

            return BadRequest(ApiResponse.Fail("工艺路线删除失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除工艺路线失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("删除工艺路线失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 复制工艺路线
    /// </summary>
    /// <param name="id">源路线ID</param>
    /// <param name="request">复制请求</param>
    /// <returns>新路线ID</returns>
    [HttpPost("{id}/copy")]
    [ProducesResponseType(typeof(ApiResponse<long>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> CopyRoute(long id, [FromBody] CopyProcessRouteRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            var sourceRoute = await _routeRepository.GetByIdAsync(id);
            if (sourceRoute == null)
            {
                return NotFound(ApiResponse.Fail("源工艺路线不存在"));
            }

            // 检查新路线编码是否已存在
            var exists = await _routeRepository.ExistsAsync(r => r.RouteCode == request.NewRouteCode);
            if (exists)
            {
                return BadRequest(ApiResponse.Fail("新工艺路线编码已存在"));
            }

            // 创建新的工艺路线
            var newRoute = new ProcessRoute
            {
                RouteCode = request.NewRouteCode,
                RouteName = request.NewRouteName,
                ProductType = sourceRoute.ProductType,
                Description = request.Description ?? sourceRoute.Description,
                IsEnabled = false, // 复制的路线默认禁用
                VersionNumber = "1.0",
                RouteConfig = sourceRoute.RouteConfig,
                CreatedBy = User.GetUserIdAsLong()
            };

            var newRouteId = await _routeRepository.AddAsync(newRoute);

            // 复制工艺路线步骤
            var sourceSteps = await _stepRepository.GetListAsync(s => s.ProcessRouteId == id);
            foreach (var sourceStep in sourceSteps.OrderBy(s => s.StepOrder))
            {
                var newStep = new ProcessRouteStep
                {
                    ProcessRouteId = newRouteId,
                    ProcessId = sourceStep.ProcessId,
                    StepOrder = sourceStep.StepOrder,
                    IsRequired = sourceStep.IsRequired,
                    StepConfig = sourceStep.StepConfig,
                    CreatedBy = User.GetUserIdAsLong()
                };

                await _stepRepository.AddAsync(newStep);
            }

            await _auditService.LogAsync("CopyRoute", "ProcessRoute", newRouteId,
                $"复制工艺路线: 从 {sourceRoute.RouteCode} 复制到 {request.NewRouteCode}");

            _logger.LogInformation("用户 {UserId} 复制工艺路线 {SourceId} 到 {NewId}",
                User.GetUserIdAsLong(), id, newRouteId);

            return Ok(ApiResponse.OK(newRouteId, "工艺路线复制成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "复制工艺路线失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("复制工艺路线失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 更新工艺路线步骤
    /// </summary>
    /// <param name="id">路线ID</param>
    /// <param name="request">步骤更新请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/steps")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> UpdateRouteSteps(long id, [FromBody] UpdateRouteStepsRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            var route = await _routeRepository.GetByIdAsync(id);
            if (route == null)
            {
                return NotFound(ApiResponse.Fail("工艺路线不存在"));
            }

            // 删除现有步骤
            var existingSteps = await _stepRepository.GetListAsync(s => s.ProcessRouteId == id);
            foreach (var step in existingSteps)
            {
                await _stepRepository.DeleteAsync(step.Id);
            }

            // 创建新步骤
            foreach (var stepRequest in request.Steps)
            {
                var step = new ProcessRouteStep
                {
                    ProcessRouteId = id,
                    ProcessId = stepRequest.ProcessId,
                    StepOrder = stepRequest.StepOrder,
                    IsRequired = stepRequest.IsRequired,
                    StepConfig = stepRequest.StepConfig,
                    CreatedBy = User.GetUserIdAsLong()
                };

                await _stepRepository.AddAsync(step);
            }

            await _auditService.LogAsync("UpdateRouteSteps", "ProcessRoute", id,
                $"更新工艺路线步骤: {route.RouteCode}");

            _logger.LogInformation("用户 {UserId} 更新工艺路线 {RouteId} 的步骤",
                User.GetUserIdAsLong(), id);

            return Ok(ApiResponse.OK("工艺路线步骤更新成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新工艺路线步骤失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("更新工艺路线步骤失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取可用的工序列表
    /// </summary>
    /// <param name="processType">工序类型筛选</param>
    /// <returns>工序列表</returns>
    [HttpGet("available-processes")]
    [ProducesResponseType(typeof(ApiResponse<List<ProcessOptionDto>>), 200)]
    public async Task<IActionResult> GetAvailableProcesses([FromQuery] ProcessType? processType = null)
    {
        try
        {
            var processes = await _processRepository.GetListAsync(
                p => p.IsEnabled && (processType == null || p.ProcessType == processType));

            var processOptions = processes.Select(p => new ProcessOptionDto
            {
                Id = p.Id,
                ProcessCode = p.ProcessCode,
                ProcessName = p.ProcessName,
                ProcessType = p.ProcessType,
                StandardTime = p.StandardTime
            }).OrderBy(p => p.ProcessCode).ToList();

            return Ok(ApiResponse.OK(processOptions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用工序列表失败");
            return BadRequest(ApiResponse.Fail("获取可用工序列表失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 启用/禁用工艺路线
    /// </summary>
    /// <param name="id">路线ID</param>
    /// <param name="request">启用状态请求</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/toggle-status")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> ToggleRouteStatus(long id, [FromBody] ToggleRouteStatusRequest request)
    {
        try
        {
            var route = await _routeRepository.GetByIdAsync(id);
            if (route == null)
            {
                return NotFound(ApiResponse.Fail("工艺路线不存在"));
            }

            route.IsEnabled = request.IsEnabled;
            route.UpdatedBy = User.GetUserIdAsLong();
            route.UpdatedTime = DateTime.UtcNow;

            var success = await _routeRepository.UpdateAsync(route);

            if (success)
            {
                await _auditService.LogAsync("ToggleRouteStatus", "ProcessRoute", id,
                    $"{(request.IsEnabled ? "启用" : "禁用")}工艺路线: {route.RouteCode}");

                _logger.LogInformation("用户 {UserId} {Action} 工艺路线 {RouteId}",
                    User.GetUserIdAsLong(), request.IsEnabled ? "启用" : "禁用", id);

                return Ok(ApiResponse.OK($"工艺路线{(request.IsEnabled ? "启用" : "禁用")}成功"));
            }

            return BadRequest(ApiResponse.Fail("操作失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "切换工艺路线状态失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("操作失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取工艺路线统计信息
    /// </summary>
    /// <returns>统计信息</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ApiResponse<ProcessRouteStatisticsDto>), 200)]
    public async Task<IActionResult> GetRouteStatistics()
    {
        try
        {
            var allRoutes = await _routeRepository.GetAllAsync();
            var enabledRoutes = allRoutes.Where(r => r.IsEnabled).ToList();

            var statistics = new ProcessRouteStatisticsDto
            {
                TotalRoutes = allRoutes.Count,
                EnabledRoutes = enabledRoutes.Count,
                DisabledRoutes = allRoutes.Count - enabledRoutes.Count,
                ProductTypeDistribution = allRoutes.GroupBy(r => r.ProductType)
                    .ToDictionary(g => g.Key, g => g.Count()),
                RecentlyCreated = allRoutes.Where(r => r.CreatedTime >= DateTime.Now.AddDays(-7)).Count(),
                RecentlyUpdated = allRoutes.Where(r => r.UpdatedTime >= DateTime.Now.AddDays(-7)).Count()
            };

            return Ok(ApiResponse.OK(statistics));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工艺路线统计信息失败");
            return BadRequest(ApiResponse.Fail("获取统计信息失败: " + ex.Message));
        }
    }
}

#region DTO Models

/// <summary>
/// 工艺路线DTO
/// </summary>
public class ProcessRouteDto
{
    public long Id { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsEnabled { get; set; }
    public string VersionNumber { get; set; } = string.Empty;
    public int StepCount { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime? UpdatedTime { get; set; }
}

/// <summary>
/// 工艺路线详情DTO
/// </summary>
public class ProcessRouteDetailDto : ProcessRouteDto
{
    public string? RouteConfig { get; set; }
    public List<ProcessRouteStepDetailDto> Steps { get; set; } = new();
}

/// <summary>
/// 工艺路线步骤详情DTO
/// </summary>
public class ProcessRouteStepDetailDto
{
    public long Id { get; set; }
    public long ProcessId { get; set; }
    public string ProcessCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public ProcessType ProcessType { get; set; }
    public int StepOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? StepConfig { get; set; }
    public decimal StandardTime { get; set; }
}

/// <summary>
/// 创建工艺路线请求
/// </summary>
public class CreateProcessRouteRequest
{
    [Required(ErrorMessage = "路线编码不能为空")]
    [StringLength(50, ErrorMessage = "路线编码长度不能超过50个字符")]
    public string RouteCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "路线名称不能为空")]
    [StringLength(100, ErrorMessage = "路线名称长度不能超过100个字符")]
    public string RouteName { get; set; } = string.Empty;

    [Required(ErrorMessage = "产品类型不能为空")]
    [StringLength(50, ErrorMessage = "产品类型长度不能超过50个字符")]
    public string ProductType { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; } = true;

    [StringLength(20, ErrorMessage = "版本号长度不能超过20个字符")]
    public string? VersionNumber { get; set; }

    public string? RouteConfig { get; set; }

    public List<CreateProcessRouteStepRequest>? Steps { get; set; }
}

/// <summary>
/// 创建工艺路线步骤请求
/// </summary>
public class CreateProcessRouteStepRequest
{
    [Required(ErrorMessage = "工序ID不能为空")]
    public long ProcessId { get; set; }

    [Required(ErrorMessage = "步骤序号不能为空")]
    [Range(1, int.MaxValue, ErrorMessage = "步骤序号必须大于0")]
    public int StepOrder { get; set; }

    public bool IsRequired { get; set; } = true;

    public string? StepConfig { get; set; }
}

/// <summary>
/// 更新工艺路线请求
/// </summary>
public class UpdateProcessRouteRequest
{
    [Required(ErrorMessage = "路线编码不能为空")]
    [StringLength(50, ErrorMessage = "路线编码长度不能超过50个字符")]
    public string RouteCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "路线名称不能为空")]
    [StringLength(100, ErrorMessage = "路线名称长度不能超过100个字符")]
    public string RouteName { get; set; } = string.Empty;

    [Required(ErrorMessage = "产品类型不能为空")]
    [StringLength(50, ErrorMessage = "产品类型长度不能超过50个字符")]
    public string ProductType { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    public string? Description { get; set; }

    public bool IsEnabled { get; set; }

    [StringLength(20, ErrorMessage = "版本号长度不能超过20个字符")]
    public string? VersionNumber { get; set; }

    public string? RouteConfig { get; set; }
}

/// <summary>
/// 复制工艺路线请求
/// </summary>
public class CopyProcessRouteRequest
{
    [Required(ErrorMessage = "新路线编码不能为空")]
    [StringLength(50, ErrorMessage = "新路线编码长度不能超过50个字符")]
    public string NewRouteCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "新路线名称不能为空")]
    [StringLength(100, ErrorMessage = "新路线名称长度不能超过100个字符")]
    public string NewRouteName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "描述长度不能超过500个字符")]
    public string? Description { get; set; }
}

/// <summary>
/// 更新路线步骤请求
/// </summary>
public class UpdateRouteStepsRequest
{
    [Required(ErrorMessage = "步骤列表不能为空")]
    public List<CreateProcessRouteStepRequest> Steps { get; set; } = new();
}

/// <summary>
/// 工序选项DTO
/// </summary>
public class ProcessOptionDto
{
    public long Id { get; set; }
    public string ProcessCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public ProcessType ProcessType { get; set; }
    public decimal StandardTime { get; set; }
}

/// <summary>
/// 切换路线状态请求
/// </summary>
public class ToggleRouteStatusRequest
{
    [Required(ErrorMessage = "启用状态不能为空")]
    public bool IsEnabled { get; set; }
}

/// <summary>
/// 工艺路线统计DTO
/// </summary>
public class ProcessRouteStatisticsDto
{
    public int TotalRoutes { get; set; }
    public int EnabledRoutes { get; set; }
    public int DisabledRoutes { get; set; }
    public Dictionary<string, int> ProductTypeDistribution { get; set; } = new();
    public int RecentlyCreated { get; set; }
    public int RecentlyUpdated { get; set; }
}

#endregion 