using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Enums;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 库存管理服务接口
/// </summary>
public interface IInventoryService
{
    #region 仓库管理
    /// <summary>
    /// 创建仓库
    /// </summary>
    Task<long> CreateWarehouseAsync(Warehouse warehouse);

    /// <summary>
    /// 获取仓库列表
    /// </summary>
    Task<List<Warehouse>> GetWarehousesAsync();

    /// <summary>
    /// 根据ID获取仓库
    /// </summary>
    Task<Warehouse?> GetWarehouseByIdAsync(long id);

    /// <summary>
    /// 根据编码获取仓库
    /// </summary>
    Task<Warehouse?> GetWarehouseByCodeAsync(string warehouseCode);

    /// <summary>
    /// 更新仓库信息
    /// </summary>
    Task<bool> UpdateWarehouseAsync(Warehouse warehouse);

    /// <summary>
    /// 删除仓库
    /// </summary>
    Task<bool> DeleteWarehouseAsync(long id);
    #endregion

    #region 库位管理
    /// <summary>
    /// 创建库位
    /// </summary>
    Task<long> CreateLocationAsync(WarehouseLocation location);

    /// <summary>
    /// 获取指定仓库的库位列表
    /// </summary>
    Task<List<WarehouseLocation>> GetLocationsByWarehouseAsync(long warehouseId);

    /// <summary>
    /// 根据ID获取库位
    /// </summary>
    Task<WarehouseLocation?> GetLocationByIdAsync(long id);

    /// <summary>
    /// 根据编码获取库位
    /// </summary>
    Task<WarehouseLocation?> GetLocationByCodeAsync(string locationCode);

    /// <summary>
    /// 更新库位信息
    /// </summary>
    Task<bool> UpdateLocationAsync(WarehouseLocation location);

    /// <summary>
    /// 删除库位
    /// </summary>
    Task<bool> DeleteLocationAsync(long id);

    /// <summary>
    /// 获取可用库位
    /// </summary>
    Task<List<WarehouseLocation>> GetAvailableLocationsAsync(long warehouseId);

    /// <summary>
    /// 检查库位容量
    /// </summary>
    Task<bool> CheckLocationCapacityAsync(long locationId, decimal requiredCapacity);
    #endregion

    #region 库存操作
    /// <summary>
    /// 入库操作
    /// </summary>
    Task<bool> InboundAsync(InboundRequest request);

    /// <summary>
    /// 出库操作
    /// </summary>
    Task<bool> OutboundAsync(OutboundRequest request);

    /// <summary>
    /// 移库操作
    /// </summary>
    Task<bool> TransferAsync(TransferRequest request);

    /// <summary>
    /// 库存调整
    /// </summary>
    Task<bool> AdjustInventoryAsync(AdjustmentRequest request);

    /// <summary>
    /// 冻结库存
    /// </summary>
    Task<bool> FreezeInventoryAsync(FreezeRequest request);

    /// <summary>
    /// 解冻库存
    /// </summary>
    Task<bool> UnfreezeInventoryAsync(UnfreezeRequest request);

    /// <summary>
    /// 预留库存
    /// </summary>
    Task<bool> ReserveInventoryAsync(ReserveRequest request);

    /// <summary>
    /// 取消预留
    /// </summary>
    Task<bool> CancelReservationAsync(CancelReservationRequest request);
    #endregion

    #region 库存查询
    /// <summary>
    /// 获取物料库存
    /// </summary>
    Task<List<InventoryRecord>> GetInventoryByMaterialAsync(string materialCode, long? warehouseId = null);

    /// <summary>
    /// 获取批次库存
    /// </summary>
    Task<List<InventoryRecord>> GetInventoryByBatchAsync(string batchNumber);

    /// <summary>
    /// 获取序列号库存
    /// </summary>
    Task<InventoryRecord?> GetInventoryBySerialNumberAsync(string serialNumber);

    /// <summary>
    /// 获取仓库库存统计
    /// </summary>
    Task<WarehouseInventorySummary> GetWarehouseInventorySummaryAsync(long warehouseId);

    /// <summary>
    /// 分页查询库存记录
    /// </summary>
    Task<(List<InventoryRecord> Items, int Total)> GetInventoryPagedAsync(InventoryQueryRequest request);

    /// <summary>
    /// 获取库存明细
    /// </summary>
    Task<List<InventoryDetail>> GetInventoryDetailsAsync(InventoryDetailRequest request);

    /// <summary>
    /// 获取库存事务历史
    /// </summary>
    Task<List<InventoryTransaction>> GetInventoryTransactionsAsync(string materialCode, DateTime? startDate = null, DateTime? endDate = null);
    #endregion

    #region 库存盘点
    /// <summary>
    /// 创建盘点计划
    /// </summary>
    Task<long> CreateInventoryCountPlanAsync(InventoryCountPlan plan);

    /// <summary>
    /// 执行盘点
    /// </summary>
    Task<bool> ExecuteInventoryCountAsync(long planId, List<InventoryCountDetail> countDetails);

    /// <summary>
    /// 确认盘点差异
    /// </summary>
    Task<bool> ConfirmInventoryDifferencesAsync(long planId, List<InventoryDifferenceConfirmation> confirmations);

    /// <summary>
    /// 获取盘点计划列表
    /// </summary>
    Task<List<InventoryCountPlan>> GetInventoryCountPlansAsync(DateTime? startDate = null, DateTime? endDate = null);
    #endregion

    #region 库存预警
    /// <summary>
    /// 配置库存预警
    /// </summary>
    Task<bool> ConfigureInventoryAlertAsync(InventoryAlert alert);

    /// <summary>
    /// 获取预警配置
    /// </summary>
    Task<List<InventoryAlert>> GetInventoryAlertsAsync(long? warehouseId = null);

    /// <summary>
    /// 检查库存预警
    /// </summary>
    Task<List<InventoryAlertResult>> CheckInventoryAlertsAsync();

    /// <summary>
    /// 获取预警历史
    /// </summary>
    Task<List<InventoryAlertHistory>> GetAlertHistoryAsync(DateTime? startDate = null, DateTime? endDate = null);
    #endregion

    #region 库存分析
    /// <summary>
    /// 获取库存周转率
    /// </summary>
    Task<List<InventoryTurnoverAnalysis>> GetInventoryTurnoverAnalysisAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 获取库存价值分析
    /// </summary>
    Task<InventoryValueAnalysis> GetInventoryValueAnalysisAsync(long? warehouseId = null);

    /// <summary>
    /// 获取ABC分析
    /// </summary>
    Task<List<AbcAnalysisResult>> GetAbcAnalysisAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 获取库存老化分析
    /// </summary>
    Task<List<InventoryAgingAnalysis>> GetInventoryAgingAnalysisAsync(long? warehouseId = null);
    #endregion
}

/// <summary>
/// 入库请求
/// </summary>
public class InboundRequest
{
    public long WarehouseId { get; set; }
    public long LocationId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public MaterialType MaterialType { get; set; }
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public string? SupplierCode { get; set; }
    public DateTime? ProductionDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? UnitPrice { get; set; }
    public QualityStatus QualityStatus { get; set; } = QualityStatus.NotInspected;
    public string? RelatedDocumentNumber { get; set; }
    public string? Remarks { get; set; }
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
}

/// <summary>
/// 出库请求
/// </summary>
public class OutboundRequest
{
    public long WarehouseId { get; set; }
    public long LocationId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; }
    public string? RelatedDocumentNumber { get; set; }
    public string? RelatedDocumentType { get; set; }
    public string? Reason { get; set; }
    public string? Remarks { get; set; }
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
}

/// <summary>
/// 移库请求
/// </summary>
public class TransferRequest
{
    public long FromWarehouseId { get; set; }
    public long FromLocationId { get; set; }
    public long ToWarehouseId { get; set; }
    public long ToLocationId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public decimal Quantity { get; set; }
    public string? Reason { get; set; }
    public string? Remarks { get; set; }
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
}

/// <summary>
/// 库存调整请求
/// </summary>
public class AdjustmentRequest
{
    public long InventoryRecordId { get; set; }
    public decimal NewQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
}

/// <summary>
/// 冻结请求
/// </summary>
public class FreezeRequest
{
    public long InventoryRecordId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
}

/// <summary>
/// 解冻请求
/// </summary>
public class UnfreezeRequest
{
    public long InventoryRecordId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
}

/// <summary>
/// 预留请求
/// </summary>
public class ReserveRequest
{
    public string MaterialCode { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public decimal Quantity { get; set; }
    public string? BatchNumber { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public DateTime ReserveUntil { get; set; }
    public string? RelatedDocumentNumber { get; set; }
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
}

/// <summary>
/// 取消预留请求
/// </summary>
public class CancelReservationRequest
{
    public long InventoryRecordId { get; set; }
    public decimal Quantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public long OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
}

/// <summary>
/// 库存查询请求
/// </summary>
public class InventoryQueryRequest
{
    public long? WarehouseId { get; set; }
    public long? LocationId { get; set; }
    public string? MaterialCode { get; set; }
    public MaterialType? MaterialType { get; set; }
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public QualityStatus? QualityStatus { get; set; }
    public InventoryStatus? InventoryStatus { get; set; }
    public DateTime? InboundDateFrom { get; set; }
    public DateTime? InboundDateTo { get; set; }
    public bool? IsExpired { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 库存明细请求
/// </summary>
public class InventoryDetailRequest
{
    public long? WarehouseId { get; set; }
    public string? MaterialCode { get; set; }
    public string? BatchNumber { get; set; }
    public bool IncludeTransactionHistory { get; set; } = false;
}

/// <summary>
/// 仓库库存汇总
/// </summary>
public class WarehouseInventorySummary
{
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public int TotalMaterials { get; set; }
    public int TotalBatches { get; set; }
    public decimal TotalValue { get; set; }
    public int AvailableLocations { get; set; }
    public int OccupiedLocations { get; set; }
    public decimal UtilizationRate { get; set; }
    public List<MaterialInventorySummary> MaterialSummaries { get; set; } = new();
}

/// <summary>
/// 物料库存汇总
/// </summary>
public class MaterialInventorySummary
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public MaterialType MaterialType { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal FrozenQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal? TotalValue { get; set; }
    public int BatchCount { get; set; }
    public DateTime? OldestInboundDate { get; set; }
    public DateTime? LatestInboundDate { get; set; }
}

/// <summary>
/// 库存明细
/// </summary>
public class InventoryDetail
{
    public InventoryRecord InventoryRecord { get; set; } = new();
    public List<InventoryTransaction>? TransactionHistory { get; set; }
    public WarehouseLocation Location { get; set; } = new();
    public Warehouse Warehouse { get; set; } = new();
}

/// <summary>
/// 盘点计划
/// </summary>
public class InventoryCountPlan
{
    public long Id { get; set; }
    public string PlanNumber { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string? LocationCodes { get; set; }
    public string? MaterialCodes { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string Status { get; set; } = "Planned"; // Planned, InProgress, Completed, Cancelled
    public long PlannerUserId { get; set; }
    public string PlannerUserName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
    public DateTime CreatedTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 盘点明细
/// </summary>
public class InventoryCountDetail
{
    public long PlanId { get; set; }
    public long InventoryRecordId { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string? BatchNumber { get; set; }
    public string? SerialNumber { get; set; }
    public decimal BookQuantity { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal DifferenceQuantity { get; set; }
    public string? Remarks { get; set; }
    public long CounterUserId { get; set; }
    public string CounterUserName { get; set; } = string.Empty;
    public DateTime CountTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 盘点差异确认
/// </summary>
public class InventoryDifferenceConfirmation
{
    public long CountDetailId { get; set; }
    public bool IsConfirmed { get; set; }
    public string? ConfirmationReason { get; set; }
    public long ConfirmerUserId { get; set; }
    public string ConfirmerUserName { get; set; } = string.Empty;
}

/// <summary>
/// 库存预警结果
/// </summary>
public class InventoryAlertResult
{
    public long AlertId { get; set; }
    public AlertType AlertType { get; set; }
    public AlertLevel AlertLevel { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public decimal? ThresholdQuantity { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
    public DateTime AlertTime { get; set; } = DateTime.Now;
    public bool IsProcessed { get; set; } = false;
}

/// <summary>
/// 库存预警历史
/// </summary>
public class InventoryAlertHistory
{
    public long Id { get; set; }
    public AlertType AlertType { get; set; }
    public AlertLevel AlertLevel { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public long WarehouseId { get; set; }
    public string AlertMessage { get; set; } = string.Empty;
    public DateTime AlertTime { get; set; }
    public DateTime? ProcessedTime { get; set; }
    public long? ProcessedBy { get; set; }
    public string? ProcessingNotes { get; set; }
}

/// <summary>
/// 库存周转率分析
/// </summary>
public class InventoryTurnoverAnalysis
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal AverageInventory { get; set; }
    public decimal TotalConsumption { get; set; }
    public decimal TurnoverRate { get; set; }
    public int TurnoverDays { get; set; }
    public decimal InventoryValue { get; set; }
    public string TurnoverGrade { get; set; } = string.Empty; // Fast, Normal, Slow
}

/// <summary>
/// 库存价值分析
/// </summary>
public class InventoryValueAnalysis
{
    public decimal TotalInventoryValue { get; set; }
    public decimal RawMaterialValue { get; set; }
    public decimal WorkInProgressValue { get; set; }
    public decimal FinishedGoodsValue { get; set; }
    public List<MaterialValueRanking> MaterialValueRankings { get; set; } = new();
    public List<WarehouseValueSummary> WarehouseValueSummaries { get; set; } = new();
}

/// <summary>
/// 物料价值排行
/// </summary>
public class MaterialValueRanking
{
    public int Ranking { get; set; }
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal InventoryValue { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// 仓库价值汇总
/// </summary>
public class WarehouseValueSummary
{
    public long WarehouseId { get; set; }
    public string WarehouseName { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// ABC分析结果
/// </summary>
public class AbcAnalysisResult
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal ConsumptionValue { get; set; }
    public decimal ConsumptionQuantity { get; set; }
    public decimal ValuePercentage { get; set; }
    public decimal CumulativePercentage { get; set; }
    public string AbcCategory { get; set; } = string.Empty; // A, B, C
}

/// <summary>
/// 库存老化分析
/// </summary>
public class InventoryAgingAnalysis
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime InboundDate { get; set; }
    public int AgingDays { get; set; }
    public decimal Quantity { get; set; }
    public decimal InventoryValue { get; set; }
    public string AgingLevel { get; set; } = string.Empty; // Fresh, Normal, Aging, Expired
} 