using System.ComponentModel;

namespace BatteryPackingMES.Core.Enums;

/// <summary>
/// 仓库类型枚举
/// </summary>
public enum WarehouseType
{
    /// <summary>
    /// 普通仓库
    /// </summary>
    [Description("普通仓库")]
    General = 1,

    /// <summary>
    /// 原材料仓库
    /// </summary>
    [Description("原材料仓库")]
    RawMaterial = 2,

    /// <summary>
    /// 半成品仓库
    /// </summary>
    [Description("半成品仓库")]
    WorkInProgress = 3,

    /// <summary>
    /// 成品仓库
    /// </summary>
    [Description("成品仓库")]
    FinishedGoods = 4,

    /// <summary>
    /// 退货仓库
    /// </summary>
    [Description("退货仓库")]
    Returns = 5,

    /// <summary>
    /// 维修仓库
    /// </summary>
    [Description("维修仓库")]
    Maintenance = 6,

    /// <summary>
    /// 不合格品仓库
    /// </summary>
    [Description("不合格品仓库")]
    Defective = 7,

    /// <summary>
    /// 危险品仓库
    /// </summary>
    [Description("危险品仓库")]
    Hazardous = 8,

    /// <summary>
    /// 冷藏仓库
    /// </summary>
    [Description("冷藏仓库")]
    Refrigerated = 9
}

/// <summary>
/// 库位类型枚举
/// </summary>
public enum LocationType
{
    /// <summary>
    /// 普通库位
    /// </summary>
    [Description("普通库位")]
    Normal = 1,

    /// <summary>
    /// 立体库库位
    /// </summary>
    [Description("立体库库位")]
    AutomatedStorage = 2,

    /// <summary>
    /// 高位库位
    /// </summary>
    [Description("高位库位")]
    HighRack = 3,

    /// <summary>
    /// 地面库位
    /// </summary>
    [Description("地面库位")]
    FloorStorage = 4,

    /// <summary>
    /// 流水线库位
    /// </summary>
    [Description("流水线库位")]
    ConveyorLine = 5,

    /// <summary>
    /// 缓存库位
    /// </summary>
    [Description("缓存库位")]
    Buffer = 6,

    /// <summary>
    /// 拣选库位
    /// </summary>
    [Description("拣选库位")]
    Picking = 7,

    /// <summary>
    /// 临时库位
    /// </summary>
    [Description("临时库位")]
    Temporary = 8
}

/// <summary>
/// 库位状态枚举
/// </summary>
public enum LocationStatus
{
    /// <summary>
    /// 可用
    /// </summary>
    [Description("可用")]
    Available = 1,

    /// <summary>
    /// 占用
    /// </summary>
    [Description("占用")]
    Occupied = 2,

    /// <summary>
    /// 部分占用
    /// </summary>
    [Description("部分占用")]
    PartiallyOccupied = 3,

    /// <summary>
    /// 预留
    /// </summary>
    [Description("预留")]
    Reserved = 4,

    /// <summary>
    /// 锁定
    /// </summary>
    [Description("锁定")]
    Locked = 5,

    /// <summary>
    /// 维护中
    /// </summary>
    [Description("维护中")]
    UnderMaintenance = 6,

    /// <summary>
    /// 禁用
    /// </summary>
    [Description("禁用")]
    Disabled = 7
}

/// <summary>
/// 物料类型枚举
/// </summary>
public enum MaterialType
{
    /// <summary>
    /// 原材料
    /// </summary>
    [Description("原材料")]
    RawMaterial = 1,

    /// <summary>
    /// 电池芯
    /// </summary>
    [Description("电池芯")]
    Cell = 2,

    /// <summary>
    /// 电池模组
    /// </summary>
    [Description("电池模组")]
    Module = 3,

    /// <summary>
    /// 电池包
    /// </summary>
    [Description("电池包")]
    Pack = 4,

    /// <summary>
    /// 包装材料
    /// </summary>
    [Description("包装材料")]
    PackagingMaterial = 5,

    /// <summary>
    /// 辅助材料
    /// </summary>
    [Description("辅助材料")]
    AuxiliaryMaterial = 6,

    /// <summary>
    /// 备件
    /// </summary>
    [Description("备件")]
    SparePart = 7,

    /// <summary>
    /// 工具
    /// </summary>
    [Description("工具")]
    Tool = 8,

    /// <summary>
    /// 半成品
    /// </summary>
    [Description("半成品")]
    WorkInProgress = 9,

    /// <summary>
    /// 成品
    /// </summary>
    [Description("成品")]
    FinishedGoods = 10
}

/// <summary>
/// 质量状态枚举
/// </summary>
public enum QualityStatus
{
    /// <summary>
    /// 未检
    /// </summary>
    [Description("未检")]
    NotInspected = 1,

    /// <summary>
    /// 合格
    /// </summary>
    [Description("合格")]
    Qualified = 2,

    /// <summary>
    /// 不合格
    /// </summary>
    [Description("不合格")]
    Unqualified = 3,

    /// <summary>
    /// 待检
    /// </summary>
    [Description("待检")]
    PendingInspection = 4,

    /// <summary>
    /// 检验中
    /// </summary>
    [Description("检验中")]
    UnderInspection = 5,

    /// <summary>
    /// 免检
    /// </summary>
    [Description("免检")]
    ExemptFromInspection = 6
}

/// <summary>
/// 库存状态枚举
/// </summary>
public enum InventoryStatus
{
    /// <summary>
    /// 可用
    /// </summary>
    [Description("可用")]
    Available = 1,

    /// <summary>
    /// 冻结
    /// </summary>
    [Description("冻结")]
    Frozen = 2,

    /// <summary>
    /// 预留
    /// </summary>
    [Description("预留")]
    Reserved = 3,

    /// <summary>
    /// 锁定
    /// </summary>
    [Description("锁定")]
    Locked = 4,

    /// <summary>
    /// 损坏
    /// </summary>
    [Description("损坏")]
    Damaged = 5,

    /// <summary>
    /// 过期
    /// </summary>
    [Description("过期")]
    Expired = 6,

    /// <summary>
    /// 报废
    /// </summary>
    [Description("报废")]
    Scrapped = 7,

    /// <summary>
    /// 已出库
    /// </summary>
    [Description("已出库")]
    OutOfStock = 8
}

/// <summary>
/// 库存事务类型枚举
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// 入库
    /// </summary>
    [Description("入库")]
    Inbound = 1,

    /// <summary>
    /// 出库
    /// </summary>
    [Description("出库")]
    Outbound = 2,

    /// <summary>
    /// 移库
    /// </summary>
    [Description("移库")]
    Transfer = 3,

    /// <summary>
    /// 盘点调整
    /// </summary>
    [Description("盘点调整")]
    InventoryAdjustment = 4,

    /// <summary>
    /// 生产消耗
    /// </summary>
    [Description("生产消耗")]
    ProductionConsumption = 5,

    /// <summary>
    /// 生产产出
    /// </summary>
    [Description("生产产出")]
    ProductionOutput = 6,

    /// <summary>
    /// 退料
    /// </summary>
    [Description("退料")]
    MaterialReturn = 7,

    /// <summary>
    /// 报废
    /// </summary>
    [Description("报废")]
    Scrap = 8,

    /// <summary>
    /// 冻结
    /// </summary>
    [Description("冻结")]
    Freeze = 9,

    /// <summary>
    /// 解冻
    /// </summary>
    [Description("解冻")]
    Unfreeze = 10,

    /// <summary>
    /// 预留
    /// </summary>
    [Description("预留")]
    Reserve = 11,

    /// <summary>
    /// 取消预留
    /// </summary>
    [Description("取消预留")]
    CancelReservation = 12
}

/// <summary>
/// 预警类型枚举
/// </summary>
public enum AlertType
{
    /// <summary>
    /// 低库存预警
    /// </summary>
    [Description("低库存预警")]
    LowStock = 1,

    /// <summary>
    /// 高库存预警
    /// </summary>
    [Description("高库存预警")]
    HighStock = 2,

    /// <summary>
    /// 零库存预警
    /// </summary>
    [Description("零库存预警")]
    ZeroStock = 3,

    /// <summary>
    /// 安全库存预警
    /// </summary>
    [Description("安全库存预警")]
    SafetyStock = 4,

    /// <summary>
    /// 过期预警
    /// </summary>
    [Description("过期预警")]
    ExpiryWarning = 5,

    /// <summary>
    /// 即将过期预警
    /// </summary>
    [Description("即将过期预警")]
    ExpiringWarning = 6,

    /// <summary>
    /// 批次质量预警
    /// </summary>
    [Description("批次质量预警")]
    QualityWarning = 7,

    /// <summary>
    /// 库位满载预警
    /// </summary>
    [Description("库位满载预警")]
    LocationFullWarning = 8
}

/// <summary>
/// 预警级别枚举
/// </summary>
public enum AlertLevel
{
    /// <summary>
    /// 信息
    /// </summary>
    [Description("信息")]
    Info = 1,

    /// <summary>
    /// 警告
    /// </summary>
    [Description("警告")]
    Warning = 2,

    /// <summary>
    /// 错误
    /// </summary>
    [Description("错误")]
    Error = 3,

    /// <summary>
    /// 严重
    /// </summary>
    [Description("严重")]
    Critical = 4
} 