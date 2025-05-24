using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Enums;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 设备管理服务接口
/// </summary>
public interface IEquipmentService
{
    #region 设备管理
    /// <summary>
    /// 创建设备
    /// </summary>
    Task<long> CreateEquipmentAsync(Equipment equipment);

    /// <summary>
    /// 获取设备列表
    /// </summary>
    Task<List<Equipment>> GetEquipmentsAsync();

    /// <summary>
    /// 根据ID获取设备
    /// </summary>
    Task<Equipment?> GetEquipmentByIdAsync(long id);

    /// <summary>
    /// 根据编码获取设备
    /// </summary>
    Task<Equipment?> GetEquipmentByCodeAsync(string equipmentCode);

    /// <summary>
    /// 更新设备信息
    /// </summary>
    Task<bool> UpdateEquipmentAsync(Equipment equipment);

    /// <summary>
    /// 删除设备
    /// </summary>
    Task<bool> DeleteEquipmentAsync(long id);

    /// <summary>
    /// 分页查询设备
    /// </summary>
    Task<(List<Equipment> Items, int Total)> GetEquipmentsPagedAsync(EquipmentQueryRequest request);

    /// <summary>
    /// 根据类型获取设备列表
    /// </summary>
    Task<List<Equipment>> GetEquipmentsByTypeAsync(EquipmentType equipmentType);

    /// <summary>
    /// 根据状态获取设备列表
    /// </summary>
    Task<List<Equipment>> GetEquipmentsByStatusAsync(EquipmentStatus status);

    /// <summary>
    /// 获取工作站设备列表
    /// </summary>
    Task<List<Equipment>> GetEquipmentsByWorkstationAsync(long workstationId);
    #endregion

    #region 设备状态管理
    /// <summary>
    /// 更新设备状态
    /// </summary>
    Task<bool> UpdateEquipmentStatusAsync(EquipmentStatusUpdateRequest request);

    /// <summary>
    /// 获取设备当前状态
    /// </summary>
    Task<EquipmentStatus> GetEquipmentCurrentStatusAsync(long equipmentId);

    /// <summary>
    /// 获取设备状态历史
    /// </summary>
    Task<List<EquipmentStatusRecord>> GetEquipmentStatusHistoryAsync(long equipmentId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取设备状态统计
    /// </summary>
    Task<EquipmentStatusStatistics> GetEquipmentStatusStatisticsAsync(long? workstationId = null, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取设备运行时间统计
    /// </summary>
    Task<EquipmentRuntimeStatistics> GetEquipmentRuntimeStatisticsAsync(long equipmentId, DateTime startDate, DateTime endDate);
    #endregion

    #region 设备维护管理
    /// <summary>
    /// 创建维护计划
    /// </summary>
    Task<long> CreateMaintenancePlanAsync(EquipmentMaintenanceRecord maintenanceRecord);

    /// <summary>
    /// 开始维护
    /// </summary>
    Task<bool> StartMaintenanceAsync(long maintenanceId, MaintenanceStartRequest request);

    /// <summary>
    /// 完成维护
    /// </summary>
    Task<bool> CompleteMaintenanceAsync(long maintenanceId, MaintenanceCompleteRequest request);

    /// <summary>
    /// 获取维护记录列表
    /// </summary>
    Task<List<EquipmentMaintenanceRecord>> GetMaintenanceRecordsAsync(long? equipmentId = null, MaintenanceStatus? status = null);

    /// <summary>
    /// 分页查询维护记录
    /// </summary>
    Task<(List<EquipmentMaintenanceRecord> Items, int Total)> GetMaintenanceRecordsPagedAsync(MaintenanceQueryRequest request);

    /// <summary>
    /// 获取维护统计
    /// </summary>
    Task<MaintenanceStatistics> GetMaintenanceStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取设备维护提醒
    /// </summary>
    Task<List<MaintenanceReminder>> GetMaintenanceRemindersAsync();
    #endregion

    #region 设备报警管理
    /// <summary>
    /// 创建设备报警
    /// </summary>
    Task<long> CreateAlarmAsync(EquipmentAlarm alarm);

    /// <summary>
    /// 确认报警
    /// </summary>
    Task<bool> AcknowledgeAlarmAsync(long alarmId, AlarmAcknowledgeRequest request);

    /// <summary>
    /// 解决报警
    /// </summary>
    Task<bool> ResolveAlarmAsync(long alarmId, AlarmResolveRequest request);

    /// <summary>
    /// 关闭报警
    /// </summary>
    Task<bool> CloseAlarmAsync(long alarmId, string closedByName);

    /// <summary>
    /// 获取活动报警列表
    /// </summary>
    Task<List<EquipmentAlarm>> GetActiveAlarmsAsync();

    /// <summary>
    /// 分页查询报警记录
    /// </summary>
    Task<(List<EquipmentAlarm> Items, int Total)> GetAlarmsPagedAsync(AlarmQueryRequest request);

    /// <summary>
    /// 获取报警统计
    /// </summary>
    Task<AlarmStatistics> GetAlarmStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 获取设备报警历史
    /// </summary>
    Task<List<EquipmentAlarm>> GetEquipmentAlarmHistoryAsync(long equipmentId, DateTime? startDate = null, DateTime? endDate = null);
    #endregion

    #region 设备操作日志
    /// <summary>
    /// 记录设备操作
    /// </summary>
    Task<bool> LogEquipmentOperationAsync(EquipmentOperationLog operationLog);

    /// <summary>
    /// 获取设备操作日志
    /// </summary>
    Task<List<EquipmentOperationLog>> GetEquipmentOperationLogsAsync(long equipmentId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// 分页查询操作日志
    /// </summary>
    Task<(List<EquipmentOperationLog> Items, int Total)> GetOperationLogsPagedAsync(OperationLogQueryRequest request);
    #endregion

    #region 设备监控面板
    /// <summary>
    /// 获取设备监控面板数据
    /// </summary>
    Task<EquipmentMonitoringDashboard> GetMonitoringDashboardAsync();

    /// <summary>
    /// 获取设备OEE统计
    /// </summary>
    Task<EquipmentOeeStatistics> GetEquipmentOeeStatisticsAsync(long equipmentId, DateTime startDate, DateTime endDate);

    /// <summary>
    /// 获取设备利用率分析
    /// </summary>
    Task<List<EquipmentUtilizationAnalysis>> GetEquipmentUtilizationAnalysisAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 获取设备故障分析
    /// </summary>
    Task<List<EquipmentFaultAnalysis>> GetEquipmentFaultAnalysisAsync(DateTime startDate, DateTime endDate);
    #endregion
}

/// <summary>
/// 设备查询请求
/// </summary>
public class EquipmentQueryRequest
{
    public string? EquipmentCode { get; set; }
    public string? EquipmentName { get; set; }
    public EquipmentType? EquipmentType { get; set; }
    public EquipmentStatus? CurrentStatus { get; set; }
    public long? WorkstationId { get; set; }
    public long? ProductionLineId { get; set; }
    public bool? IsEnabled { get; set; }
    public bool? IsCritical { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 设备状态更新请求
/// </summary>
public class EquipmentStatusUpdateRequest
{
    public long EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public EquipmentStatus NewStatus { get; set; }
    public string? Reason { get; set; }
    public long? OperatorId { get; set; }
    public string? OperatorName { get; set; }
    public string? WorkOrderNumber { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// 维护开始请求
/// </summary>
public class MaintenanceStartRequest
{
    public long? MaintenancePersonId { get; set; }
    public string? MaintenancePersonName { get; set; }
    public string? ExternalCompany { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// 维护完成请求
/// </summary>
public class MaintenanceCompleteRequest
{
    public string MaintenanceResult { get; set; } = string.Empty;
    public string? SpareParts { get; set; }
    public decimal? MaintenanceCost { get; set; }
    public DateTime? NextMaintenanceTime { get; set; }
    public string? Remarks { get; set; }
}

/// <summary>
/// 维护查询请求
/// </summary>
public class MaintenanceQueryRequest
{
    public long? EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public MaintenanceType? MaintenanceType { get; set; }
    public MaintenanceLevel? MaintenanceLevel { get; set; }
    public MaintenanceStatus? MaintenanceStatus { get; set; }
    public DateTime? PlannedStartTimeFrom { get; set; }
    public DateTime? PlannedStartTimeTo { get; set; }
    public long? MaintenancePersonId { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 报警确认请求
/// </summary>
public class AlarmAcknowledgeRequest
{
    public long AcknowledgedById { get; set; }
    public string AcknowledgedByName { get; set; } = string.Empty;
    public string? Remarks { get; set; }
}

/// <summary>
/// 报警解决请求
/// </summary>
public class AlarmResolveRequest
{
    public string HandlingActions { get; set; } = string.Empty;
    public string? RootCause { get; set; }
    public string? PreventiveMeasures { get; set; }
    public long ResolvedById { get; set; }
    public string ResolvedByName { get; set; } = string.Empty;
}

/// <summary>
/// 报警查询请求
/// </summary>
public class AlarmQueryRequest
{
    public long? EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public AlarmType? AlarmType { get; set; }
    public AlarmLevel? AlarmLevel { get; set; }
    public AlarmStatus? AlarmStatus { get; set; }
    public DateTime? AlarmStartTimeFrom { get; set; }
    public DateTime? AlarmStartTimeTo { get; set; }
    public long? AcknowledgedById { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 操作日志查询请求
/// </summary>
public class OperationLogQueryRequest
{
    public long? EquipmentId { get; set; }
    public string? EquipmentCode { get; set; }
    public EquipmentOperationType? OperationType { get; set; }
    public DateTime? OperationTimeFrom { get; set; }
    public DateTime? OperationTimeTo { get; set; }
    public long? OperatorId { get; set; }
    public string? RelatedBatchNumber { get; set; }
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// 设备状态统计
/// </summary>
public class EquipmentStatusStatistics
{
    public int TotalEquipments { get; set; }
    public int RunningEquipments { get; set; }
    public int IdleEquipments { get; set; }
    public int FaultEquipments { get; set; }
    public int MaintenanceEquipments { get; set; }
    public int OfflineEquipments { get; set; }
    public double OverallUtilizationRate { get; set; }
    public List<StatusDistribution> StatusDistributions { get; set; } = new();
}

/// <summary>
/// 状态分布
/// </summary>
public class StatusDistribution
{
    public EquipmentStatus Status { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// 设备运行时间统计
/// </summary>
public class EquipmentRuntimeStatistics
{
    public long EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public TimeSpan TotalRuntime { get; set; }
    public TimeSpan IdleTime { get; set; }
    public TimeSpan FaultTime { get; set; }
    public TimeSpan MaintenanceTime { get; set; }
    public double UtilizationRate { get; set; }
    public double AvailabilityRate { get; set; }
    public List<RuntimeSegment> RuntimeSegments { get; set; } = new();
}

/// <summary>
/// 运行时间段
/// </summary>
public class RuntimeSegment
{
    public EquipmentStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// 维护统计
/// </summary>
public class MaintenanceStatistics
{
    public int TotalMaintenanceRecords { get; set; }
    public int PlannedMaintenances { get; set; }
    public int CompletedMaintenances { get; set; }
    public int OverdueMaintenances { get; set; }
    public int EmergencyMaintenances { get; set; }
    public decimal TotalMaintenanceCost { get; set; }
    public double AverageMaintenanceTime { get; set; }
    public List<MaintenanceTypeStatistics> TypeStatistics { get; set; } = new();
}

/// <summary>
/// 维护类型统计
/// </summary>
public class MaintenanceTypeStatistics
{
    public MaintenanceType MaintenanceType { get; set; }
    public int Count { get; set; }
    public decimal TotalCost { get; set; }
    public double AverageTime { get; set; }
}

/// <summary>
/// 维护提醒
/// </summary>
public class MaintenanceReminder
{
    public long EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public MaintenanceType MaintenanceType { get; set; }
    public DateTime PlannedMaintenanceTime { get; set; }
    public int DaysOverdue { get; set; }
    public bool IsOverdue { get; set; }
    public bool IsCritical { get; set; }
}

/// <summary>
/// 报警统计
/// </summary>
public class AlarmStatistics
{
    public int TotalAlarms { get; set; }
    public int ActiveAlarms { get; set; }
    public int AcknowledgedAlarms { get; set; }
    public int ResolvedAlarms { get; set; }
    public int CriticalAlarms { get; set; }
    public double AverageResponseTime { get; set; }
    public double AverageResolutionTime { get; set; }
    public List<AlarmTypeStatistics> TypeStatistics { get; set; } = new();
    public List<AlarmLevelStatistics> LevelStatistics { get; set; } = new();
}

/// <summary>
/// 报警类型统计
/// </summary>
public class AlarmTypeStatistics
{
    public AlarmType AlarmType { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// 报警级别统计
/// </summary>
public class AlarmLevelStatistics
{
    public AlarmLevel AlarmLevel { get; set; }
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// 设备监控面板
/// </summary>
public class EquipmentMonitoringDashboard
{
    public EquipmentStatusStatistics StatusStatistics { get; set; } = new();
    public AlarmStatistics AlarmStatistics { get; set; } = new();
    public MaintenanceStatistics MaintenanceStatistics { get; set; } = new();
    public List<Equipment> CriticalEquipments { get; set; } = new();
    public List<EquipmentAlarm> RecentAlarms { get; set; } = new();
    public List<MaintenanceReminder> MaintenanceReminders { get; set; } = new();
    public double OverallOee { get; set; }
    public DateTime LastUpdateTime { get; set; } = DateTime.Now;
}

/// <summary>
/// 设备OEE统计
/// </summary>
public class EquipmentOeeStatistics
{
    public long EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public double Availability { get; set; }
    public double Performance { get; set; }
    public double Quality { get; set; }
    public double Oee { get; set; }
    public DateTime CalculationPeriodStart { get; set; }
    public DateTime CalculationPeriodEnd { get; set; }
    public TimeSpan PlannedProductionTime { get; set; }
    public TimeSpan ActualProductionTime { get; set; }
    public TimeSpan DownTime { get; set; }
    public int TotalProduction { get; set; }
    public int QualifiedProduction { get; set; }
    public int DefectiveProduction { get; set; }
}

/// <summary>
/// 设备利用率分析
/// </summary>
public class EquipmentUtilizationAnalysis
{
    public long EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public EquipmentType EquipmentType { get; set; }
    public double UtilizationRate { get; set; }
    public TimeSpan TotalRuntime { get; set; }
    public TimeSpan IdleTime { get; set; }
    public TimeSpan FaultTime { get; set; }
    public int FaultCount { get; set; }
    public double MeanTimeBetweenFailures { get; set; }
    public double MeanTimeToRepair { get; set; }
}

/// <summary>
/// 设备故障分析
/// </summary>
public class EquipmentFaultAnalysis
{
    public long EquipmentId { get; set; }
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public int TotalFaults { get; set; }
    public TimeSpan TotalFaultTime { get; set; }
    public double AverageFaultDuration { get; set; }
    public AlarmType MostFrequentFaultType { get; set; }
    public List<FaultFrequencyStatistics> FaultFrequencies { get; set; } = new();
}

/// <summary>
/// 故障频率统计
/// </summary>
public class FaultFrequencyStatistics
{
    public AlarmType AlarmType { get; set; }
    public int Count { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public double AverageDuration { get; set; }
} 