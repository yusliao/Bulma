using SqlSugar;
using System.ComponentModel.DataAnnotations;
using BatteryPackingMES.Core.Enums;

namespace BatteryPackingMES.Core.Entities;

/// <summary>
/// 仓库信息
/// </summary>
[SugarTable("warehouses")]
public class Warehouse : BaseEntity
{
    /// <summary>
    /// 仓库编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "仓库编码")]
    [Required(ErrorMessage = "仓库编码不能为空")]
    public string WarehouseCode { get; set; } = string.Empty;

    /// <summary>
    /// 仓库名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "仓库名称")]
    [Required(ErrorMessage = "仓库名称不能为空")]
    public string WarehouseName { get; set; } = string.Empty;

    /// <summary>
    /// 仓库类型
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "仓库类型")]
    public WarehouseType WarehouseType { get; set; } = WarehouseType.General;

    /// <summary>
    /// 仓库地址
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    [Display(Name = "仓库地址")]
    public string? Address { get; set; }

    /// <summary>
    /// 仓库管理员ID
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "仓库管理员ID")]
    public long? ManagerId { get; set; }

    /// <summary>
    /// 联系电话
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    [Display(Name = "联系电话")]
    public string? Phone { get; set; }

    /// <summary>
    /// 仓库面积（平方米）
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "仓库面积")]
    public decimal? Area { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 仓库管理员
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(ManagerId))]
    public User? Manager { get; set; }

    /// <summary>
    /// 导航属性 - 库位列表
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(WarehouseLocation.WarehouseId))]
    public List<WarehouseLocation>? Locations { get; set; }
}

/// <summary>
/// 库位信息
/// </summary>
[SugarTable("warehouse_locations")]
public class WarehouseLocation : BaseEntity
{
    /// <summary>
    /// 仓库ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "仓库ID")]
    public long WarehouseId { get; set; }

    /// <summary>
    /// 库位编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "库位编码")]
    [Required(ErrorMessage = "库位编码不能为空")]
    public string LocationCode { get; set; } = string.Empty;

    /// <summary>
    /// 库位名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "库位名称")]
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// 库位类型
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "库位类型")]
    public LocationType LocationType { get; set; } = LocationType.Normal;

    /// <summary>
    /// 区域编码
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    [Display(Name = "区域编码")]
    public string? ZoneCode { get; set; }

    /// <summary>
    /// 巷道编码
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    [Display(Name = "巷道编码")]
    public string? AisleCode { get; set; }

    /// <summary>
    /// 货架编码
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    [Display(Name = "货架编码")]
    public string? RackCode { get; set; }

    /// <summary>
    /// 层数
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "层数")]
    public int? Level { get; set; }

    /// <summary>
    /// 位置
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "位置")]
    public int? Position { get; set; }

    /// <summary>
    /// 最大容量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "最大容量")]
    public decimal MaxCapacity { get; set; } = 0;

    /// <summary>
    /// 当前占用容量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "当前占用容量")]
    public decimal UsedCapacity { get; set; } = 0;

    /// <summary>
    /// 容量单位
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = true)]
    [Display(Name = "容量单位")]
    public string? CapacityUnit { get; set; }

    /// <summary>
    /// 库位状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "库位状态")]
    public LocationStatus LocationStatus { get; set; } = LocationStatus.Available;

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 环境要求（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "环境要求")]
    public string? EnvironmentRequirements { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 仓库
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// 导航属性 - 库存记录
    /// </summary>
    [Navigate(NavigateType.OneToMany, nameof(InventoryRecord.LocationId))]
    public List<InventoryRecord>? InventoryRecords { get; set; }
}

/// <summary>
/// 库存记录
/// </summary>
[SugarTable("inventory_records")]
public class InventoryRecord : BaseEntity
{
    /// <summary>
    /// 仓库ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "仓库ID")]
    public long WarehouseId { get; set; }

    /// <summary>
    /// 库位ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "库位ID")]
    public long LocationId { get; set; }

    /// <summary>
    /// 物料编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "物料编码")]
    [Required(ErrorMessage = "物料编码不能为空")]
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>
    /// 物料名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "物料名称")]
    public string MaterialName { get; set; } = string.Empty;

    /// <summary>
    /// 物料类型
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "物料类型")]
    public MaterialType MaterialType { get; set; } = MaterialType.RawMaterial;

    /// <summary>
    /// 批次号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "批次号")]
    public string? BatchNumber { get; set; }

    /// <summary>
    /// 序列号
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "序列号")]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// 供应商编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "供应商编码")]
    public string? SupplierCode { get; set; }

    /// <summary>
    /// 生产日期
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "生产日期")]
    public DateTime? ProductionDate { get; set; }

    /// <summary>
    /// 过期日期
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "过期日期")]
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// 入库日期
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "入库日期")]
    public DateTime InboundDate { get; set; } = DateTime.Now;

    /// <summary>
    /// 当前数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "当前数量")]
    public decimal CurrentQuantity { get; set; } = 0;

    /// <summary>
    /// 冻结数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "冻结数量")]
    public decimal FrozenQuantity { get; set; } = 0;

    /// <summary>
    /// 可用数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "可用数量")]
    public decimal AvailableQuantity { get; set; } = 0;

    /// <summary>
    /// 单位
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    [Display(Name = "单位")]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 单价
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "单价")]
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// 总价值
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "总价值")]
    public decimal? TotalValue { get; set; }

    /// <summary>
    /// 质量状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "质量状态")]
    public QualityStatus QualityStatus { get; set; } = QualityStatus.Qualified;

    /// <summary>
    /// 库存状态
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "库存状态")]
    public InventoryStatus InventoryStatus { get; set; } = InventoryStatus.Available;

    /// <summary>
    /// 是否启用批次管理
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用批次管理")]
    public bool IsBatchManaged { get; set; } = false;

    /// <summary>
    /// 是否启用序列号管理
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用序列号管理")]
    public bool IsSerialManaged { get; set; } = false;

    /// <summary>
    /// 最后盘点时间
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "最后盘点时间")]
    public DateTime? LastInventoryTime { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 仓库
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// 导航属性 - 库位
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(LocationId))]
    public WarehouseLocation? Location { get; set; }
}

/// <summary>
/// 库存事务记录
/// </summary>
[SugarTable("inventory_transactions")]
public class InventoryTransaction : BaseEntity
{
    /// <summary>
    /// 事务编号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "事务编号")]
    [Required(ErrorMessage = "事务编号不能为空")]
    public string TransactionNumber { get; set; } = string.Empty;

    /// <summary>
    /// 事务类型
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "事务类型")]
    public TransactionType TransactionType { get; set; }

    /// <summary>
    /// 仓库ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "仓库ID")]
    public long WarehouseId { get; set; }

    /// <summary>
    /// 库位ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "库位ID")]
    public long LocationId { get; set; }

    /// <summary>
    /// 物料编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "物料编码")]
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>
    /// 物料名称
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = false)]
    [Display(Name = "物料名称")]
    public string MaterialName { get; set; } = string.Empty;

    /// <summary>
    /// 批次号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "批次号")]
    public string? BatchNumber { get; set; }

    /// <summary>
    /// 序列号
    /// </summary>
    [SugarColumn(Length = 100, IsNullable = true)]
    [Display(Name = "序列号")]
    public string? SerialNumber { get; set; }

    /// <summary>
    /// 事务前数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "事务前数量")]
    public decimal QuantityBefore { get; set; } = 0;

    /// <summary>
    /// 变动数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "变动数量")]
    public decimal QuantityChanged { get; set; } = 0;

    /// <summary>
    /// 事务后数量
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "事务后数量")]
    public decimal QuantityAfter { get; set; } = 0;

    /// <summary>
    /// 单位
    /// </summary>
    [SugarColumn(Length = 20, IsNullable = false)]
    [Display(Name = "单位")]
    public string Unit { get; set; } = string.Empty;

    /// <summary>
    /// 单价
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "单价")]
    public decimal? UnitPrice { get; set; }

    /// <summary>
    /// 总价值变动
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "总价值变动")]
    public decimal? ValueChanged { get; set; }

    /// <summary>
    /// 关联单据号
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "关联单据号")]
    public string? RelatedDocumentNumber { get; set; }

    /// <summary>
    /// 关联单据类型
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = true)]
    [Display(Name = "关联单据类型")]
    public string? RelatedDocumentType { get; set; }

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
    /// 事务时间
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "事务时间")]
    public DateTime TransactionTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 事务原因
    /// </summary>
    [SugarColumn(Length = 200, IsNullable = true)]
    [Display(Name = "事务原因")]
    public string? Reason { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 仓库
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }

    /// <summary>
    /// 导航属性 - 库位
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(LocationId))]
    public WarehouseLocation? Location { get; set; }

    /// <summary>
    /// 导航属性 - 操作员
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(OperatorId))]
    public User? Operator { get; set; }
}

/// <summary>
/// 库存预警配置
/// </summary>
[SugarTable("inventory_alerts")]
public class InventoryAlert : BaseEntity
{
    /// <summary>
    /// 物料编码
    /// </summary>
    [SugarColumn(Length = 50, IsNullable = false)]
    [Display(Name = "物料编码")]
    [Required(ErrorMessage = "物料编码不能为空")]
    public string MaterialCode { get; set; } = string.Empty;

    /// <summary>
    /// 仓库ID
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "仓库ID")]
    public long WarehouseId { get; set; }

    /// <summary>
    /// 预警类型
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "预警类型")]
    public AlertType AlertType { get; set; }

    /// <summary>
    /// 最小库存量
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "最小库存量")]
    public decimal? MinQuantity { get; set; }

    /// <summary>
    /// 最大库存量
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "最大库存量")]
    public decimal? MaxQuantity { get; set; }

    /// <summary>
    /// 安全库存量
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "安全库存量")]
    public decimal? SafetyStockQuantity { get; set; }

    /// <summary>
    /// 过期预警天数
    /// </summary>
    [SugarColumn(IsNullable = true)]
    [Display(Name = "过期预警天数")]
    public int? ExpiryWarningDays { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "是否启用")]
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 预警级别
    /// </summary>
    [SugarColumn(IsNullable = false)]
    [Display(Name = "预警级别")]
    public AlertLevel AlertLevel { get; set; } = AlertLevel.Warning;

    /// <summary>
    /// 通知方式（JSON格式）
    /// </summary>
    [SugarColumn(ColumnDataType = "text", IsNullable = true)]
    [Display(Name = "通知方式")]
    public string? NotificationMethods { get; set; }

    /// <summary>
    /// 备注
    /// </summary>
    [SugarColumn(Length = 500, IsNullable = true)]
    [Display(Name = "备注")]
    public string? Remarks { get; set; }

    /// <summary>
    /// 导航属性 - 仓库
    /// </summary>
    [Navigate(NavigateType.OneToOne, nameof(WarehouseId))]
    public Warehouse? Warehouse { get; set; }
} 