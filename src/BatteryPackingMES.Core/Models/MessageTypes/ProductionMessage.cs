namespace BatteryPackingMES.Core.Models.MessageTypes;

/// <summary>
/// 生产消息基类
/// </summary>
public abstract class ProductionMessage
{
    /// <summary>
    /// 消息ID
    /// </summary>
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 时间戳
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 用户ID
    /// </summary>
    public long? UserId { get; set; }

    /// <summary>
    /// 工作站编号
    /// </summary>
    public string? WorkstationCode { get; set; }

    /// <summary>
    /// 消息类型
    /// </summary>
    public string MessageType { get; set; } = string.Empty;
}

/// <summary>
/// 产线状态变更消息
/// </summary>
public class ProductionLineStatusMessage : ProductionMessage
{
    public ProductionLineStatusMessage()
    {
        MessageType = "ProductionLineStatus";
    }

    /// <summary>
    /// 产线编号
    /// </summary>
    public string LineCode { get; set; } = string.Empty;

    /// <summary>
    /// 旧状态
    /// </summary>
    public string OldStatus { get; set; } = string.Empty;

    /// <summary>
    /// 新状态
    /// </summary>
    public string NewStatus { get; set; } = string.Empty;

    /// <summary>
    /// 状态原因
    /// </summary>
    public string? Reason { get; set; }
}

/// <summary>
/// 产品质量检测消息
/// </summary>
public class QualityCheckMessage : ProductionMessage
{
    public QualityCheckMessage()
    {
        MessageType = "QualityCheck";
    }

    /// <summary>
    /// 产品条码
    /// </summary>
    public string ProductBarcode { get; set; } = string.Empty;

    /// <summary>
    /// 检测结果
    /// </summary>
    public string CheckResult { get; set; } = string.Empty;

    /// <summary>
    /// 不良代码
    /// </summary>
    public string? DefectCode { get; set; }

    /// <summary>
    /// 检测数据
    /// </summary>
    public Dictionary<string, object>? CheckData { get; set; }
}

/// <summary>
/// 设备告警消息
/// </summary>
public class EquipmentAlarmMessage : ProductionMessage
{
    public EquipmentAlarmMessage()
    {
        MessageType = "EquipmentAlarm";
    }

    /// <summary>
    /// 设备编号
    /// </summary>
    public string EquipmentCode { get; set; } = string.Empty;

    /// <summary>
    /// 告警类型
    /// </summary>
    public string AlarmType { get; set; } = string.Empty;

    /// <summary>
    /// 告警级别
    /// </summary>
    public string AlarmLevel { get; set; } = string.Empty;

    /// <summary>
    /// 告警描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 是否需要停机
    /// </summary>
    public bool RequireShutdown { get; set; }
}

/// <summary>
/// 生产订单变更消息
/// </summary>
public class ProductionOrderMessage : ProductionMessage
{
    public ProductionOrderMessage()
    {
        MessageType = "ProductionOrder";
    }

    /// <summary>
    /// 订单编号
    /// </summary>
    public string OrderNumber { get; set; } = string.Empty;

    /// <summary>
    /// 操作类型 (Create, Update, Cancel, Complete)
    /// </summary>
    public string OperationType { get; set; } = string.Empty;

    /// <summary>
    /// 产品型号
    /// </summary>
    public string ProductModel { get; set; } = string.Empty;

    /// <summary>
    /// 计划数量
    /// </summary>
    public int PlannedQuantity { get; set; }

    /// <summary>
    /// 已生产数量
    /// </summary>
    public int ProducedQuantity { get; set; }
} 