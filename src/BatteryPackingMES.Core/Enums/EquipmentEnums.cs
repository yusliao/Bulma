using System.ComponentModel;

namespace BatteryPackingMES.Core.Enums;

/// <summary>
/// 设备类型枚举
/// </summary>
public enum EquipmentType
{
    /// <summary>
    /// 电池组装设备
    /// </summary>
    [Description("电池组装设备")]
    BatteryAssembly = 1,

    /// <summary>
    /// 包装设备
    /// </summary>
    [Description("包装设备")]
    PackagingEquipment = 2,

    /// <summary>
    /// 测试设备
    /// </summary>
    [Description("测试设备")]
    TestingEquipment = 3,

    /// <summary>
    /// 检测设备
    /// </summary>
    [Description("检测设备")]
    InspectionEquipment = 4,

    /// <summary>
    /// 搬运设备
    /// </summary>
    [Description("搬运设备")]
    HandlingEquipment = 5,

    /// <summary>
    /// 存储设备
    /// </summary>
    [Description("存储设备")]
    StorageEquipment = 6,

    /// <summary>
    /// 传送设备
    /// </summary>
    [Description("传送设备")]
    ConveyorEquipment = 7,

    /// <summary>
    /// 机器人
    /// </summary>
    [Description("机器人")]
    Robot = 8,

    /// <summary>
    /// 视觉系统
    /// </summary>
    [Description("视觉系统")]
    VisionSystem = 9,

    /// <summary>
    /// 控制系统
    /// </summary>
    [Description("控制系统")]
    ControlSystem = 10,

    /// <summary>
    /// 充电设备
    /// </summary>
    [Description("充电设备")]
    ChargingEquipment = 11,

    /// <summary>
    /// 老化设备
    /// </summary>
    [Description("老化设备")]
    AgingEquipment = 12,

    /// <summary>
    /// 激光设备
    /// </summary>
    [Description("激光设备")]
    LaserEquipment = 13,

    /// <summary>
    /// 焊接设备
    /// </summary>
    [Description("焊接设备")]
    WeldingEquipment = 14,

    /// <summary>
    /// 其他设备
    /// </summary>
    [Description("其他设备")]
    Other = 99
}

/// <summary>
/// 设备状态枚举
/// </summary>
public enum EquipmentStatus
{
    /// <summary>
    /// 空闲
    /// </summary>
    [Description("空闲")]
    Idle = 1,

    /// <summary>
    /// 运行中
    /// </summary>
    [Description("运行中")]
    Running = 2,

    /// <summary>
    /// 故障
    /// </summary>
    [Description("故障")]
    Fault = 3,

    /// <summary>
    /// 维护中
    /// </summary>
    [Description("维护中")]
    UnderMaintenance = 4,

    /// <summary>
    /// 停机
    /// </summary>
    [Description("停机")]
    Stopped = 5,

    /// <summary>
    /// 暂停
    /// </summary>
    [Description("暂停")]
    Paused = 6,

    /// <summary>
    /// 报警
    /// </summary>
    [Description("报警")]
    Alarm = 7,

    /// <summary>
    /// 等待
    /// </summary>
    [Description("等待")]
    Waiting = 8,

    /// <summary>
    /// 预热中
    /// </summary>
    [Description("预热中")]
    Warming = 9,

    /// <summary>
    /// 关闭
    /// </summary>
    [Description("关闭")]
    Shutdown = 10,

    /// <summary>
    /// 离线
    /// </summary>
    [Description("离线")]
    Offline = 11
}

/// <summary>
/// 维护类型枚举
/// </summary>
public enum MaintenanceType
{
    /// <summary>
    /// 预防性维护
    /// </summary>
    [Description("预防性维护")]
    Preventive = 1,

    /// <summary>
    /// 纠正性维护
    /// </summary>
    [Description("纠正性维护")]
    Corrective = 2,

    /// <summary>
    /// 预测性维护
    /// </summary>
    [Description("预测性维护")]
    Predictive = 3,

    /// <summary>
    /// 紧急维护
    /// </summary>
    [Description("紧急维护")]
    Emergency = 4,

    /// <summary>
    /// 改进性维护
    /// </summary>
    [Description("改进性维护")]
    Improvement = 5,

    /// <summary>
    /// 检验维护
    /// </summary>
    [Description("检验维护")]
    Inspection = 6
}

/// <summary>
/// 维护级别枚举
/// </summary>
public enum MaintenanceLevel
{
    /// <summary>
    /// 一级维护
    /// </summary>
    [Description("一级维护")]
    Level1 = 1,

    /// <summary>
    /// 二级维护
    /// </summary>
    [Description("二级维护")]
    Level2 = 2,

    /// <summary>
    /// 三级维护
    /// </summary>
    [Description("三级维护")]
    Level3 = 3,

    /// <summary>
    /// 大修
    /// </summary>
    [Description("大修")]
    Overhaul = 4
}

/// <summary>
/// 维护状态枚举
/// </summary>
public enum MaintenanceStatus
{
    /// <summary>
    /// 计划中
    /// </summary>
    [Description("计划中")]
    Planned = 1,

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
    /// 已取消
    /// </summary>
    [Description("已取消")]
    Cancelled = 4,

    /// <summary>
    /// 延期
    /// </summary>
    [Description("延期")]
    Postponed = 5
}

/// <summary>
/// 报警类型枚举
/// </summary>
public enum AlarmType
{
    /// <summary>
    /// 系统报警
    /// </summary>
    [Description("系统报警")]
    System = 1,

    /// <summary>
    /// 设备故障
    /// </summary>
    [Description("设备故障")]
    EquipmentFault = 2,

    /// <summary>
    /// 安全报警
    /// </summary>
    [Description("安全报警")]
    Safety = 3,

    /// <summary>
    /// 质量报警
    /// </summary>
    [Description("质量报警")]
    Quality = 4,

    /// <summary>
    /// 温度报警
    /// </summary>
    [Description("温度报警")]
    Temperature = 5,

    /// <summary>
    /// 压力报警
    /// </summary>
    [Description("压力报警")]
    Pressure = 6,

    /// <summary>
    /// 振动报警
    /// </summary>
    [Description("振动报警")]
    Vibration = 7,

    /// <summary>
    /// 电气报警
    /// </summary>
    [Description("电气报警")]
    Electrical = 8,

    /// <summary>
    /// 机械报警
    /// </summary>
    [Description("机械报警")]
    Mechanical = 9,

    /// <summary>
    /// 通信报警
    /// </summary>
    [Description("通信报警")]
    Communication = 10,

    /// <summary>
    /// 操作报警
    /// </summary>
    [Description("操作报警")]
    Operation = 11,

    /// <summary>
    /// 其他报警
    /// </summary>
    [Description("其他报警")]
    Other = 99
}

/// <summary>
/// 报警级别枚举
/// </summary>
public enum AlarmLevel
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
    Critical = 4,

    /// <summary>
    /// 紧急
    /// </summary>
    [Description("紧急")]
    Emergency = 5
}

/// <summary>
/// 报警状态枚举
/// </summary>
public enum AlarmStatus
{
    /// <summary>
    /// 活动
    /// </summary>
    [Description("活动")]
    Active = 1,

    /// <summary>
    /// 已确认
    /// </summary>
    [Description("已确认")]
    Acknowledged = 2,

    /// <summary>
    /// 已解决
    /// </summary>
    [Description("已解决")]
    Resolved = 3,

    /// <summary>
    /// 已关闭
    /// </summary>
    [Description("已关闭")]
    Closed = 4,

    /// <summary>
    /// 已抑制
    /// </summary>
    [Description("已抑制")]
    Suppressed = 5
}

/// <summary>
/// 设备操作类型枚举
/// </summary>
public enum EquipmentOperationType
{
    /// <summary>
    /// 启动
    /// </summary>
    [Description("启动")]
    Start = 1,

    /// <summary>
    /// 停止
    /// </summary>
    [Description("停止")]
    Stop = 2,

    /// <summary>
    /// 暂停
    /// </summary>
    [Description("暂停")]
    Pause = 3,

    /// <summary>
    /// 复位
    /// </summary>
    [Description("复位")]
    Reset = 4,

    /// <summary>
    /// 参数设置
    /// </summary>
    [Description("参数设置")]
    ParameterSetting = 5,

    /// <summary>
    /// 模式切换
    /// </summary>
    [Description("模式切换")]
    ModeSwitch = 6,

    /// <summary>
    /// 状态查询
    /// </summary>
    [Description("状态查询")]
    StatusQuery = 7,

    /// <summary>
    /// 报警确认
    /// </summary>
    [Description("报警确认")]
    AlarmAcknowledge = 8,

    /// <summary>
    /// 维护操作
    /// </summary>
    [Description("维护操作")]
    Maintenance = 9,

    /// <summary>
    /// 校准操作
    /// </summary>
    [Description("校准操作")]
    Calibration = 10,

    /// <summary>
    /// 数据采集
    /// </summary>
    [Description("数据采集")]
    DataCollection = 11,

    /// <summary>
    /// 远程控制
    /// </summary>
    [Description("远程控制")]
    RemoteControl = 12,

    /// <summary>
    /// 配置更新
    /// </summary>
    [Description("配置更新")]
    ConfigurationUpdate = 13,

    /// <summary>
    /// 其他操作
    /// </summary>
    [Description("其他操作")]
    Other = 99
} 