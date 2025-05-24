using System.ComponentModel;

namespace BatteryPackingMES.Core.Enums;

/// <summary>
/// 工序类型枚举
/// </summary>
public enum ProcessType
{
    /// <summary>
    /// 电芯包装
    /// </summary>
    [Description("电芯包装")]
    CellPacking = 1,

    /// <summary>
    /// 模组包装
    /// </summary>
    [Description("模组包装")]
    ModulePacking = 2,

    /// <summary>
    /// Pack包装
    /// </summary>
    [Description("Pack包装")]
    PackPacking = 3,

    /// <summary>
    /// 栈板包装
    /// </summary>
    [Description("栈板包装")]
    PalletPacking = 4
} 