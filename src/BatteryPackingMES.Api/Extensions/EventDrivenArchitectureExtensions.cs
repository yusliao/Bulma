using BatteryPackingMES.Core.Events;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BatteryPackingMES.Api.Extensions;

/// <summary>
/// 事件驱动架构扩展方法
/// </summary>
public static class EventDrivenArchitectureExtensions
{
    /// <summary>
    /// 添加事件驱动架构服务
    /// </summary>
    public static IServiceCollection AddEventDrivenArchitecture(this IServiceCollection services, IConfiguration configuration)
    {
        // 1. 注册核心事件服务
        services.AddSingleton<IEventHandlerRegistry, EventHandlerRegistry>();
        services.AddScoped<IEventStore, EventStoreService>();
        
        // 2. 根据配置选择事件总线实现
        var useEnhancedEventBus = configuration.GetValue<bool>("EventBus:UseEnhanced", true);
        if (useEnhancedEventBus)
        {
            services.AddSingleton<IEventBus, EnhancedEventBusService>();
        }
        else
        {
            services.AddSingleton<IEventBus, EventBusService>();
        }

        // 3. 注册事件处理器
        RegisterEventHandlers(services);

        // 4. 配置事件处理器自动注册
        services.AddHostedService<EventHandlerRegistrationService>();

        return services;
    }

    /// <summary>
    /// 注册所有事件处理器
    /// </summary>
    private static void RegisterEventHandlers(IServiceCollection services)
    {
        // 生产事件处理器
        services.AddScoped<IEventHandler<ProductionBatchCreatedEvent>, ProductionBatchCreatedEventHandler>();
        services.AddScoped<IEventHandler<ProductionBatchStatusChangedEvent>, ProductionBatchStatusChangedEventHandler>();
        services.AddScoped<IEventHandler<ProductionBatchCompletedEvent>, ProductionBatchCompletedEventHandler>();

        // 质量事件处理器
        services.AddScoped<IEventHandler<QualityInspectionCompletedEvent>, QualityInspectionCompletedEventHandler>();

        // 设备事件处理器
        services.AddScoped<IEventHandler<EquipmentStatusChangedEvent>, EquipmentStatusChangedEventHandler>();
        services.AddScoped<IEventHandler<EquipmentFailureEvent>, EquipmentFailureEventHandler>();

        // 参数事件处理器
        services.AddScoped<IEventHandler<ProcessParameterOutOfRangeEvent>, ProcessParameterOutOfRangeEventHandler>();
        services.AddScoped<IEventHandler<ProcessParameterAnomalyDetectedEvent>, ParameterAnomalyEventHandler>();
        services.AddScoped<IEventHandler<ProcessParameterAlertTriggeredEvent>, ParameterAlertEventHandler>();
        services.AddScoped<IEventHandler<ParameterAggregationCompletedEvent>, ParameterAggregationEventHandler>();
    }

    /// <summary>
    /// 配置事件处理器自动订阅
    /// </summary>
    public static IServiceCollection ConfigureEventSubscriptions(this IServiceCollection services)
    {
        services.AddHostedService<EventSubscriptionService>();
        return services;
    }
}

/// <summary>
/// 事件处理器注册后台服务
/// </summary>
public class EventHandlerRegistrationService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventHandlerRegistrationService> _logger;

    public EventHandlerRegistrationService(
        IServiceProvider serviceProvider, 
        ILogger<EventHandlerRegistrationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var handlerRegistry = scope.ServiceProvider.GetRequiredService<IEventHandlerRegistry>();

        try
        {
            // 自动注册所有事件处理器
            RegisterHandlers<ProductionBatchCreatedEvent>(handlerRegistry, scope);
            RegisterHandlers<ProductionBatchStatusChangedEvent>(handlerRegistry, scope);
            RegisterHandlers<ProductionBatchCompletedEvent>(handlerRegistry, scope);
            RegisterHandlers<QualityInspectionCompletedEvent>(handlerRegistry, scope);
            RegisterHandlers<EquipmentStatusChangedEvent>(handlerRegistry, scope);
            RegisterHandlers<EquipmentFailureEvent>(handlerRegistry, scope);
            RegisterHandlers<ProcessParameterOutOfRangeEvent>(handlerRegistry, scope);
            RegisterHandlers<ProcessParameterAnomalyDetectedEvent>(handlerRegistry, scope);
            RegisterHandlers<ProcessParameterAlertTriggeredEvent>(handlerRegistry, scope);
            RegisterHandlers<ParameterAggregationCompletedEvent>(handlerRegistry, scope);

            _logger.LogInformation("事件处理器注册完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事件处理器注册失败");
        }

        await Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("事件处理器注册服务已停止");
        return Task.CompletedTask;
    }

    private void RegisterHandlers<T>(IEventHandlerRegistry registry, IServiceScope scope) where T : BaseEvent
    {
        var handlers = scope.ServiceProvider.GetServices<IEventHandler<T>>();
        foreach (var handler in handlers)
        {
            registry.RegisterHandler(handler);
            _logger.LogDebug("已注册事件处理器: {EventType} -> {HandlerType}", 
                typeof(T).Name, handler.GetType().Name);
        }
    }
}

/// <summary>
/// 事件订阅后台服务
/// </summary>
public class EventSubscriptionService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EventSubscriptionService> _logger;

    public EventSubscriptionService(
        IServiceProvider serviceProvider,
        ILogger<EventSubscriptionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        try
        {
            // 订阅所有事件类型
            await SubscribeToEvents(eventBus);
            _logger.LogInformation("事件订阅配置完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事件订阅配置失败");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("事件订阅服务已停止");
        return Task.CompletedTask;
    }

    private async Task SubscribeToEvents(IEventBus eventBus)
    {
        // 生产事件订阅
        await eventBus.SubscribeAsync<ProductionBatchCreatedEvent>(DefaultEventHandler);
        await eventBus.SubscribeAsync<ProductionBatchStatusChangedEvent>(DefaultEventHandler);
        await eventBus.SubscribeAsync<ProductionBatchCompletedEvent>(DefaultEventHandler);

        // 质量事件订阅
        await eventBus.SubscribeAsync<QualityInspectionCompletedEvent>(DefaultEventHandler);

        // 设备事件订阅
        await eventBus.SubscribeAsync<EquipmentStatusChangedEvent>(DefaultEventHandler);
        await eventBus.SubscribeAsync<EquipmentFailureEvent>(DefaultEventHandler);

        // 参数事件订阅
        await eventBus.SubscribeAsync<ProcessParameterOutOfRangeEvent>(DefaultEventHandler);
        await eventBus.SubscribeAsync<ProcessParameterAnomalyDetectedEvent>(DefaultEventHandler);
        await eventBus.SubscribeAsync<ProcessParameterAlertTriggeredEvent>(DefaultEventHandler);
        await eventBus.SubscribeAsync<ParameterAggregationCompletedEvent>(DefaultEventHandler);

        _logger.LogInformation("已订阅所有事件类型");
    }

    private async Task DefaultEventHandler<T>(T @event, CancellationToken cancellationToken) where T : BaseEvent
    {
        // 这是一个通用的事件处理入口，实际处理由注册的处理器完成
        _logger.LogDebug("收到事件: {EventType}, ID: {EventId}", @event.EventType, @event.EventId);
        await Task.CompletedTask;
    }
}

/// <summary>
/// 生产批次完成事件处理器
/// </summary>
public class ProductionBatchCompletedEventHandler : IEventHandler<ProductionBatchCompletedEvent>
{
    private readonly ILogger<ProductionBatchCompletedEventHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ICacheService _cacheService;

    public ProductionBatchCompletedEventHandler(
        ILogger<ProductionBatchCompletedEventHandler> logger,
        IServiceProvider serviceProvider,
        IMessageQueueService messageQueueService,
        ICacheService cacheService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageQueueService = messageQueueService;
        _cacheService = cacheService;
    }

    public async Task HandleAsync(ProductionBatchCompletedEvent eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("处理生产批次完成事件: 批次号 {BatchNumber}, 实际数量 {ActualQuantity}, 合格数量 {QualifiedQuantity}",
                eventData.BatchNumber, eventData.ActualQuantity, eventData.QualifiedQuantity);

            // 1. 生成生产报告
            await GenerateProductionReport(eventData);

            // 2. 释放资源
            await ReleaseResources(eventData);

            // 3. 更新库存
            await UpdateInventory(eventData);

            // 4. 发送完成通知
            await SendCompletionNotification(eventData);

            // 5. 更新统计数据
            await UpdateCompletionStatistics(eventData);

            _logger.LogInformation("生产批次完成事件处理完成: {BatchNumber}", eventData.BatchNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理生产批次完成事件失败: {BatchNumber}", eventData.BatchNumber);
            throw;
        }
    }

    private async Task GenerateProductionReport(ProductionBatchCompletedEvent eventData)
    {
        var report = new
        {
            BatchNumber = eventData.BatchNumber,
            ActualQuantity = eventData.ActualQuantity,
            QualifiedQuantity = eventData.QualifiedQuantity,
            ActualDuration = eventData.ActualDuration,
            CompletionRate = eventData.CompletionRate,
            QualificationRate = eventData.QualificationRate,
            CompletedAt = eventData.OccurredOn,
            Performance = new
            {
                Efficiency = eventData.CompletionRate >= 95 ? "优秀" : eventData.CompletionRate >= 80 ? "良好" : "需改进",
                Quality = eventData.QualificationRate >= 99 ? "优秀" : eventData.QualificationRate >= 95 ? "良好" : "需改进"
            }
        };

        var reportKey = $"production_report:{eventData.BatchNumber}";
        await _cacheService.SetAsync(reportKey, report, TimeSpan.FromDays(90));

        _logger.LogInformation("已生成生产报告: {BatchNumber}, 完成率 {CompletionRate:F1}%, 合格率 {QualificationRate:F1}%",
            eventData.BatchNumber, eventData.CompletionRate, eventData.QualificationRate);
    }

    private async Task ReleaseResources(ProductionBatchCompletedEvent eventData)
    {
        using var scope = _serviceProvider.CreateScope();
        var equipmentRepository = scope.ServiceProvider.GetRequiredService<IRepository<Equipment>>();

        var equipment = await equipmentRepository.GetListAsync(e => e.Remarks == eventData.BatchNumber);
        foreach (var item in equipment)
        {
            item.CurrentStatus = Core.Enums.EquipmentStatus.Idle;
            item.Remarks = null;
            item.UpdatedTime = DateTime.UtcNow;
            await equipmentRepository.UpdateAsync(item);
        }

        _logger.LogInformation("已释放生产资源: {BatchNumber}, 设备数量 {EquipmentCount}", 
            eventData.BatchNumber, equipment.Count());
    }

    private async Task UpdateInventory(ProductionBatchCompletedEvent eventData)
    {
        // 这里可以添加更新库存的逻辑
        var inventoryUpdate = new
        {
            BatchNumber = eventData.BatchNumber,
            CompletedQuantity = eventData.QualifiedQuantity,
            DefectiveQuantity = eventData.ActualQuantity - eventData.QualifiedQuantity,
            UpdatedAt = eventData.OccurredOn
        };

        await _messageQueueService.PublishAsync("inventory-updates", inventoryUpdate);
        _logger.LogDebug("已发送库存更新请求: {BatchNumber}", eventData.BatchNumber);
    }

    private async Task SendCompletionNotification(ProductionBatchCompletedEvent eventData)
    {
        var notification = new
        {
            Type = "ProductionBatchCompleted",
            BatchNumber = eventData.BatchNumber,
            ActualQuantity = eventData.ActualQuantity,
            QualifiedQuantity = eventData.QualifiedQuantity,
            CompletionRate = eventData.CompletionRate,
            QualificationRate = eventData.QualificationRate,
            Duration = eventData.ActualDuration,
            Message = $"生产批次 {eventData.BatchNumber} 已完成，实际产量 {eventData.ActualQuantity}，合格数量 {eventData.QualifiedQuantity}",
            Timestamp = eventData.OccurredOn,
            Priority = "Normal"
        };

        await _messageQueueService.PublishAsync("production-notifications", notification);
    }

    private async Task UpdateCompletionStatistics(ProductionBatchCompletedEvent eventData)
    {
        var today = DateTime.UtcNow.Date;
        var statsKey = $"completion_stats:{today:yyyyMMdd}";

        var completionStats = await _cacheService.GetAsync<CompletionDailyStats>(statsKey) ?? new CompletionDailyStats();
        completionStats.TotalCompleted++;
        completionStats.TotalActualQuantity += eventData.ActualQuantity;
        completionStats.TotalQualifiedQuantity += eventData.QualifiedQuantity;
        completionStats.AverageCompletionRate = (completionStats.AverageCompletionRate * (completionStats.TotalCompleted - 1) + eventData.CompletionRate) / completionStats.TotalCompleted;
        completionStats.AverageQualificationRate = (completionStats.AverageQualificationRate * (completionStats.TotalCompleted - 1) + eventData.QualificationRate) / completionStats.TotalCompleted;
        completionStats.LastBatchCompleted = eventData.BatchNumber;
        completionStats.LastUpdated = DateTime.UtcNow;

        await _cacheService.SetAsync(statsKey, completionStats, TimeSpan.FromDays(7));
    }
}

/// <summary>
/// 设备状态变更事件处理器
/// </summary>
public class EquipmentStatusChangedEventHandler : IEventHandler<EquipmentStatusChangedEvent>
{
    private readonly ILogger<EquipmentStatusChangedEventHandler> _logger;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ICacheService _cacheService;

    public EquipmentStatusChangedEventHandler(
        ILogger<EquipmentStatusChangedEventHandler> logger,
        IMessageQueueService messageQueueService,
        ICacheService cacheService)
    {
        _logger = logger;
        _messageQueueService = messageQueueService;
        _cacheService = cacheService;
    }

    public async Task HandleAsync(EquipmentStatusChangedEvent eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("处理设备状态变更事件: 设备 {EquipmentCode}, 状态 {OldStatus} -> {NewStatus}",
                eventData.EquipmentCode, eventData.OldStatus, eventData.NewStatus);

            // 1. 更新设备状态缓存
            await UpdateEquipmentStatusCache(eventData);

            // 2. 发送状态变更通知
            await SendStatusChangeNotification(eventData);

            // 3. 更新设备利用率统计
            await UpdateEquipmentUtilization(eventData);

            _logger.LogInformation("设备状态变更事件处理完成: {EquipmentCode}", eventData.EquipmentCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理设备状态变更事件失败: {EquipmentCode}", eventData.EquipmentCode);
            throw;
        }
    }

    private async Task UpdateEquipmentStatusCache(EquipmentStatusChangedEvent eventData)
    {
        var statusInfo = new
        {
            EquipmentCode = eventData.EquipmentCode,
            CurrentStatus = eventData.NewStatus,
            PreviousStatus = eventData.OldStatus,
            Reason = eventData.Reason,
            UpdatedAt = eventData.OccurredOn
        };

        var cacheKey = $"equipment_status:{eventData.EquipmentCode}";
        await _cacheService.SetAsync(cacheKey, statusInfo, TimeSpan.FromDays(30));
    }

    private async Task SendStatusChangeNotification(EquipmentStatusChangedEvent eventData)
    {
        var notification = new
        {
            Type = "EquipmentStatusChanged",
            EquipmentCode = eventData.EquipmentCode,
            OldStatus = eventData.OldStatus,
            NewStatus = eventData.NewStatus,
            Reason = eventData.Reason,
            Message = $"设备 {eventData.EquipmentCode} 状态从 {eventData.OldStatus} 变更为 {eventData.NewStatus}",
            Timestamp = eventData.OccurredOn,
            Priority = DeterminePriority(eventData.NewStatus)
        };

        await _messageQueueService.PublishAsync("equipment-notifications", notification);
    }

    private async Task UpdateEquipmentUtilization(EquipmentStatusChangedEvent eventData)
    {
        var today = DateTime.UtcNow.Date;
        var utilizationKey = $"equipment_utilization:{eventData.EquipmentCode}:{today:yyyyMMdd}";

        // 记录状态变更时间点
        var statusChange = new
        {
            Status = eventData.NewStatus,
            Timestamp = eventData.OccurredOn,
            Reason = eventData.Reason
        };

        var statusChanges = await _cacheService.GetAsync<List<object>>(utilizationKey) ?? new List<object>();
        statusChanges.Add(statusChange);
        
        // 保留最近1000条记录
        if (statusChanges.Count > 1000)
        {
            statusChanges.RemoveAt(0);
        }
        
        await _cacheService.SetAsync(utilizationKey, statusChanges, TimeSpan.FromDays(30));
    }

    private static string DeterminePriority(string status)
    {
        return status switch
        {
            "Fault" or "Emergency" => "High",
            "Maintenance" => "Medium",
            _ => "Low"
        };
    }
}

/// <summary>
/// 工艺参数超限事件处理器
/// </summary>
public class ProcessParameterOutOfRangeEventHandler : IEventHandler<ProcessParameterOutOfRangeEvent>
{
    private readonly ILogger<ProcessParameterOutOfRangeEventHandler> _logger;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ICacheService _cacheService;

    public ProcessParameterOutOfRangeEventHandler(
        ILogger<ProcessParameterOutOfRangeEventHandler> logger,
        IMessageQueueService messageQueueService,
        ICacheService cacheService)
    {
        _logger = logger;
        _messageQueueService = messageQueueService;
        _cacheService = cacheService;
    }

    public async Task HandleAsync(ProcessParameterOutOfRangeEvent eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogWarning("处理工艺参数超限事件: 参数 {ParameterName}, 当前值 {CurrentValue}, 期望范围 {ExpectedRange}",
                eventData.ParameterName, eventData.CurrentValue, eventData.ExpectedRange);

            // 1. 记录超限信息
            await RecordOutOfRangeIncident(eventData);

            // 2. 发送超限预警
            await SendOutOfRangeAlert(eventData);

            // 3. 更新参数统计
            await UpdateParameterStatistics(eventData);

            _logger.LogInformation("工艺参数超限事件处理完成: {ParameterName}", eventData.ParameterName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理工艺参数超限事件失败: {ParameterName}", eventData.ParameterName);
            throw;
        }
    }

    private async Task RecordOutOfRangeIncident(ProcessParameterOutOfRangeEvent eventData)
    {
        var incident = new
        {
            ParameterName = eventData.ParameterName,
            CurrentValue = eventData.CurrentValue,
            ExpectedRange = eventData.ExpectedRange,
            BatchNumber = eventData.BatchNumber,
            OccurredAt = eventData.OccurredOn,
            Severity = "Warning"
        };

        var incidentKey = $"parameter_incident:{eventData.ParameterName}:{eventData.OccurredOn:yyyyMMddHHmmss}";
        await _cacheService.SetAsync(incidentKey, incident, TimeSpan.FromDays(30));

        // 添加到参数历史
        var historyKey = $"parameter_history:{eventData.ParameterName}";
        var historyList = await _cacheService.GetAsync<List<object>>(historyKey) ?? new List<object>();
        historyList.Add(incident);
        
        // 保留最近500条记录
        if (historyList.Count > 500)
        {
            historyList.RemoveAt(0);
        }
        
        await _cacheService.SetAsync(historyKey, historyList, TimeSpan.FromDays(90));
    }

    private async Task SendOutOfRangeAlert(ProcessParameterOutOfRangeEvent eventData)
    {
        var alert = new
        {
            Type = "ParameterOutOfRange",
            ParameterName = eventData.ParameterName,
            CurrentValue = eventData.CurrentValue,
            ExpectedRange = eventData.ExpectedRange,
            BatchNumber = eventData.BatchNumber,
            Message = $"参数 {eventData.ParameterName} 超出正常范围，当前值: {eventData.CurrentValue}，期望范围: {eventData.ExpectedRange}",
            Timestamp = eventData.OccurredOn,
            Priority = "Medium"
        };

        await _messageQueueService.PublishAsync("parameter-alerts", alert);
    }

    private async Task UpdateParameterStatistics(ProcessParameterOutOfRangeEvent eventData)
    {
        var today = DateTime.UtcNow.Date;
        var statsKey = $"parameter_outofrange_stats:{eventData.ParameterName}:{today:yyyyMMdd}";

        var currentCountStr = await _cacheService.GetAsync<string>(statsKey);
        var currentCount = int.TryParse(currentCountStr, out var count) ? count : 0;
        await _cacheService.SetAsync(statsKey, (currentCount + 1).ToString(), TimeSpan.FromDays(30));
    }
}

// 数据模型类
public class CompletionDailyStats
{
    public int TotalCompleted { get; set; }
    public int TotalActualQuantity { get; set; }
    public int TotalQualifiedQuantity { get; set; }
    public double AverageCompletionRate { get; set; }
    public double AverageQualificationRate { get; set; }
    public string? LastBatchCompleted { get; set; }
    public DateTime LastUpdated { get; set; }
} 