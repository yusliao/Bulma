using SqlSugar;
using System.ComponentModel.DataAnnotations;
using BatteryPackingMES.Core.Enums;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 设备信息
/// </summary>
[SugarTable("equipments")]
public class Equipment : BaseEntity
{
    /// <summary>
    /// 设备编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "设备编码")]
    [Required(ErrorMessage = "设备编码不能为空")]
    public string EquipmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 设备名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "设备名称")]
    [Required(ErrorMessage = "设备名称不能为空")]
    public string EquipmentName { get; set; } = string.Empty;

    /// <summary>
    /// 设备类型
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "设备类型")]
    public EquipmentType EquipmentType { get; set; }

    /// <summary>
    /// 设备分类
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "设备分类")]
    public string? Category { get; set; }

    /// <summary>
    /// 设备型号
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "设备型号")]
    public string? Model { get; set; }

    /// <summary>
    /// 制造商
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "制造商")]
    public string? Manufacturer { get; set; }

    /// <summary>
    /// 序列号
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "序列号")]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// 规格参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "规格参数")]
    public string? Specifications { get; set; }

    /// <summary>
    /// 所属工作站ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "所属工作站ID")]
    public long? WorkstationId { get; set; }

    /// <summary>
    /// 所属产线ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "所属产线ID")]
    public long? ProductionLineId { get; set; }

    /// <summary>
    /// 设备位置
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    [Display(Name = "设备位置")]
    public string? Location { get; set; }

    /// <summary>
    /// 当前状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "当前状态")]
    public EquipmentStatus CurrentStatus { get; set; } = EquipmentStatus.Idle;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 是否关键设备
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否关键设备")]
    public bool IsCritical { get; set; } = false;

    /// <summary>
    /// 安装日期
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "安装日期")]
    public DateTime? InstallationDate { get; set; }

    /// <summary>
    /// 启用日期
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "启用日期")]
    public DateTime? CommissioningDate { get; set; }

    /// <summary>
    /// 保修期结束日期
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "保修期结束日期")]
    public DateTime? WarrantyEndDate { get; set; }

    /// <summary>
    /// 责任人ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "责任人ID")]
    public long? ResponsiblePersonId { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    [Display(Name = "联系电话")]
    public string? ContactPhone { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 负责人
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(ResponsiblePersonId))]
    public User? ResponsiblePerson { get; set; }

    /// <summary>
    /// 导航属性 - 状态记录
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(EquipmentStatusRecord.EquipmentId))]
    public List<EquipmentStatusRecord>? StatusRecords { get; set; }
}

/// <summary>
/// 设备状态记录
/// </summary>
[SugarTable("equipment_status_records")]
public class EquipmentStatusRecord : BaseEntity
{
    /// <summary>
    /// 设备ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "设备ID")]
    public long EquipmentId { get; set; }

    /// <summary>
    /// 设备编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "设备编码")]
    public string EquipmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 前一状态
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "前一状态")]
    public EquipmentStatus? PreviousStatus { get; set; }

    /// <summary>
    /// 当前状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "当前状态")]
    public EquipmentStatus CurrentStatus { get; set; }

    /// <summary>
    /// 状态开始时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "状态开始时间")]
    public DateTime StartTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 状态结束时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "状态结束时间")]
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 持续时间（分钟）
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "持续时间")]
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// 状态变更原因
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    [Display(Name = "状态变更原因")]
    public string? Reason { get; set; }

    /// <summary>
    /// 操作员ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "操作员ID")]
    public long? OperatorId { get; set; }

    /// <summary>
    /// 操作员姓名
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "操作员姓名")]
    public string? OperatorName { get; set; }

    /// <summary>
    /// 相关工单号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "相关工单号")]
    public string? WorkOrderNumber { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 设备
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(EquipmentId))]
    public Equipment? Equipment { get; set; }

    /// <summary>
    /// 导航属性 - 操作员
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(OperatorId))]
    public User? Operator { get; set; }
}

/// <summary>
/// 设备维护记录
/// </summary>
[SugarTable("equipment_maintenance_records")]
public class EquipmentMaintenanceRecord : BaseEntity
{
    /// <summary>
    /// 维护单号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "维护单号")]
    [Required(ErrorMessage = "维护单号不能为空")]
    public string MaintenanceNumber { get; set; } = string.Empty;

    /// <summary>
    /// 设备ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "设备ID")]
    public long EquipmentId { get; set; }

    /// <summary>
    /// 设备编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "设备编码")]
    public string EquipmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 维护类型
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "维护类型")]
    public MaintenanceType MaintenanceType { get; set; }

    /// <summary>
    /// 维护级别
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "维护级别")]
    public MaintenanceLevel MaintenanceLevel { get; set; }

    /// <summary>
    /// 计划开始时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "计划开始时间")]
    public DateTime PlannedStartTime { get; set; }

    /// <summary>
    /// 计划结束时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "计划结束时间")]
    public DateTime PlannedEndTime { get; set; }

    /// <summary>
    /// 实际开始时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "实际开始时间")]
    public DateTime? ActualStartTime { get; set; }

    /// <summary>
    /// 实际结束时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "实际结束时间")]
    public DateTime? ActualEndTime { get; set; }

    /// <summary>
    /// 维护内容
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = false)]
    [Display(Name = "维护内容")]
    [Required(ErrorMessage = "维护内容不能为空")]
    public string MaintenanceContent { get; set; } = string.Empty;

    /// <summary>
    /// 维护结果
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    [Display(Name = "维护结果")]
    public string? MaintenanceResult { get; set; }

    /// <summary>
    /// 使用的备件（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "使用的备件")]
    public string? SpareParts { get; set; }

    /// <summary>
    /// 维护费用
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "维护费用")]
    public decimal? MaintenanceCost { get; set; }

    /// <summary>
    /// 维护状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "维护状态")]
    public MaintenanceStatus MaintenanceStatus { get; set; } = MaintenanceStatus.Planned;

    /// <summary>
    /// 维护人员ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "维护人员ID")]
    public long? MaintenancePersonId { get; set; }

    /// <summary>
    /// 维护人员姓名
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "维护人员姓名")]
    public string? MaintenancePersonName { get; set; }

    /// <summary>
    /// 外协单位
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "外协单位")]
    public string? ExternalCompany { get; set; }

    /// <summary>
    /// 下次维护时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "下次维护时间")]
    public DateTime? NextMaintenanceTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 设备
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(EquipmentId))]
    public Equipment? Equipment { get; set; }

    /// <summary>
    /// 导航属性 - 维护人员
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(MaintenancePersonId))]
    public User? MaintenancePerson { get; set; }
}

/// <summary>
/// 设备报警记录
/// </summary>
[SugarTable("equipment_alarms")]
public class EquipmentAlarm : BaseEntity
{
    /// <summary>
    /// 报警编号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "报警编号")]
    [Required(ErrorMessage = "报警编号不能为空")]
    public string AlarmNumber { get; set; } = string.Empty;

    /// <summary>
    /// 设备ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "设备ID")]
    public long EquipmentId { get; set; }

    /// <summary>
    /// 设备编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "设备编码")]
    public string EquipmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 报警类型
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "报警类型")]
    public AlarmType AlarmType { get; set; }

    /// <summary>
    /// 报警级别
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "报警级别")]
    public AlarmLevel AlarmLevel { get; set; }

    /// <summary>
    /// 报警代码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "报警代码")]
    public string? AlarmCode { get; set; }

    /// <summary>
    /// 报警描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = false)]
    [Display(Name = "报警描述")]
    [Required(ErrorMessage = "报警描述不能为空")]
    public string AlarmDescription { get; set; } = string.Empty;

    /// <summary>
    /// 报警详细信息（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "报警详细信息")]
    public string? AlarmDetails { get; set; }

    /// <summary>
    /// 报警开始时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "报警开始时间")]
    public DateTime AlarmStartTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 报警结束时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "报警结束时间")]
    public DateTime? AlarmEndTime { get; set; }

    /// <summary>
    /// 报警持续时间（分钟）
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "报警持续时间")]
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// 报警状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "报警状态")]
    public AlarmStatus AlarmStatus { get; set; } = AlarmStatus.Active;

    /// <summary>
    /// 确认人员ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "确认人员ID")]
    public long? AcknowledgedById { get; set; }

    /// <summary>
    /// 确认人员姓名
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "确认人员姓名")]
    public string? AcknowledgedByName { get; set; }

    /// <summary>
    /// 确认时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "确认时间")]
    public DateTime? AcknowledgedTime { get; set; }

    /// <summary>
    /// 处理措施
    /// </summary>
    [SugarColumn(Length = 1000, IsNullable = true)]
    [Display(Name = "处理措施")]
    public string? HandlingActions { get; set; }

    /// <summary>
    /// 根本原因
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "根本原因")]
    public string? RootCause { get; set; }

    /// <summary>
    /// 预防措施
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "预防措施")]
    public string? PreventiveMeasures { get; set; }

    /// <summary>
    /// 是否已通知
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否已通知")]
    public bool IsNotified { get; set; } = false;

    /// <summary>
    /// 通知时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "通知时间")]
    public DateTime? NotifiedTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 设备
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(EquipmentId))]
    public Equipment? Equipment { get; set; }

    /// <summary>
    /// 导航属性 - 确认人员
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(AcknowledgedById))]
    public User? AcknowledgedBy { get; set; }
}

/// <summary>
/// 设备操作日志
/// </summary>
[SugarTable("equipment_operation_logs")]
public class EquipmentOperationLog : BaseEntity
{
    /// <summary>
    /// 设备ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "设备ID")]
    public long EquipmentId { get; set; }

    /// <summary>
    /// 设备编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "设备编码")]
    public string EquipmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "操作类型")]
    public EquipmentOperationType OperationType { get; set; }

    /// <summary>
    /// 操作描述
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = false)]
    [Display(Name = "操作描述")]
    [Required(ErrorMessage = "操作描述不能为空")]
    public string OperationDescription { get; set; } = string.Empty;

    /// <summary>
    /// 操作参数（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "操作参数")]
    public string? OperationParameters { get; set; }

    /// <summary>
    /// 操作结果
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "操作结果")]
    public string? OperationResult { get; set; }

    /// <summary>
    /// 操作时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "操作时间")]
    public DateTime OperationTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 操作员ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "操作员ID")]
    public long OperatorId { get; set; }

    /// <summary>
    /// 操作员姓名
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "操作员姓名")]
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// 相关批次号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "相关批次号")]
    public string? RelatedBatchNumber { get; set; }

    /// <summary>
    /// 相关工单号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "相关工单号")]
    public string? RelatedWorkOrderNumber { get; set; }

    /// <summary>
    /// 客户端IP
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "客户端IP")]
    public string? ClientIp { get; set; }

    /// <summary>
    /// 用户代理
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "用户代理")]
    public string? UserAgent { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 设备
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(EquipmentId))]
    public Equipment? Equipment { get; set; }

    /// <summary>
    /// 导航属性 - 操作员
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(OperatorId))]
    public User? Operator { get; set; }
} 