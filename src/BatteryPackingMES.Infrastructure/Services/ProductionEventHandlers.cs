using BatteryPackingMES.Core.Events;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 生产批次创建事件处理器
/// </summary>
public class ProductionBatchCreatedEventHandler : IEventHandler<ProductionBatchCreatedEvent>
{
    private readonly ILogger<ProductionBatchCreatedEventHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ICacheService _cacheService;

    public ProductionBatchCreatedEventHandler(
        ILogger<ProductionBatchCreatedEventHandler> logger,
        IServiceProvider serviceProvider,
        IMessageQueueService messageQueueService,
        ICacheService cacheService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageQueueService = messageQueueService;
        _cacheService = cacheService;
    }

    public async Task HandleAsync(ProductionBatchCreatedEvent eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("处理生产批次创建事件: 批次号 {BatchNumber}, 产品型号 {ProductModel}, 计划数量 {PlannedQuantity}",
                eventData.BatchNumber, eventData.ProductModel, eventData.PlannedQuantity);

            using var scope = _serviceProvider.CreateScope();

            // 1. 自动分配资源
            await AllocateResources(eventData, scope);

            // 2. 生成作业指导书
            await GenerateWorkInstructions(eventData, scope);

            // 3. 初始化质量检查点
            await InitializeQualityCheckpoints(eventData, scope);

            // 4. 发送通知
            await SendNotifications(eventData);

            // 5. 更新缓存统计
            await UpdateProductionStatistics(eventData);

            _logger.LogInformation("生产批次创建事件处理完成: {BatchNumber}", eventData.BatchNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理生产批次创建事件失败: {BatchNumber}", eventData.BatchNumber);
            throw;
        }
    }

    private async Task AllocateResources(ProductionBatchCreatedEvent eventData, IServiceScope scope)
    {
        try
        {
            // 查找可用设备
            var equipmentRepository = scope.ServiceProvider.GetRequiredService<IRepository<Equipment>>();
            var availableEquipment = await equipmentRepository.GetListAsync(
                e => e.CurrentStatus == Core.Enums.EquipmentStatus.Idle && e.IsEnabled);

            if (availableEquipment.Any())
            {
                var selectedEquipment = availableEquipment.First();
                
                // 分配设备
                selectedEquipment.CurrentStatus = Core.Enums.EquipmentStatus.Running;
                selectedEquipment.UpdatedTime = DateTime.UtcNow;
                
                await equipmentRepository.UpdateAsync(selectedEquipment);

                _logger.LogInformation("已为批次 {BatchNumber} 分配设备 {EquipmentCode}", 
                    eventData.BatchNumber, selectedEquipment.EquipmentCode);
            }
            else
            {
                _logger.LogWarning("批次 {BatchNumber} 在车间 {WorkshopCode} 未找到可用设备", 
                    eventData.BatchNumber, eventData.WorkshopCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "资源分配失败: {BatchNumber}", eventData.BatchNumber);
        }
    }

    private async Task GenerateWorkInstructions(ProductionBatchCreatedEvent eventData, IServiceScope scope)
    {
        try
        {
            var instructionData = new
            {
                BatchNumber = eventData.BatchNumber,
                ProductModel = eventData.ProductModel,
                PlannedQuantity = eventData.PlannedQuantity,
                WorkshopCode = eventData.WorkshopCode,
                GeneratedAt = DateTime.UtcNow,
                Instructions = new[]
                {
                    "1. 检查原材料质量和规格",
                    "2. 设置设备参数",
                    "3. 执行标准操作程序",
                    "4. 记录工艺参数",
                    "5. 执行质量检查"
                }
            };

            // 缓存作业指导书
            var cacheKey = $"work_instructions:{eventData.BatchNumber}";
            await _cacheService.SetAsync(cacheKey, instructionData, TimeSpan.FromDays(30));

            _logger.LogInformation("已生成作业指导书: {BatchNumber}", eventData.BatchNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成作业指导书失败: {BatchNumber}", eventData.BatchNumber);
        }
    }

    private async Task InitializeQualityCheckpoints(ProductionBatchCreatedEvent eventData, IServiceScope scope)
    {
        try
        {
            var checkpoints = new[]
            {
                new { Stage = "incoming", Description = "来料检验", Required = true },
                new { Stage = "process", Description = "过程检验", Required = true },
                new { Stage = "final", Description = "成品检验", Required = true }
            };

            foreach (var checkpoint in checkpoints)
            {
                var checkpointData = new
                {
                    BatchNumber = eventData.BatchNumber,
                    Stage = checkpoint.Stage,
                    Description = checkpoint.Description,
                    Status = "Pending",
                    CreatedAt = DateTime.UtcNow
                };

                var cacheKey = $"quality_checkpoint:{eventData.BatchNumber}:{checkpoint.Stage}";
                await _cacheService.SetAsync(cacheKey, checkpointData, TimeSpan.FromDays(30));
            }

            _logger.LogInformation("已初始化质量检查点: {BatchNumber}", eventData.BatchNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "初始化质量检查点失败: {BatchNumber}", eventData.BatchNumber);
        }
    }

    private async Task SendNotifications(ProductionBatchCreatedEvent eventData)
    {
        try
        {
            var notification = new
            {
                Type = "ProductionBatchCreated",
                BatchNumber = eventData.BatchNumber,
                ProductModel = eventData.ProductModel,
                PlannedQuantity = eventData.PlannedQuantity,
                WorkshopCode = eventData.WorkshopCode,
                Message = $"生产批次 {eventData.BatchNumber} 已创建，产品型号: {eventData.ProductModel}，计划数量: {eventData.PlannedQuantity}",
                Timestamp = eventData.OccurredOn,
                Priority = "Normal"
            };

            await _messageQueueService.PublishAsync("production-notifications", notification);
            _logger.LogDebug("已发送生产批次创建通知: {BatchNumber}", eventData.BatchNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发送通知失败: {BatchNumber}", eventData.BatchNumber);
        }
    }

    private async Task UpdateProductionStatistics(ProductionBatchCreatedEvent eventData)
    {
        try
        {
            var today = DateTime.UtcNow.Date;
            var statsKey = $"production_stats:{eventData.WorkshopCode}:{today:yyyyMMdd}";
            
            var currentStats = await _cacheService.GetAsync<ProductionDailyStats>(statsKey) ?? new ProductionDailyStats();
            currentStats.TotalBatches++;
            currentStats.TotalPlannedQuantity += eventData.PlannedQuantity;
            currentStats.LastBatchCreated = eventData.BatchNumber;
            currentStats.LastUpdated = DateTime.UtcNow;

            await _cacheService.SetAsync(statsKey, currentStats, TimeSpan.FromDays(7));
            _logger.LogDebug("已更新生产统计: {WorkshopCode}", eventData.WorkshopCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新生产统计失败: {BatchNumber}", eventData.BatchNumber);
        }
    }
}

/// <summary>
/// 生产批次状态变更事件处理器
/// </summary>
public class ProductionBatchStatusChangedEventHandler : IEventHandler<ProductionBatchStatusChangedEvent>
{
    private readonly ILogger<ProductionBatchStatusChangedEventHandler> _logger;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ICacheService _cacheService;

    public ProductionBatchStatusChangedEventHandler(
        ILogger<ProductionBatchStatusChangedEventHandler> logger,
        IMessageQueueService messageQueueService,
        ICacheService cacheService)
    {
        _logger = logger;
        _messageQueueService = messageQueueService;
        _cacheService = cacheService;
    }

    public async Task HandleAsync(ProductionBatchStatusChangedEvent eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("处理生产批次状态变更事件: 批次号 {BatchNumber}, 状态 {OldStatus} -> {NewStatus}",
                eventData.BatchNumber, eventData.OldStatus, eventData.NewStatus);

            // 1. 更新批次状态缓存
            await UpdateBatchStatusCache(eventData);

            // 2. 处理状态相关的业务逻辑
            await ProcessStatusTransition(eventData);

            // 3. 发送状态变更通知
            await SendStatusChangeNotification(eventData);

            // 4. 更新统计信息
            await UpdateStatusStatistics(eventData);

            _logger.LogInformation("生产批次状态变更事件处理完成: {BatchNumber}", eventData.BatchNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理生产批次状态变更事件失败: {BatchNumber}", eventData.BatchNumber);
            throw;
        }
    }

    private async Task UpdateBatchStatusCache(ProductionBatchStatusChangedEvent eventData)
    {
        var cacheKey = $"batch_status:{eventData.BatchNumber}";
        var statusInfo = new
        {
            BatchNumber = eventData.BatchNumber,
            CurrentStatus = eventData.NewStatus,
            PreviousStatus = eventData.OldStatus,
            Reason = eventData.Reason,
            UpdatedAt = eventData.OccurredOn
        };

        await _cacheService.SetAsync(cacheKey, statusInfo, TimeSpan.FromDays(30));
    }

    private async Task ProcessStatusTransition(ProductionBatchStatusChangedEvent eventData)
    {
        switch (eventData.NewStatus)
        {
            case "InProgress":
                await HandleBatchStarted(eventData);
                break;
            case "Completed":
                await HandleBatchCompleted(eventData);
                break;
            case "Suspended":
                await HandleBatchSuspended(eventData);
                break;
            case "Cancelled":
                await HandleBatchCancelled(eventData);
                break;
        }
    }

    private async Task HandleBatchStarted(ProductionBatchStatusChangedEvent eventData)
    {
        _logger.LogInformation("生产批次开始: {BatchNumber}", eventData.BatchNumber);
        
        // 记录开始时间
        var cacheKey = $"batch_timing:{eventData.BatchNumber}";
        var timingInfo = new
        {
            BatchNumber = eventData.BatchNumber,
            StartTime = eventData.OccurredOn,
            Status = "Started"
        };
        
        await _cacheService.SetAsync(cacheKey, timingInfo, TimeSpan.FromDays(30));
    }

    private async Task HandleBatchCompleted(ProductionBatchStatusChangedEvent eventData)
    {
        _logger.LogInformation("生产批次完成: {BatchNumber}", eventData.BatchNumber);
        
        // 计算生产耗时
        var timingCacheKey = $"batch_timing:{eventData.BatchNumber}";
        var timingInfo = await _cacheService.GetAsync<dynamic>(timingCacheKey);
        
        if (timingInfo != null)
        {
            var startTime = (DateTimeOffset)timingInfo.StartTime;
            var duration = eventData.OccurredOn - startTime;
            
            var completionInfo = new
            {
                BatchNumber = eventData.BatchNumber,
                StartTime = startTime,
                EndTime = eventData.OccurredOn,
                Duration = duration,
                Status = "Completed"
            };
            
            await _cacheService.SetAsync(timingCacheKey, completionInfo, TimeSpan.FromDays(30));
            
            _logger.LogInformation("生产批次 {BatchNumber} 完成，耗时: {Duration}", 
                eventData.BatchNumber, duration);
        }
    }

    private async Task HandleBatchSuspended(ProductionBatchStatusChangedEvent eventData)
    {
        _logger.LogWarning("生产批次暂停: {BatchNumber}, 原因: {Reason}", 
            eventData.BatchNumber, eventData.Reason);
        
        // 发送紧急通知
        var urgentNotification = new
        {
            Type = "ProductionSuspended",
            BatchNumber = eventData.BatchNumber,
            Reason = eventData.Reason,
            Timestamp = eventData.OccurredOn,
            Priority = "High"
        };
        
        await _messageQueueService.PublishAsync("urgent-notifications", urgentNotification);
    }

    private async Task HandleBatchCancelled(ProductionBatchStatusChangedEvent eventData)
    {
        _logger.LogWarning("生产批次取消: {BatchNumber}, 原因: {Reason}", 
            eventData.BatchNumber, eventData.Reason);
        
        // 释放已分配的资源
        // 这里可以添加释放设备、库存等资源的逻辑
    }

    private async Task SendStatusChangeNotification(ProductionBatchStatusChangedEvent eventData)
    {
        var notification = new
        {
            Type = "ProductionBatchStatusChanged",
            BatchNumber = eventData.BatchNumber,
            OldStatus = eventData.OldStatus,
            NewStatus = eventData.NewStatus,
            Reason = eventData.Reason,
            Message = $"生产批次 {eventData.BatchNumber} 状态从 {eventData.OldStatus} 变更为 {eventData.NewStatus}",
            Timestamp = eventData.OccurredOn,
            Priority = DeterminePriority(eventData.NewStatus)
        };

        await _messageQueueService.PublishAsync("production-notifications", notification);
    }

    private async Task UpdateStatusStatistics(ProductionBatchStatusChangedEvent eventData)
    {
        var today = DateTime.UtcNow.Date;
        var statsKey = $"status_stats:{eventData.NewStatus}:{today:yyyyMMdd}";
        
        // 使用 string 存储计数并转换为 int
        var currentCountStr = await _cacheService.GetAsync<string>(statsKey);
        var currentCount = int.TryParse(currentCountStr, out var count) ? count : 0;
        await _cacheService.SetAsync(statsKey, (currentCount + 1).ToString(), TimeSpan.FromDays(30));
    }

    private static string DeterminePriority(string status)
    {
        return status switch
        {
            "Suspended" or "Cancelled" => "High",
            "Completed" => "Normal",
            _ => "Low"
        };
    }
}

/// <summary>
/// 质量检测完成事件处理器
/// </summary>
public class QualityInspectionCompletedEventHandler : IEventHandler<QualityInspectionCompletedEvent>
{
    private readonly ILogger<QualityInspectionCompletedEventHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ICacheService _cacheService;

    public QualityInspectionCompletedEventHandler(
        ILogger<QualityInspectionCompletedEventHandler> logger,
        IServiceProvider serviceProvider,
        IMessageQueueService messageQueueService,
        ICacheService cacheService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageQueueService = messageQueueService;
        _cacheService = cacheService;
    }

    public async Task HandleAsync(QualityInspectionCompletedEvent eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("处理质量检测完成事件: 产品条码 {ProductBarcode}, 批次号 {BatchNumber}, 检测结果 {IsQualified}",
                eventData.ProductBarcode, eventData.BatchNumber, eventData.IsQualified ? "合格" : "不合格");

            // 1. 记录质量检测结果
            await RecordQualityResult(eventData);

            // 2. 更新产品追溯信息
            await UpdateProductTraceability(eventData);

            // 3. 处理不合格品
            if (!eventData.IsQualified)
            {
                await HandleNonconformingProduct(eventData);
            }

            // 4. 更新质量统计
            await UpdateQualityStatistics(eventData);

            // 5. 发送质量通知
            await SendQualityNotification(eventData);

            _logger.LogInformation("质量检测事件处理完成: {ProductBarcode}", eventData.ProductBarcode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理质量检测事件失败: {ProductBarcode}", eventData.ProductBarcode);
            throw;
        }
    }

    private async Task RecordQualityResult(QualityInspectionCompletedEvent eventData)
    {
        var resultData = new
        {
            ProductBarcode = eventData.ProductBarcode,
            BatchNumber = eventData.BatchNumber,
            IsQualified = eventData.IsQualified,
            TestResults = eventData.TestResults,
            InspectionTime = eventData.OccurredOn,
            Inspector = eventData.UserId
        };

        var cacheKey = $"quality_result:{eventData.ProductBarcode}";
        await _cacheService.SetAsync(cacheKey, resultData, TimeSpan.FromDays(365)); // 质量记录保存1年
        
        _logger.LogDebug("已记录质量检测结果: {ProductBarcode}", eventData.ProductBarcode);
    }

    private async Task UpdateProductTraceability(QualityInspectionCompletedEvent eventData)
    {
        using var scope = _serviceProvider.CreateScope();
        var traceabilityRepository = scope.ServiceProvider.GetRequiredService<IRepository<ProductTraceability>>();

        var traceRecord = new ProductTraceability
        {
            SerialNumber = eventData.ProductBarcode,
            BatchNumber = eventData.BatchNumber,
            ProcessStep = "质量检验",
            OperationType = "检验",
            OperationTime = eventData.OccurredOn.DateTime,
            Operator = eventData.UserId?.ToString() ?? "System",
            TestParameters = System.Text.Json.JsonSerializer.Serialize(eventData.TestResults),
            QualityParameters = eventData.IsQualified ? "合格" : "不合格",
            IsQualityCheckpoint = true,
            CreatedTime = DateTime.UtcNow
        };

        await traceabilityRepository.AddAsync(traceRecord);
        _logger.LogDebug("已更新产品追溯记录: {ProductBarcode}", eventData.ProductBarcode);
    }

    private async Task HandleNonconformingProduct(QualityInspectionCompletedEvent eventData)
    {
        _logger.LogWarning("检测到不合格产品: {ProductBarcode}, 批次: {BatchNumber}", 
            eventData.ProductBarcode, eventData.BatchNumber);

        // 1. 隔离不合格品
        var isolationData = new
        {
            ProductBarcode = eventData.ProductBarcode,
            BatchNumber = eventData.BatchNumber,
            IsolationTime = eventData.OccurredOn,
            Reason = "质量检测不合格",
            TestResults = eventData.TestResults,
            Status = "Isolated"
        };

        var isolationKey = $"isolated_product:{eventData.ProductBarcode}";
        await _cacheService.SetAsync(isolationKey, isolationData, TimeSpan.FromDays(30));

        // 2. 发送不合格品通知
        var nonconformingNotification = new
        {
            Type = "NonconformingProduct",
            ProductBarcode = eventData.ProductBarcode,
            BatchNumber = eventData.BatchNumber,
            TestResults = eventData.TestResults,
            Message = $"产品 {eventData.ProductBarcode} 质量检测不合格",
            Timestamp = eventData.OccurredOn,
            Priority = "High"
        };

        await _messageQueueService.PublishAsync("quality-alerts", nonconformingNotification);

        // 3. 检查是否需要批次质量预警
        await CheckBatchQualityTrend(eventData);
    }

    private async Task CheckBatchQualityTrend(QualityInspectionCompletedEvent eventData)
    {
        var batchQualityKey = $"batch_quality:{eventData.BatchNumber}";
        var batchQuality = await _cacheService.GetAsync<BatchQualityData>(batchQualityKey) ?? new BatchQualityData();

        batchQuality.TotalInspected++;
        if (!eventData.IsQualified)
        {
            batchQuality.NonconformingCount++;
        }

        batchQuality.QualificationRate = (double)(batchQuality.TotalInspected - batchQuality.NonconformingCount) / batchQuality.TotalInspected * 100;

        await _cacheService.SetAsync(batchQualityKey, batchQuality, TimeSpan.FromDays(30));

        // 如果不合格率超过阈值，发送批次质量预警
        if (batchQuality.QualificationRate < 95.0 && batchQuality.TotalInspected >= 10)
        {
            var batchAlert = new
            {
                Type = "BatchQualityAlert",
                BatchNumber = eventData.BatchNumber,
                QualificationRate = batchQuality.QualificationRate,
                TotalInspected = batchQuality.TotalInspected,
                NonconformingCount = batchQuality.NonconformingCount,
                Message = $"批次 {eventData.BatchNumber} 合格率 {batchQuality.QualificationRate:F1}% 低于标准",
                Timestamp = eventData.OccurredOn,
                Priority = "Critical"
            };

            await _messageQueueService.PublishAsync("quality-alerts", batchAlert);
            
            _logger.LogWarning("批次质量预警: 批次 {BatchNumber}, 合格率 {QualificationRate:F1}%", 
                eventData.BatchNumber, batchQuality.QualificationRate);
        }
    }

    private async Task UpdateQualityStatistics(QualityInspectionCompletedEvent eventData)
    {
        var today = DateTime.UtcNow.Date;
        var qualityStatsKey = $"quality_stats:{today:yyyyMMdd}";
        
        var qualityStats = await _cacheService.GetAsync<QualityDailyStats>(qualityStatsKey) ?? new QualityDailyStats();
        qualityStats.TotalInspected++;
        
        if (eventData.IsQualified)
        {
            qualityStats.QualifiedCount++;
        }
        else
        {
            qualityStats.NonconformingCount++;
        }
        
        qualityStats.QualificationRate = (double)qualityStats.QualifiedCount / qualityStats.TotalInspected * 100;
        qualityStats.LastUpdated = DateTime.UtcNow;

        await _cacheService.SetAsync(qualityStatsKey, qualityStats, TimeSpan.FromDays(7));
    }

    private async Task SendQualityNotification(QualityInspectionCompletedEvent eventData)
    {
        var notification = new
        {
            Type = "QualityInspectionCompleted",
            ProductBarcode = eventData.ProductBarcode,
            BatchNumber = eventData.BatchNumber,
            IsQualified = eventData.IsQualified,
            TestResults = eventData.TestResults,
            Message = $"产品 {eventData.ProductBarcode} 质量检测完成，结果: {(eventData.IsQualified ? "合格" : "不合格")}",
            Timestamp = eventData.OccurredOn,
            Priority = eventData.IsQualified ? "Low" : "High"
        };

        await _messageQueueService.PublishAsync("quality-notifications", notification);
    }
}

/// <summary>
/// 设备故障事件处理器
/// </summary>
public class EquipmentFailureEventHandler : IEventHandler<EquipmentFailureEvent>
{
    private readonly ILogger<EquipmentFailureEventHandler> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageQueueService _messageQueueService;
    private readonly ICacheService _cacheService;

    public EquipmentFailureEventHandler(
        ILogger<EquipmentFailureEventHandler> logger,
        IServiceProvider serviceProvider,
        IMessageQueueService messageQueueService,
        ICacheService cacheService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _messageQueueService = messageQueueService;
        _cacheService = cacheService;
    }

    public async Task HandleAsync(EquipmentFailureEvent eventData, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogError("处理设备故障事件: 设备 {EquipmentCode}, 故障类型 {FaultType}, 严重程度 {Severity}",
                eventData.EquipmentCode, eventData.FaultType, eventData.Severity);

            // 1. 立即停机处理
            await HandleEmergencyShutdown(eventData);

            // 2. 记录故障信息
            await RecordFailureInformation(eventData);

            // 3. 发送紧急通知
            await SendEmergencyNotification(eventData);

            // 4. 调度维护任务
            if (eventData.RequiresMaintenance)
            {
                await ScheduleMaintenanceTask(eventData);
            }

            // 5. 更新设备统计
            await UpdateEquipmentStatistics(eventData);

            _logger.LogInformation("设备故障事件处理完成: {EquipmentCode}", eventData.EquipmentCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理设备故障事件失败: {EquipmentCode}", eventData.EquipmentCode);
            throw;
        }
    }

    private async Task HandleEmergencyShutdown(EquipmentFailureEvent eventData)
    {
        using var scope = _serviceProvider.CreateScope();
        var equipmentRepository = scope.ServiceProvider.GetRequiredService<IRepository<Equipment>>();

        var equipment = await equipmentRepository.GetFirstAsync(e => e.EquipmentCode == eventData.EquipmentCode);
        if (equipment != null)
        {
            equipment.CurrentStatus = Core.Enums.EquipmentStatus.Fault;
            equipment.Remarks = eventData.Description;
            equipment.UpdatedTime = DateTime.UtcNow;
            
            await equipmentRepository.UpdateAsync(equipment);
            
            _logger.LogWarning("设备 {EquipmentCode} 已紧急停机", eventData.EquipmentCode);
        }
    }

    private async Task RecordFailureInformation(EquipmentFailureEvent eventData)
    {
        var failureRecord = new
        {
            EquipmentCode = eventData.EquipmentCode,
            FaultType = eventData.FaultType,
            Description = eventData.Description,
            Severity = eventData.Severity,
            FailureTime = eventData.OccurredOn,
            RequiresMaintenance = eventData.RequiresMaintenance,
            Status = "Active"
        };

        var failureKey = $"equipment_failure:{eventData.EquipmentCode}:{eventData.OccurredOn:yyyyMMddHHmmss}";
        await _cacheService.SetAsync(failureKey, failureRecord, TimeSpan.FromDays(90));

        // 记录到故障历史
        var historyKey = $"failure_history:{eventData.EquipmentCode}";
        var historyList = await _cacheService.GetAsync<List<object>>(historyKey) ?? new List<object>();
        historyList.Add(failureRecord);
        
        // 保留最近100条记录
        if (historyList.Count > 100)
        {
            historyList.RemoveAt(0);
        }
        
        await _cacheService.SetAsync(historyKey, historyList, TimeSpan.FromDays(365));
    }

    private async Task SendEmergencyNotification(EquipmentFailureEvent eventData)
    {
        var emergencyNotification = new
        {
            Type = "EquipmentFailure",
            EquipmentCode = eventData.EquipmentCode,
            FaultType = eventData.FaultType,
            Description = eventData.Description,
            Severity = eventData.Severity,
            RequiresMaintenance = eventData.RequiresMaintenance,
            Message = $"设备 {eventData.EquipmentCode} 发生 {eventData.Severity} 级故障: {eventData.Description}",
            Timestamp = eventData.OccurredOn,
            Priority = DetermineNotificationPriority(eventData.Severity)
        };

        await _messageQueueService.PublishAsync("emergency-alerts", emergencyNotification);
        
        _logger.LogCritical("已发送设备故障紧急通知: {EquipmentCode}", eventData.EquipmentCode);
    }

    private async Task ScheduleMaintenanceTask(EquipmentFailureEvent eventData)
    {
        var maintenanceTask = new
        {
            EquipmentCode = eventData.EquipmentCode,
            TaskType = "Emergency",
            FaultType = eventData.FaultType,
            Description = eventData.Description,
            Severity = eventData.Severity,
            ScheduledTime = eventData.OccurredOn.AddMinutes(15), // 15分钟内响应
            Priority = DetermineMaintenancePriority(eventData.Severity),
            Status = "Scheduled",
            CreatedAt = eventData.OccurredOn
        };

        await _messageQueueService.PublishAsync("maintenance-tasks", maintenanceTask);
        
        _logger.LogInformation("已调度维护任务: 设备 {EquipmentCode}, 优先级 {Priority}", 
            eventData.EquipmentCode, maintenanceTask.Priority);
    }

    private async Task UpdateEquipmentStatistics(EquipmentFailureEvent eventData)
    {
        var today = DateTime.UtcNow.Date;
        var statsKey = $"equipment_failure_stats:{today:yyyyMMdd}";
        
        var failureStats = await _cacheService.GetAsync<EquipmentFailureStats>(statsKey) ?? new EquipmentFailureStats();
        failureStats.TotalFailures++;
        
        switch (eventData.Severity)
        {
            case "Critical":
                failureStats.CriticalFailures++;
                break;
            case "High":
                failureStats.HighFailures++;
                break;
            case "Medium":
                failureStats.MediumFailures++;
                break;
            default:
                failureStats.LowFailures++;
                break;
        }
        
        failureStats.LastFailureTime = eventData.OccurredOn.DateTime;

        await _cacheService.SetAsync(statsKey, failureStats, TimeSpan.FromDays(30));
    }

    private static string DetermineNotificationPriority(string severity)
    {
        return severity switch
        {
            "Critical" => "Critical",
            "High" => "High",
            "Medium" => "Medium",
            _ => "Low"
        };
    }

    private static string DetermineMaintenancePriority(string severity)
    {
        return severity switch
        {
            "Critical" => "Urgent",
            "High" => "High",
            "Medium" => "Medium",
            _ => "Low"
        };
    }
}

// 数据模型类
public class ProductionDailyStats
{
    public int TotalBatches { get; set; }
    public int TotalPlannedQuantity { get; set; }
    public string? LastBatchCreated { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class BatchQualityData
{
    public int TotalInspected { get; set; }
    public int NonconformingCount { get; set; }
    public double QualificationRate { get; set; }
}

public class QualityDailyStats
{
    public int TotalInspected { get; set; }
    public int QualifiedCount { get; set; }
    public int NonconformingCount { get; set; }
    public double QualificationRate { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class EquipmentFailureStats
{
    public int TotalFailures { get; set; }
    public int CriticalFailures { get; set; }
    public int HighFailures { get; set; }
    public int MediumFailures { get; set; }
    public int LowFailures { get; set; }
    public DateTime LastFailureTime { get; set; }
} 