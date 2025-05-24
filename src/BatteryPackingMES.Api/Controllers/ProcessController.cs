using Microsoft.AspNetCore.Mvc;
using BatteryPackingMES.Api.Models;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Enums;

namespace BatteryPackingMES.Api.Controllers;

/// <summary>
/// 工序管理控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProcessController : ControllerBase
{
    private readonly IRepository<Process> _processRepository;
    private readonly ILogger<ProcessController> _logger;

    public ProcessController(IRepository<Process> processRepository, ILogger<ProcessController> logger)
    {
        _processRepository = processRepository;
        _logger = logger;
    }

    /// <summary>
    /// 获取所有工序
    /// </summary>
    /// <returns>工序列表</returns>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<Process>>>> GetAllProcesses()
    {
        try
        {
            var processes = await _processRepository.GetAllAsync();
            return Ok(ApiResponse<List<Process>>.Ok(processes, "获取工序列表成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工序列表失败");
            return StatusCode(500, ApiResponse<List<Process>>.Fail("获取工序列表失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 根据ID获取工序
    /// </summary>
    /// <param name="id">工序ID</param>
    /// <returns>工序信息</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<Process>>> GetProcessById(long id)
    {
        try
        {
            var process = await _processRepository.GetByIdAsync(id);
            if (process == null)
            {
                return NotFound(ApiResponse<Process>.Fail("工序不存在", "NOT_FOUND"));
            }

            return Ok(ApiResponse<Process>.Ok(process, "获取工序信息成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工序信息失败，ID: {Id}", id);
            return StatusCode(500, ApiResponse<Process>.Fail("获取工序信息失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 分页获取工序列表
    /// </summary>
    /// <param name="pageIndex">页码</param>
    /// <param name="pageSize">页大小</param>
    /// <param name="processType">工序类型</param>
    /// <param name="isEnabled">是否启用</param>
    /// <returns>分页工序列表</returns>
    [HttpGet("paged")]
    public async Task<ActionResult<ApiResponse<PagedResult<Process>>>> GetPagedProcesses(
        int pageIndex = 1, 
        int pageSize = 10, 
        ProcessType? processType = null, 
        bool? isEnabled = null)
    {
        try
        {
            if (pageIndex < 1) pageIndex = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var (items, total) = await _processRepository.GetPagedAsync(
                pageIndex, 
                pageSize, 
                p => (processType == null || p.ProcessType == processType) &&
                     (isEnabled == null || p.IsEnabled == isEnabled));

            var response = new PagedResult<Process>
            {
                Items = items,
                Total = total,
                PageIndex = pageIndex,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<Process>>.Ok(response, "获取分页工序列表成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取分页工序列表失败");
            return StatusCode(500, ApiResponse<PagedResult<Process>>.Fail("获取分页工序列表失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 创建工序
    /// </summary>
    /// <param name="process">工序信息</param>
    /// <returns>创建结果</returns>
    [HttpPost]
    public async Task<ActionResult<ApiResponse<long>>> CreateProcess([FromBody] Process process)
    {
        try
        {
            // 检查工序编码是否已存在
            var exists = await _processRepository.ExistsAsync(p => p.ProcessCode == process.ProcessCode);
            if (exists)
            {
                return BadRequest(ApiResponse<long>.Fail("工序编码已存在", "DUPLICATE_CODE"));
            }

            var id = await _processRepository.AddAsync(process);
            return Ok(ApiResponse<long>.Ok(id, "创建工序成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建工序失败");
            return StatusCode(500, ApiResponse<long>.Fail("创建工序失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 更新工序
    /// </summary>
    /// <param name="id">工序ID</param>
    /// <param name="process">工序信息</param>
    /// <returns>更新结果</returns>
    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse>> UpdateProcess(long id, [FromBody] Process process)
    {
        try
        {
            var existingProcess = await _processRepository.GetByIdAsync(id);
            if (existingProcess == null)
            {
                return NotFound(ApiResponse.Fail("工序不存在", "NOT_FOUND"));
            }

            // 检查工序编码是否与其他工序重复
            var duplicateExists = await _processRepository.ExistsAsync(
                p => p.ProcessCode == process.ProcessCode && p.Id != id);
            if (duplicateExists)
            {
                return BadRequest(ApiResponse.Fail("工序编码已存在", "DUPLICATE_CODE"));
            }

            process.Id = id;
            process.CreatedTime = existingProcess.CreatedTime;
            process.CreatedBy = existingProcess.CreatedBy;
            process.Version = existingProcess.Version;

            var success = await _processRepository.UpdateAsync(process);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail("更新工序失败，可能数据已被其他用户修改", "CONCURRENT_UPDATE"));
            }

            return Ok(ApiResponse.Ok("更新工序成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新工序失败，ID: {Id}", id);
            return StatusCode(500, ApiResponse.Fail("更新工序失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 删除工序
    /// </summary>
    /// <param name="id">工序ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("{id}")]
    public async Task<ActionResult<ApiResponse>> DeleteProcess(long id)
    {
        try
        {
            var exists = await _processRepository.ExistsAsync(p => p.Id == id);
            if (!exists)
            {
                return NotFound(ApiResponse.Fail("工序不存在", "NOT_FOUND"));
            }

            var success = await _processRepository.DeleteAsync(id);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail("删除工序失败", "DELETE_FAILED"));
            }

            return Ok(ApiResponse.Ok("删除工序成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除工序失败，ID: {Id}", id);
            return StatusCode(500, ApiResponse.Fail("删除工序失败", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// 批量删除工序
    /// </summary>
    /// <param name="ids">工序ID列表</param>
    /// <returns>删除结果</returns>
    [HttpDelete("batch")]
    public async Task<ActionResult<ApiResponse>> BatchDeleteProcesses([FromBody] List<long> ids)
    {
        try
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest(ApiResponse.Fail("请选择要删除的工序", "INVALID_INPUT"));
            }

            var success = await _processRepository.DeleteRangeAsync(ids);
            if (!success)
            {
                return BadRequest(ApiResponse.Fail("批量删除工序失败", "DELETE_FAILED"));
            }

            return Ok(ApiResponse.Ok($"成功删除 {ids.Count} 个工序"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "批量删除工序失败");
            return StatusCode(500, ApiResponse.Fail("批量删除工序失败", "INTERNAL_ERROR"));
        }
    }
} 