using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Api.Extensions;
using BatteryPackingMES.Api.Models;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 库存管理控制器
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/inventory")]
[Authorize]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<InventoryController> _logger;

    public InventoryController(IInventoryService inventoryService, ILogger<InventoryController> logger)
    {
        _inventoryService = inventoryService;
        _logger = logger;
    }

    #region 仓库管理

    /// <summary>
    /// 创建仓库
    /// </summary>
    /// <param name="warehouse">仓库信息</param>
    /// <returns>仓库ID</returns>
    [HttpPost("warehouses")]
    [ProducesResponseType(typeof(ApiResponse<long>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CreateWarehouse([FromBody] Warehouse warehouse)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            warehouse.CreatedBy = User.GetUserIdAsLong();
            var warehouseId = await _inventoryService.CreateWarehouseAsync(warehouse);

            _logger.LogInformation("用户 {UserId} 创建仓库 {WarehouseCode}", User.GetUserIdAsLong(), warehouse.WarehouseCode);
            return Ok(ApiResponse.OK(warehouseId, "仓库创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建仓库失败");
            return BadRequest(ApiResponse.Fail("创建仓库失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取仓库列表
    /// </summary>
    /// <returns>仓库列表</returns>
    [HttpGet("warehouses")]
    [ProducesResponseType(typeof(ApiResponse<List<Warehouse>>), 200)]
    public async Task<IActionResult> GetWarehouses()
    {
        try
        {
            var warehouses = await _inventoryService.GetWarehousesAsync();
            return Ok(ApiResponse.OK(warehouses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取仓库列表失败");
            return BadRequest(ApiResponse.Fail("获取仓库列表失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 根据ID获取仓库信息
    /// </summary>
    /// <param name="id">仓库ID</param>
    /// <returns>仓库信息</returns>
    [HttpGet("warehouses/{id}")]
    [ProducesResponseType(typeof(ApiResponse<Warehouse>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetWarehouse(long id)
    {
        try
        {
            var warehouse = await _inventoryService.GetWarehouseByIdAsync(id);
            if (warehouse == null)
            {
                return NotFound(ApiResponse.Fail("仓库不存在"));
            }

            return Ok(ApiResponse.OK(warehouse));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取仓库信息失败: {WarehouseId}", id);
            return BadRequest(ApiResponse.Fail("获取仓库信息失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 更新仓库信息
    /// </summary>
    /// <param name="id">仓库ID</param>
    /// <param name="warehouse">仓库信息</param>
    /// <returns>更新结果</returns>
    [HttpPut("warehouses/{id}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> UpdateWarehouse(long id, [FromBody] Warehouse warehouse)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            warehouse.Id = id;
            warehouse.UpdatedBy = User.GetUserIdAsLong();
            var success = await _inventoryService.UpdateWarehouseAsync(warehouse);

            if (success)
            {
                _logger.LogInformation("用户 {UserId} 更新仓库 {WarehouseId}", User.GetUserIdAsLong(), id);
                return Ok(ApiResponse.OK("仓库更新成功"));
            }

            return BadRequest(ApiResponse.Fail("仓库更新失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新仓库失败: {WarehouseId}", id);
            return BadRequest(ApiResponse.Fail("更新仓库失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 删除仓库
    /// </summary>
    /// <param name="id">仓库ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("warehouses/{id}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> DeleteWarehouse(long id)
    {
        try
        {
            var success = await _inventoryService.DeleteWarehouseAsync(id);
            if (success)
            {
                _logger.LogInformation("用户 {UserId} 删除仓库 {WarehouseId}", User.GetUserIdAsLong(), id);
                return Ok(ApiResponse.OK("仓库删除成功"));
            }

            return BadRequest(ApiResponse.Fail("仓库删除失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除仓库失败: {WarehouseId}", id);
            return BadRequest(ApiResponse.Fail("删除仓库失败: " + ex.Message));
        }
    }

    #endregion

    #region 库位管理

    /// <summary>
    /// 创建库位
    /// </summary>
    /// <param name="location">库位信息</param>
    /// <returns>库位ID</returns>
    [HttpPost("locations")]
    [ProducesResponseType(typeof(ApiResponse<long>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> CreateLocation([FromBody] WarehouseLocation location)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            location.CreatedBy = User.GetUserIdAsLong();
            var locationId = await _inventoryService.CreateLocationAsync(location);

            _logger.LogInformation("用户 {UserId} 创建库位 {LocationCode}", User.GetUserIdAsLong(), location.LocationCode);
            return Ok(ApiResponse.OK(locationId, "库位创建成功"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建库位失败");
            return BadRequest(ApiResponse.Fail("创建库位失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取仓库的库位列表
    /// </summary>
    /// <param name="warehouseId">仓库ID</param>
    /// <returns>库位列表</returns>
    [HttpGet("locations/warehouse/{warehouseId}")]
    [ProducesResponseType(typeof(ApiResponse<List<WarehouseLocation>>), 200)]
    public async Task<IActionResult> GetLocationsByWarehouse(long warehouseId)
    {
        try
        {
            var locations = await _inventoryService.GetLocationsByWarehouseAsync(warehouseId);
            return Ok(ApiResponse.OK(locations));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取库位列表失败: {WarehouseId}", warehouseId);
            return BadRequest(ApiResponse.Fail("获取库位列表失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取可用库位列表
    /// </summary>
    /// <param name="warehouseId">仓库ID</param>
    /// <returns>可用库位列表</returns>
    [HttpGet("locations/available/{warehouseId}")]
    [ProducesResponseType(typeof(ApiResponse<List<WarehouseLocation>>), 200)]
    public async Task<IActionResult> GetAvailableLocations(long warehouseId)
    {
        try
        {
            var locations = await _inventoryService.GetAvailableLocationsAsync(warehouseId);
            return Ok(ApiResponse.OK(locations));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取可用库位列表失败: {WarehouseId}", warehouseId);
            return BadRequest(ApiResponse.Fail("获取可用库位列表失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 根据ID获取库位信息
    /// </summary>
    /// <param name="id">库位ID</param>
    /// <returns>库位信息</returns>
    [HttpGet("locations/{id}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseLocation>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetLocation(long id)
    {
        try
        {
            var location = await _inventoryService.GetLocationByIdAsync(id);
            if (location == null)
            {
                return NotFound(ApiResponse.Fail("库位不存在"));
            }

            return Ok(ApiResponse.OK(location));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取库位信息失败: {LocationId}", id);
            return BadRequest(ApiResponse.Fail("获取库位信息失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 更新库位信息
    /// </summary>
    /// <param name="id">库位ID</param>
    /// <param name="location">库位信息</param>
    /// <returns>更新结果</returns>
    [HttpPut("locations/{id}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> UpdateLocation(long id, [FromBody] WarehouseLocation location)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            location.Id = id;
            location.UpdatedBy = User.GetUserIdAsLong();
            var success = await _inventoryService.UpdateLocationAsync(location);

            if (success)
            {
                _logger.LogInformation("用户 {UserId} 更新库位 {LocationId}", User.GetUserIdAsLong(), id);
                return Ok(ApiResponse.OK("库位更新成功"));
            }

            return BadRequest(ApiResponse.Fail("库位更新失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新库位失败: {LocationId}", id);
            return BadRequest(ApiResponse.Fail("更新库位失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 删除库位
    /// </summary>
    /// <param name="id">库位ID</param>
    /// <returns>删除结果</returns>
    [HttpDelete("locations/{id}")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> DeleteLocation(long id)
    {
        try
        {
            var success = await _inventoryService.DeleteLocationAsync(id);
            if (success)
            {
                _logger.LogInformation("用户 {UserId} 删除库位 {LocationId}", User.GetUserIdAsLong(), id);
                return Ok(ApiResponse.OK("库位删除成功"));
            }

            return BadRequest(ApiResponse.Fail("库位删除失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除库位失败: {LocationId}", id);
            return BadRequest(ApiResponse.Fail("删除库位失败: " + ex.Message));
        }
    }

    #endregion

    #region 库存操作

    /// <summary>
    /// 入库操作
    /// </summary>
    /// <param name="request">入库请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("inbound")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> Inbound([FromBody] InboundRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            request.OperatorId = User.GetUserIdAsLong() ?? 0;
            request.OperatorName = User.GetUserName();

            var success = await _inventoryService.InboundAsync(request);
            if (success)
            {
                _logger.LogInformation("用户 {UserId} 执行入库操作: {MaterialCode}, 数量: {Quantity}", 
                    User.GetUserIdAsLong(), request.MaterialCode, request.Quantity);
                return Ok(ApiResponse.OK("入库操作成功"));
            }

            return BadRequest(ApiResponse.Fail("入库操作失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "入库操作失败");
            return BadRequest(ApiResponse.Fail("入库操作失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 出库操作
    /// </summary>
    /// <param name="request">出库请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("outbound")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> Outbound([FromBody] OutboundRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            request.OperatorId = User.GetUserIdAsLong() ?? 0;
            request.OperatorName = User.GetUserName();

            var success = await _inventoryService.OutboundAsync(request);
            if (success)
            {
                _logger.LogInformation("用户 {UserId} 执行出库操作: {MaterialCode}, 数量: {Quantity}", 
                    User.GetUserIdAsLong(), request.MaterialCode, request.Quantity);
                return Ok(ApiResponse.OK("出库操作成功"));
            }

            return BadRequest(ApiResponse.Fail("出库操作失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出库操作失败");
            return BadRequest(ApiResponse.Fail("出库操作失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 移库操作
    /// </summary>
    /// <param name="request">移库请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            request.OperatorId = User.GetUserIdAsLong() ?? 0;
            request.OperatorName = User.GetUserName();

            var success = await _inventoryService.TransferAsync(request);
            if (success)
            {
                _logger.LogInformation("用户 {UserId} 执行移库操作: {MaterialCode}, 数量: {Quantity}", 
                    User.GetUserIdAsLong(), request.MaterialCode, request.Quantity);
                return Ok(ApiResponse.OK("移库操作成功"));
            }

            return BadRequest(ApiResponse.Fail("移库操作失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移库操作失败");
            return BadRequest(ApiResponse.Fail("移库操作失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 库存调整
    /// </summary>
    /// <param name="request">调整请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("adjust")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> AdjustInventory([FromBody] AdjustmentRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            request.OperatorId = User.GetUserIdAsLong() ?? 0;
            request.OperatorName = User.GetUserName();

            var success = await _inventoryService.AdjustInventoryAsync(request);
            if (success)
            {
                _logger.LogInformation("用户 {UserId} 执行库存调整: {InventoryRecordId}, 新数量 {NewQuantity}", 
                    User.GetUserIdAsLong(), request.InventoryRecordId, request.NewQuantity);
                return Ok(ApiResponse.OK("库存调整成功"));
            }

            return BadRequest(ApiResponse.Fail("库存调整失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "库存调整失败");
            return BadRequest(ApiResponse.Fail("库存调整失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 冻结库存
    /// </summary>
    /// <param name="request">冻结请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("freeze")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> FreezeInventory([FromBody] FreezeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            request.OperatorId = User.GetUserIdAsLong() ?? 0;
            request.OperatorName = User.GetUserName();

            var success = await _inventoryService.FreezeInventoryAsync(request);
            if (success)
            {
                _logger.LogInformation("用户 {UserId} 冻结库存: {InventoryRecordId}, 数量: {Quantity}", 
                    User.GetUserIdAsLong(), request.InventoryRecordId, request.Quantity);
                return Ok(ApiResponse.OK("库存冻结成功"));
            }

            return BadRequest(ApiResponse.Fail("库存冻结失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "库存冻结失败");
            return BadRequest(ApiResponse.Fail("库存冻结失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 解冻库存
    /// </summary>
    /// <param name="request">解冻请求</param>
    /// <returns>操作结果</returns>
    [HttpPost("unfreeze")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> UnfreezeInventory([FromBody] UnfreezeRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            request.OperatorId = User.GetUserIdAsLong() ?? 0;
            request.OperatorName = User.GetUserName();

            var success = await _inventoryService.UnfreezeInventoryAsync(request);
            if (success)
            {
                _logger.LogInformation("用户 {UserId} 解冻库存: {InventoryRecordId}, 数量: {Quantity}", 
                    User.GetUserIdAsLong(), request.InventoryRecordId, request.Quantity);
                return Ok(ApiResponse.OK("库存解冻成功"));
            }

            return BadRequest(ApiResponse.Fail("库存解冻失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "库存解冻失败");
            return BadRequest(ApiResponse.Fail("库存解冻失败: " + ex.Message));
        }
    }

    #endregion

    #region 库存查询

    /// <summary>
    /// 根据批次号查询库存
    /// </summary>
    /// <param name="batchNumber">批次号</param>
    /// <returns>库存记录列表</returns>
    [HttpGet("by-batch/{batchNumber}")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryRecord>>), 200)]
    public async Task<IActionResult> GetInventoryByBatch(string batchNumber)
    {
        try
        {
            var inventory = await _inventoryService.GetInventoryByBatchAsync(batchNumber);
            return Ok(ApiResponse.OK(inventory));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询批次库存失败: {BatchNumber}", batchNumber);
            return BadRequest(ApiResponse.Fail("查询批次库存失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 根据序列号查询库存
    /// </summary>
    /// <param name="serialNumber">序列号</param>
    /// <returns>库存记录</returns>
    [HttpGet("by-serial/{serialNumber}")]
    [ProducesResponseType(typeof(ApiResponse<InventoryRecord>), 200)]
    [ProducesResponseType(typeof(ApiResponse), 404)]
    public async Task<IActionResult> GetInventoryBySerialNumber(string serialNumber)
    {
        try
        {
            var inventory = await _inventoryService.GetInventoryBySerialNumberAsync(serialNumber);
            if (inventory == null)
            {
                return NotFound(ApiResponse.Fail("未找到对应的库存记录"));
            }

            return Ok(ApiResponse.OK(inventory));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "查询序列号库存失败: {SerialNumber}", serialNumber);
            return BadRequest(ApiResponse.Fail("查询序列号库存失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 分页查询库存记录
    /// </summary>
    /// <param name="request">查询条件</param>
    /// <returns>分页的库存记录</returns>
    [HttpPost("query")]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<InventoryRecord>>), 200)]
    public async Task<IActionResult> QueryInventory([FromBody] InventoryQueryRequest request)
    {
        try
        {
            var (items, total) = await _inventoryService.GetInventoryPagedAsync(request);
            var result = new PagedResult<InventoryRecord>
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
            _logger.LogError(ex, "查询库存记录失败");
            return BadRequest(ApiResponse.Fail("查询库存记录失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取仓库库存汇总
    /// </summary>
    /// <param name="warehouseId">仓库ID</param>
    /// <returns>库存汇总信息</returns>
    [HttpGet("summary/{warehouseId}")]
    [ProducesResponseType(typeof(ApiResponse<WarehouseInventorySummary>), 200)]
    public async Task<IActionResult> GetWarehouseInventorySummary(long warehouseId)
    {
        try
        {
            var summary = await _inventoryService.GetWarehouseInventorySummaryAsync(warehouseId);
            return Ok(ApiResponse.OK(summary));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取仓库库存汇总失败: {WarehouseId}", warehouseId);
            return BadRequest(ApiResponse.Fail("获取仓库库存汇总失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取库存明细
    /// </summary>
    /// <param name="request">查询条件</param>
    /// <returns>库存明细列表</returns>
    [HttpPost("details")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryDetail>>), 200)]
    public async Task<IActionResult> GetInventoryDetails([FromBody] InventoryDetailRequest request)
    {
        try
        {
            var details = await _inventoryService.GetInventoryDetailsAsync(request);
            return Ok(ApiResponse.OK(details));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取库存明细失败");
            return BadRequest(ApiResponse.Fail("获取库存明细失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取库存事务历史
    /// </summary>
    /// <param name="materialCode">物料编码</param>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>事务历史列表</returns>
    [HttpGet("transactions/{materialCode}")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryTransaction>>), 200)]
    public async Task<IActionResult> GetInventoryTransactions(string materialCode, 
        [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var transactions = await _inventoryService.GetInventoryTransactionsAsync(materialCode, startDate, endDate);
            return Ok(ApiResponse.OK(transactions));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取库存事务历史失败: {MaterialCode}", materialCode);
            return BadRequest(ApiResponse.Fail("获取库存事务历史失败: " + ex.Message));
        }
    }

    #endregion

    #region 库存预警

    /// <summary>
    /// 配置库存预警
    /// </summary>
    /// <param name="alert">预警配置</param>
    /// <returns>配置结果</returns>
    [HttpPost("alerts")]
    [ProducesResponseType(typeof(ApiResponse), 200)]
    [ProducesResponseType(typeof(ApiResponse), 400)]
    public async Task<IActionResult> ConfigureInventoryAlert([FromBody] InventoryAlert alert)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse.Fail("请求参数无效", ModelState.GetErrors()));
            }

            alert.CreatedBy = User.GetUserIdAsLong();
            var success = await _inventoryService.ConfigureInventoryAlertAsync(alert);

            if (success)
            {
                _logger.LogInformation("用户 {UserId} 配置库存预警: {MaterialCode}", User.GetUserIdAsLong(), alert.MaterialCode);
                return Ok(ApiResponse.OK("预警配置成功"));
            }

            return BadRequest(ApiResponse.Fail("预警配置失败"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置库存预警失败");
            return BadRequest(ApiResponse.Fail("配置库存预警失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取预警配置列表
    /// </summary>
    /// <param name="warehouseId">仓库ID（可选）</param>
    /// <returns>预警配置列表</returns>
    [HttpGet("alerts")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryAlert>>), 200)]
    public async Task<IActionResult> GetInventoryAlerts([FromQuery] long? warehouseId = null)
    {
        try
        {
            var alerts = await _inventoryService.GetInventoryAlertsAsync(warehouseId);
            return Ok(ApiResponse.OK(alerts));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取预警配置失败");
            return BadRequest(ApiResponse.Fail("获取预警配置失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 检查库存预警
    /// </summary>
    /// <returns>预警结果列表</returns>
    [HttpGet("alerts/check")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryAlertResult>>), 200)]
    public async Task<IActionResult> CheckInventoryAlerts()
    {
        try
        {
            var alertResults = await _inventoryService.CheckInventoryAlertsAsync();
            return Ok(ApiResponse.OK(alertResults));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查库存预警失败");
            return BadRequest(ApiResponse.Fail("检查库存预警失败: " + ex.Message));
        }
    }

    #endregion

    #region 库存分析

    /// <summary>
    /// 获取库存周转率分析
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>周转率分析结果</returns>
    [HttpGet("analysis/turnover")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryTurnoverAnalysis>>), 200)]
    public async Task<IActionResult> GetInventoryTurnoverAnalysis(
        [FromQuery, Required] DateTime startDate, 
        [FromQuery, Required] DateTime endDate)
    {
        try
        {
            var analysis = await _inventoryService.GetInventoryTurnoverAnalysisAsync(startDate, endDate);
            return Ok(ApiResponse.OK(analysis));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取库存周转率分析失败");
            return BadRequest(ApiResponse.Fail("获取库存周转率分析失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取库存价值分析
    /// </summary>
    /// <param name="warehouseId">仓库ID（可选）</param>
    /// <returns>价值分析结果</returns>
    [HttpGet("analysis/value")]
    [ProducesResponseType(typeof(ApiResponse<InventoryValueAnalysis>), 200)]
    public async Task<IActionResult> GetInventoryValueAnalysis([FromQuery] long? warehouseId = null)
    {
        try
        {
            var analysis = await _inventoryService.GetInventoryValueAnalysisAsync(warehouseId);
            return Ok(ApiResponse.OK(analysis));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取库存价值分析失败");
            return BadRequest(ApiResponse.Fail("获取库存价值分析失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取ABC分析
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>ABC分析结果</returns>
    [HttpGet("analysis/abc")]
    [ProducesResponseType(typeof(ApiResponse<List<AbcAnalysisResult>>), 200)]
    public async Task<IActionResult> GetAbcAnalysis(
        [FromQuery, Required] DateTime startDate, 
        [FromQuery, Required] DateTime endDate)
    {
        try
        {
            var analysis = await _inventoryService.GetAbcAnalysisAsync(startDate, endDate);
            return Ok(ApiResponse.OK(analysis));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取ABC分析失败");
            return BadRequest(ApiResponse.Fail("获取ABC分析失败: " + ex.Message));
        }
    }

    /// <summary>
    /// 获取库存老化分析
    /// </summary>
    /// <param name="warehouseId">仓库ID（可选）</param>
    /// <returns>老化分析结果</returns>
    [HttpGet("analysis/aging")]
    [ProducesResponseType(typeof(ApiResponse<List<InventoryAgingAnalysis>>), 200)]
    public async Task<IActionResult> GetInventoryAgingAnalysis([FromQuery] long? warehouseId = null)
    {
        try
        {
            var analysis = await _inventoryService.GetInventoryAgingAnalysisAsync(warehouseId);
            return Ok(ApiResponse.OK(analysis));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取库存老化分析失败");
            return BadRequest(ApiResponse.Fail("获取库存老化分析失败: " + ex.Message));
        }
    }

    #endregion
} 
