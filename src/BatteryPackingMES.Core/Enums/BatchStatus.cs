using System.ComponentModel;

namespace BatteryPackingMES.Core.Enums;

/// <summary>
/// 批次状态枚举
/// </summary>
public enum BatchStatus
{
    /// <summary>
    /// 已创建
    /// </summary>
    [Description("已创建")]
    Created = 0,

    /// <summary>
    /// 已计划
    /// </summary>
    [Description("已计划")]
    Planned = 1,

    /// <summary>
    /// 进行中
    /// </summary>
    [Description("进行中")]
    InProgress = 2,

    /// <summary>
    /// 暂停
    /// </summary>
    [Description("暂停")]
    Paused = 3,

    /// <summary>
    /// 已完成
    /// </summary>
    [Description("已完成")]
    Completed = 4,

    /// <summary>
    /// 已取消
    /// </summary>
    [Description("已取消")]
    Cancelled = 5
}

/// <summary>
/// 批次优先级枚举
/// </summary>
public enum BatchPriority
{
    /// <summary>
    /// 低优先级
    /// </summary>
    [Description("低")]
    Low = 1,

    /// <summary>
    /// 普通优先级
    /// </summary>
    [Description("普通")]
    Normal = 2,

    /// <summary>
    /// 高优先级
    /// </summary>
    [Description("高")]
    High = 3,

    /// <summary>
    /// 紧急
    /// </summary>
    [Description("紧急")]
    Urgent = 4
}

/// <summary>
/// 产品项状态枚举
/// </summary>
public enum ProductItemStatus
{
    /// <summary>
    /// 已创建
    /// </summary>
    [Description("已创建")]
    Created = 1,

    /// <summary>
    /// 进行中
    /// </summary>
    [Description("进行中")]
    InProgress = 2,

    /// <summary>
    /// 已完成
    /// </summary>
    [Description("已完成")]
    Completed = 3,

    /// <summary>
    /// 质量检查
    /// </summary>
    [Description("质量检查")]
    QualityChecked = 4,

    /// <summary>
    /// 不合格
    /// </summary>
    [Description("不合格")]
    Defective = 5,

    /// <summary>
    /// 返工
    /// </summary>
    [Description("返工")]
    Rework = 6,

    /// <summary>
    /// 已报废
    /// </summary>
    [Description("已报废")]
    Scrapped = 7,

    /// <summary>
    /// 已降级
    /// </summary>
    [Description("已降级")]
    Downgraded = 8,

    /// <summary>
    /// 已入库
    /// </summary>
    [Description("已入库")]
    Stored = 9
}

/// <summary>
/// 质量等级枚举
/// </summary>
public enum QualityGrade
{
    /// <summary>
    /// 未分级
    /// </summary>
    [Description("未分级")]
    Ungraded = 0,

    /// <summary>
    /// A级优秀
    /// </summary>
    [Description("A级")]
    A = 1,

    /// <summary>
    /// B级良好
    /// </summary>
    [Description("B级")]
    B = 2,

    /// <summary>
    /// C级合格
    /// </summary>
    [Description("C级")]
    C = 3,

    /// <summary>
    /// 不合格
    /// </summary>
    [Description("不合格")]
    Defective = 4
}

/// <summary>
/// 不合格品处理方式
/// </summary>
public enum DefectiveHandlingAction
{
    /// <summary>
    /// 返工
    /// </summary>
    [Description("返工")]
    Rework = 1,

    /// <summary>
    /// 报废
    /// </summary>
    [Description("报废")]
    Scrap = 2,

    /// <summary>
    /// 降级
    /// </summary>
    [Description("降级")]
    Downgrade = 3
} 