namespace BatteryPackingMES.Core.Events;

/// <summary>
/// 生产批次创建事件
/// </summary>
public class ProductionBatchCreatedEvent : BaseEvent
{
    public ProductionBatchCreatedEvent(string batchNumber, string productModel, int plannedQuantity, string workshopCode)
    {
        AggregateId = batchNumber;
        BatchNumber = batchNumber;
        ProductModel = productModel;
        PlannedQuantity = plannedQuantity;
        WorkshopCode = workshopCode;
    }

    public string BatchNumber { get; }
    public string ProductModel { get; }
    public int PlannedQuantity { get; }
    public string WorkshopCode { get; }
}

/// <summary>
/// 生产批次状态变更事件
/// </summary>
public class ProductionBatchStatusChangedEvent : BaseEvent
{
    public ProductionBatchStatusChangedEvent(string batchNumber, string oldStatus, string newStatus, string? reason = null)
    {
        AggregateId = batchNumber;
        BatchNumber = batchNumber;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Reason = reason;
    }

    public string BatchNumber { get; }
    public string OldStatus { get; }
    public string NewStatus { get; }
    public string? Reason { get; }
}

/// <summary>
/// 生产批次完成事件
/// </summary>
public class ProductionBatchCompletedEvent : BaseEvent
{
    public ProductionBatchCompletedEvent(string batchNumber, int actualQuantity, int qualifiedQuantity, TimeSpan actualDuration)
    {
        AggregateId = batchNumber;
        BatchNumber = batchNumber;
        ActualQuantity = actualQuantity;
        QualifiedQuantity = qualifiedQuantity;
        ActualDuration = actualDuration;
        CompletionRate = (double)actualQuantity / (double)ActualQuantity * 100;
        QualificationRate = (double)qualifiedQuantity / (double)actualQuantity * 100;
    }

    public string BatchNumber { get; }
    public int ActualQuantity { get; }
    public int QualifiedQuantity { get; }
    public TimeSpan ActualDuration { get; }
    public double CompletionRate { get; }
    public double QualificationRate { get; }
}

/// <summary>
/// 质量检测事件
/// </summary>
public class QualityInspectionCompletedEvent : BaseEvent
{
    public QualityInspectionCompletedEvent(string productBarcode, string batchNumber, bool isQualified, Dictionary<string, object> testResults)
    {
        AggregateId = productBarcode;
        ProductBarcode = productBarcode;
        BatchNumber = batchNumber;
        IsQualified = isQualified;
        TestResults = testResults;
    }

    public string ProductBarcode { get; }
    public string BatchNumber { get; }
    public bool IsQualified { get; }
    public Dictionary<string, object> TestResults { get; }
}

/// <summary>
/// 设备状态变更事件
/// </summary>
public class EquipmentStatusChangedEvent : BaseEvent
{
    public EquipmentStatusChangedEvent(string equipmentCode, string oldStatus, string newStatus, string? reason = null)
    {
        AggregateId = equipmentCode;
        EquipmentCode = equipmentCode;
        OldStatus = oldStatus;
        NewStatus = newStatus;
        Reason = reason;
    }

    public string EquipmentCode { get; }
    public string OldStatus { get; }
    public string NewStatus { get; }
    public string? Reason { get; }
}

/// <summary>
/// 设备故障事件
/// </summary>
public class EquipmentFailureEvent : BaseEvent
{
    public EquipmentFailureEvent(string equipmentCode, string faultType, string description, string severity)
    {
        AggregateId = equipmentCode;
        EquipmentCode = equipmentCode;
        FaultType = faultType;
        Description = description;
        Severity = severity;
        RequiresMaintenance = severity == "Critical" || severity == "High";
    }

    public string EquipmentCode { get; }
    public string FaultType { get; }
    public string Description { get; }
    public string Severity { get; }
    public bool RequiresMaintenance { get; }
}

/// <summary>
/// 工艺参数超限事件
/// </summary>
public class ProcessParameterOutOfRangeEvent : BaseEvent
{
    public ProcessParameterOutOfRangeEvent(string parameterName, object currentValue, object expectedRange, string batchNumber)
    {
        AggregateId = $"{batchNumber}_{parameterName}";
        ParameterName = parameterName;
        CurrentValue = currentValue;
        ExpectedRange = expectedRange;
        BatchNumber = batchNumber;
    }

    public string ParameterName { get; }
    public object CurrentValue { get; }
    public object ExpectedRange { get; }
    public string BatchNumber { get; }
} 