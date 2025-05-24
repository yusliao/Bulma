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
/// 可视化工艺路线设计器控制器 V2.0
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/process-route-designer")]
[Authorize]
[Produces("application/json")]
public class ProcessRouteDesignerController : ControllerBase
{
    private readonly IRepository<ProcessRoute> _routeRepository;
    private readonly IRepository<Process> _processRepository;
    private readonly IAuditService _auditService;
    private readonly ILogger<ProcessRouteDesignerController> _logger;

    public ProcessRouteDesignerController(
        IRepository<ProcessRoute> routeRepository,
        IRepository<Process> processRepository,
        IAuditService auditService,
        ILogger<ProcessRouteDesignerController> logger)
    {
        _routeRepository = routeRepository;
        _processRepository = processRepository;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// 获取工艺路线设计器配置
    /// </summary>
    /// <param name="id">路线ID</param>
    /// <returns>设计器配置</returns>
    [HttpGet("{id}/designer-config")]
    [ProducesResponseType(typeof(ApiResponse<ProcessRouteDesignerConfigDto>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetDesignerConfig(long id)
    {
        try
        {
            var route = await _routeRepository.GetByIdAsync(id);
            if (route == null)
            {
                return NotFound(ApiResponse.Fail("工艺路线不存在"));
            }

            var config = new ProcessRouteDesignerConfigDto
            {
                RouteId = route.Id,
                RouteCode = route.RouteCode,
                RouteName = route.RouteName,
                ProductType = route.ProductType,
                FlowChartConfig = string.IsNullOrEmpty(route.RouteConfig) 
                    ? GetDefaultFlowChartConfig() 
                    : JsonSerializer.Deserialize<FlowChartConfigDto>(route.RouteConfig),
                AvailableProcesses = await GetAvailableProcessNodes()
            };

            return Ok(ApiResponse.OK(config));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工艺路线设计器配置失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("获取设计器配置失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 保存工艺路线设计器配置
    /// </summary>
    /// <param name="id">路线ID</param>
    /// <param name="request">设计器配置</param>
    /// <returns>操作结果</returns>
    [HttpPut("{id}/designer-config")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> SaveDesignerConfig(long id, [FromBody] SaveDesignerConfigRequest request)
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

            // 验证流程图配置
            var validationResult = ValidateFlowChartConfig(request.FlowChartConfig);
            if (!validationResult.IsValid)
            {
                return BadRequest(ApiResponse.Fail($"流程图配置无效: {validationResult.ErrorMessage}"));
            }

            // 保存配置
            route.RouteConfig = JsonSerializer.Serialize(request.FlowChartConfig);
            route.UpdatedBy = User.GetUserIdAsLong();
            route.UpdatedTime = DateTime.UtcNow;

            var success = await _routeRepository.UpdateAsync(route);

            if (success)
            {
                await _auditService.LogAsync("SaveDesignerConfig", "ProcessRoute", id,
                    $"保存工艺路线设计器配置: {route.RouteCode}");

                _logger.LogInformation("用户 {UserId} 保存工艺路线 {RouteId} 的设计器配置",
                    User.GetUserIdAsLong(), id);

                return Ok(ApiResponse.OK("设计器配置保存成功"));
            }

            return BadRequest(ApiResponse.Fail("设计器配置保存失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存工艺路线设计器配置失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("保存设计器配置失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 验证工艺路线流程图
    /// </summary>
    /// <param name="request">验证请求</param>
    /// <returns>验证结果</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ApiResponse<FlowChartValidationResultDto>), 200)]
    public async Task<IActionResult> ValidateFlowChart([FromBody] ValidateFlowChartRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            var validationResult = await ValidateFlowChartConfigAsync(request.FlowChartConfig);

            return Ok(ApiResponse.OK(validationResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证工艺路线流程图失败");
            return BadRequest(ApiResponse.Fail("验证流程图失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取工艺路线模板
    /// </summary>
    /// <param name="productType">产品类型</param>
    /// <returns>模板列表</returns>
    [HttpGet("templates")]
    [ProducesResponseType(typeof(ApiResponse<List<ProcessRouteTemplateDto>>), 200)]
    public async Task<IActionResult> GetRouteTemplates([FromQuery] string? productType = null)
    {
        try
        {
            var templates = GetPredefinedTemplates();

            if (!string.IsNullOrEmpty(productType))
            {
                templates = templates.Where(t => t.ProductType == productType).ToList();
            }

            return Ok(ApiResponse.OK(templates));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工艺路线模板失败");
            return BadRequest(ApiResponse.Fail("获取模板失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 应用工艺路线模板
    /// </summary>
    /// <param name="id">路线ID</param>
    /// <param name="request">应用模板请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/apply-template")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> ApplyTemplate(long id, [FromBody] ApplyTemplateRequest request)
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

            var template = GetTemplateById(request.TemplateId);
            if (template == null)
            {
                return BadRequest(ApiResponse.Fail("模板不存在"));
            }

            // 应用模板配置
            route.RouteConfig = JsonSerializer.Serialize(template.FlowChartConfig);
            route.UpdatedBy = User.GetUserIdAsLong();
            route.UpdatedTime = DateTime.UtcNow;

            var success = await _routeRepository.UpdateAsync(route);

            if (success)
            {
                await _auditService.LogAsync("ApplyTemplate", "ProcessRoute", id,
                    $"应用工艺路线模板: {template.TemplateName}");

                _logger.LogInformation("用户 {UserId} 为工艺路线 {RouteId} 应用模板 {TemplateId}",
                    User.GetUserIdAsLong(), id, request.TemplateId);

                return Ok(ApiResponse.OK("模板应用成功"));
            }

            return BadRequest(ApiResponse.Fail("模板应用失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "应用工艺路线模板失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("应用模板失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 导出工艺路线配置
    /// </summary>
    /// <param name="id">路线ID</param>
    /// <param name="format">导出格式</param>
    /// <returns>导出文件</returns>
    [HttpGet("{id}/export")]
    [ProducesResponseType(typeof(FileResult), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> ExportRouteConfig(long id, [FromQuery] string format = "json")
    {
        try
        {
            var route = await _routeRepository.GetByIdAsync(id);
            if (route == null)
            {
                return NotFound(ApiResponse.Fail("工艺路线不存在"));
            }

            var fileName = $"ProcessRoute_{route.RouteCode}_{DateTime.Now:yyyyMMdd}";
            
            switch (format.ToLower())
            {
                case "json":
                    var jsonContent = JsonSerializer.Serialize(route, new JsonSerializerOptions 
                    { 
                        WriteIndented = true 
                    });
                    var jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonContent);
                    return File(jsonBytes, "application/json", $"{fileName}.json");

                case "xml":
                    // 这里可以实现XML导出逻辑
                    return BadRequest(ApiResponse.Fail("XML格式暂不支持"));

                default:
                    return BadRequest(ApiResponse.Fail("不支持的导出格式"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导出工艺路线配置失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("导出配置失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 导入工艺路线配置
    /// </summary>
    /// <param name="id">路线ID</param>
    /// <param name="file">配置文件</param>
    /// <returns>操作结果</returns>
    [HttpPost("{id}/import")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> ImportRouteConfig(long id, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(ApiResponse.Fail("请选择要导入的文件"));
            }

            var route = await _routeRepository.GetByIdAsync(id);
            if (route == null)
            {
                return NotFound(ApiResponse.Fail("工艺路线不存在"));
            }

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();

            // 验证和解析配置
            FlowChartConfigDto? config;
            try
            {
                config = JsonSerializer.Deserialize<FlowChartConfigDto>(content);
                if (config == null)
                {
                    return BadRequest(ApiResponse.Fail("无效的配置文件格式"));
                }
            }
            catch (JsonException)
            {
                return BadRequest(ApiResponse.Fail("配置文件格式错误"));
            }

            // 验证配置有效性
            var validationResult = await ValidateFlowChartConfigAsync(config);
            if (!validationResult.IsValid)
            {
                return BadRequest(ApiResponse.Fail($"配置验证失败: {string.Join(", ", validationResult.Errors)}"));
            }

            // 保存配置
            route.RouteConfig = content;
            route.UpdatedBy = User.GetUserIdAsLong();
            route.UpdatedTime = DateTime.UtcNow;

            var success = await _routeRepository.UpdateAsync(route);

            if (success)
            {
                await _auditService.LogAsync("ImportConfig", "ProcessRoute", id,
                    $"导入工艺路线配置: {file.FileName}");

                _logger.LogInformation("用户 {UserId} 为工艺路线 {RouteId} 导入配置文件 {FileName}",
                    User.GetUserIdAsLong(), id, file.FileName);

                return Ok(ApiResponse.OK("配置导入成功"));
            }

            return BadRequest(ApiResponse.Fail("配置导入失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入工艺路线配置失败: {RouteId}", id);
            return BadRequest(ApiResponse.Fail("导入配置失败: " + ex.Message));
        }
    }

    #region Private Methods

    /// <summary>
    /// 获取默认流程图配置
    /// </summary>
    /// <returns>默认配置</returns>
    private static FlowChartConfigDto GetDefaultFlowChartConfig()
    {
        return new FlowChartConfigDto
        {
            Nodes = new List<FlowChartNodeDto>
            {
                new FlowChartNodeDto
                {
                    Id = "start",
                    Type = "start",
                    Label = "开始",
                    Position = new PositionDto { X = 100, Y = 100 }
                },
                new FlowChartNodeDto
                {
                    Id = "end",
                    Type = "end",
                    Label = "结束",
                    Position = new PositionDto { X = 500, Y = 100 }
                }
            },
            Edges = new List<FlowChartEdgeDto>(),
            ViewPort = new ViewPortDto { X = 0, Y = 0, Zoom = 1 }
        };
    }

    /// <summary>
    /// 获取可用的工序节点
    /// </summary>
    /// <returns>工序节点列表</returns>
    private async Task<List<ProcessNodeDto>> GetAvailableProcessNodes()
    {
        var processes = await _processRepository.GetListAsync(p => p.IsEnabled);
        
        return processes.Select(p => new ProcessNodeDto
        {
            Id = p.Id.ToString(),
            ProcessId = p.Id,
            ProcessCode = p.ProcessCode,
            ProcessName = p.ProcessName,
            ProcessType = p.ProcessType,
            StandardTime = p.StandardTime,
            Icon = GetProcessTypeIcon(p.ProcessType),
            Color = GetProcessTypeColor(p.ProcessType)
        }).ToList();
    }

    /// <summary>
    /// 验证流程图配置
    /// </summary>
    /// <param name="config">流程图配置</param>
    /// <returns>验证结果</returns>
    private ValidationResult ValidateFlowChartConfig(FlowChartConfigDto config)
    {
        if (config == null)
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "配置不能为空" };
        }

        if (config.Nodes == null || !config.Nodes.Any())
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "至少需要一个节点" };
        }

        // 检查是否有开始节点
        if (!config.Nodes.Any(n => n.Type == "start"))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "缺少开始节点" };
        }

        // 检查是否有结束节点
        if (!config.Nodes.Any(n => n.Type == "end"))
        {
            return new ValidationResult { IsValid = false, ErrorMessage = "缺少结束节点" };
        }

        return new ValidationResult { IsValid = true };
    }

    /// <summary>
    /// 验证流程图配置（异步版本）
    /// </summary>
    /// <param name="config">流程图配置</param>
    /// <returns>验证结果</returns>
    private async Task<FlowChartValidationResultDto> ValidateFlowChartConfigAsync(FlowChartConfigDto config)
    {
        var result = new FlowChartValidationResultDto { IsValid = true, Errors = new List<string>() };

        if (config == null)
        {
            result.IsValid = false;
            result.Errors.Add("配置不能为空");
            return result;
        }

        if (config.Nodes == null || !config.Nodes.Any())
        {
            result.IsValid = false;
            result.Errors.Add("至少需要一个节点");
            return result;
        }

        // 检查开始节点
        var startNodes = config.Nodes.Where(n => n.Type == "start").ToList();
        if (!startNodes.Any())
        {
            result.IsValid = false;
            result.Errors.Add("缺少开始节点");
        }
        else if (startNodes.Count > 1)
        {
            result.IsValid = false;
            result.Errors.Add("只能有一个开始节点");
        }

        // 检查结束节点
        if (!config.Nodes.Any(n => n.Type == "end"))
        {
            result.IsValid = false;
            result.Errors.Add("缺少结束节点");
        }

        // 检查工序节点是否存在
        var processNodes = config.Nodes.Where(n => n.Type == "process").ToList();
        foreach (var processNode in processNodes)
        {
            if (processNode.ProcessId.HasValue)
            {
                var processExists = await _processRepository.ExistsAsync(p => p.Id == processNode.ProcessId.Value);
                if (!processExists)
                {
                    result.IsValid = false;
                    result.Errors.Add($"工序 ID {processNode.ProcessId} 不存在");
                }
            }
        }

        // 检查节点连接
        if (config.Edges != null)
        {
            foreach (var edge in config.Edges)
            {
                var sourceExists = config.Nodes.Any(n => n.Id == edge.Source);
                var targetExists = config.Nodes.Any(n => n.Id == edge.Target);

                if (!sourceExists)
                {
                    result.IsValid = false;
                    result.Errors.Add($"连接的源节点 {edge.Source} 不存在");
                }

                if (!targetExists)
                {
                    result.IsValid = false;
                    result.Errors.Add($"连接的目标节点 {edge.Target} 不存在");
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 获取预定义模板
    /// </summary>
    /// <returns>模板列表</returns>
    private static List<ProcessRouteTemplateDto> GetPredefinedTemplates()
    {
        return new List<ProcessRouteTemplateDto>
        {
            new ProcessRouteTemplateDto
            {
                Id = "template_cell_packing",
                TemplateName = "电芯包装标准工艺",
                ProductType = "电芯",
                Description = "适用于标准电芯包装流程",
                FlowChartConfig = CreateCellPackingTemplate()
            },
            new ProcessRouteTemplateDto
            {
                Id = "template_module_packing",
                TemplateName = "模组包装标准工艺",
                ProductType = "模组",
                Description = "适用于标准模组包装流程",
                FlowChartConfig = CreateModulePackingTemplate()
            }
        };
    }

    /// <summary>
    /// 根据ID获取模板
    /// </summary>
    /// <param name="templateId">模板ID</param>
    /// <returns>模板</returns>
    private static ProcessRouteTemplateDto? GetTemplateById(string templateId)
    {
        return GetPredefinedTemplates().FirstOrDefault(t => t.Id == templateId);
    }

    /// <summary>
    /// 创建电芯包装模板
    /// </summary>
    /// <returns>流程图配置</returns>
    private static FlowChartConfigDto CreateCellPackingTemplate()
    {
        return new FlowChartConfigDto
        {
            Nodes = new List<FlowChartNodeDto>
            {
                new FlowChartNodeDto { Id = "start", Type = "start", Label = "开始", Position = new PositionDto { X = 100, Y = 100 } },
                new FlowChartNodeDto { Id = "inspect", Type = "process", Label = "检验", Position = new PositionDto { X = 250, Y = 100 } },
                new FlowChartNodeDto { Id = "pack", Type = "process", Label = "包装", Position = new PositionDto { X = 400, Y = 100 } },
                new FlowChartNodeDto { Id = "end", Type = "end", Label = "结束", Position = new PositionDto { X = 550, Y = 100 } }
            },
            Edges = new List<FlowChartEdgeDto>
            {
                new FlowChartEdgeDto { Id = "e1", Source = "start", Target = "inspect" },
                new FlowChartEdgeDto { Id = "e2", Source = "inspect", Target = "pack" },
                new FlowChartEdgeDto { Id = "e3", Source = "pack", Target = "end" }
            },
            ViewPort = new ViewPortDto { X = 0, Y = 0, Zoom = 1 }
        };
    }

    /// <summary>
    /// 创建模组包装模板
    /// </summary>
    /// <returns>流程图配置</returns>
    private static FlowChartConfigDto CreateModulePackingTemplate()
    {
        return new FlowChartConfigDto
        {
            Nodes = new List<FlowChartNodeDto>
            {
                new FlowChartNodeDto { Id = "start", Type = "start", Label = "开始", Position = new PositionDto { X = 100, Y = 100 } },
                new FlowChartNodeDto { Id = "assembly", Type = "process", Label = "组装", Position = new PositionDto { X = 250, Y = 100 } },
                new FlowChartNodeDto { Id = "test", Type = "process", Label = "测试", Position = new PositionDto { X = 400, Y = 100 } },
                new FlowChartNodeDto { Id = "pack", Type = "process", Label = "包装", Position = new PositionDto { X = 550, Y = 100 } },
                new FlowChartNodeDto { Id = "end", Type = "end", Label = "结束", Position = new PositionDto { X = 700, Y = 100 } }
            },
            Edges = new List<FlowChartEdgeDto>
            {
                new FlowChartEdgeDto { Id = "e1", Source = "start", Target = "assembly" },
                new FlowChartEdgeDto { Id = "e2", Source = "assembly", Target = "test" },
                new FlowChartEdgeDto { Id = "e3", Source = "test", Target = "pack" },
                new FlowChartEdgeDto { Id = "e4", Source = "pack", Target = "end" }
            },
            ViewPort = new ViewPortDto { X = 0, Y = 0, Zoom = 1 }
        };
    }

    /// <summary>
    /// 获取工序类型图标
    /// </summary>
    /// <param name="processType">工序类型</param>
    /// <returns>图标名称</returns>
    private static string GetProcessTypeIcon(ProcessType processType)
    {
        return processType switch
        {
            ProcessType.CellPacking => "battery",
            ProcessType.ModulePacking => "module",
            ProcessType.PackPacking => "package",
            ProcessType.PalletPacking => "pallet",
            _ => "process"
        };
    }

    /// <summary>
    /// 获取工序类型颜色
    /// </summary>
    /// <param name="processType">工序类型</param>
    /// <returns>颜色值</returns>
    private static string GetProcessTypeColor(ProcessType processType)
    {
        return processType switch
        {
            ProcessType.CellPacking => "#FF6B6B",
            ProcessType.ModulePacking => "#4ECDC4",
            ProcessType.PackPacking => "#45B7D1",
            ProcessType.PalletPacking => "#96CEB4",
            _ => "#95A5A6"
        };
    }

    #endregion
}

#region Designer DTO Models

/// <summary>
/// 工艺路线设计器配置DTO
/// </summary>
public class ProcessRouteDesignerConfigDto
{
    public long RouteId { get; set; }
    public string RouteCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public FlowChartConfigDto? FlowChartConfig { get; set; }
    public List<ProcessNodeDto> AvailableProcesses { get; set; } = new();
}

/// <summary>
/// 流程图配置DTO
/// </summary>
public class FlowChartConfigDto
{
    public List<FlowChartNodeDto> Nodes { get; set; } = new();
    public List<FlowChartEdgeDto> Edges { get; set; } = new();
    public ViewPortDto ViewPort { get; set; } = new();
}

/// <summary>
/// 流程图节点DTO
/// </summary>
public class FlowChartNodeDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // start, end, process, decision
    public string Label { get; set; } = string.Empty;
    public PositionDto Position { get; set; } = new();
    public long? ProcessId { get; set; }
    public Dictionary<string, object>? Data { get; set; }
    public NodeStyleDto? Style { get; set; }
}

/// <summary>
/// 流程图连接线DTO
/// </summary>
public class FlowChartEdgeDto
{
    public string Id { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string? Label { get; set; }
    public EdgeStyleDto? Style { get; set; }
    public Dictionary<string, object>? Data { get; set; }
}

/// <summary>
/// 位置DTO
/// </summary>
public class PositionDto
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// 视口DTO
/// </summary>
public class ViewPortDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Zoom { get; set; } = 1;
}

/// <summary>
/// 节点样式DTO
/// </summary>
public class NodeStyleDto
{
    public string? BackgroundColor { get; set; }
    public string? BorderColor { get; set; }
    public string? TextColor { get; set; }
    public double? BorderWidth { get; set; }
    public double? BorderRadius { get; set; }
}

/// <summary>
/// 连接线样式DTO
/// </summary>
public class EdgeStyleDto
{
    public string? Stroke { get; set; }
    public double? StrokeWidth { get; set; }
    public string? StrokeDasharray { get; set; }
}

/// <summary>
/// 工序节点DTO
/// </summary>
public class ProcessNodeDto
{
    public string Id { get; set; } = string.Empty;
    public long ProcessId { get; set; }
    public string ProcessCode { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public ProcessType ProcessType { get; set; }
    public decimal StandardTime { get; set; }
    public string Icon { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}

/// <summary>
/// 保存设计器配置请求
/// </summary>
public class SaveDesignerConfigRequest
{
    [Required(ErrorMessage = "流程图配置不能为空")]
    public FlowChartConfigDto FlowChartConfig { get; set; } = new();
}

/// <summary>
/// 验证流程图请求
/// </summary>
public class ValidateFlowChartRequest
{
    [Required(ErrorMessage = "流程图配置不能为空")]
    public FlowChartConfigDto FlowChartConfig { get; set; } = new();
}

/// <summary>
/// 流程图验证结果DTO
/// </summary>
public class FlowChartValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

/// <summary>
/// 工艺路线模板DTO
/// </summary>
public class ProcessRouteTemplateDto
{
    public string Id { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public string ProductType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public FlowChartConfigDto FlowChartConfig { get; set; } = new();
}

/// <summary>
/// 应用模板请求
/// </summary>
public class ApplyTemplateRequest
{
    [Required(ErrorMessage = "模板ID不能为空")]
    public string TemplateId { get; set; } = string.Empty;
}

/// <summary>
/// 验证结果
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion 