using SqlSugar;
using Microsoft.Extensions.Logging;
using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Enums;
using System.Text.Json;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 设备管理服务实现
/// </summary>
public class EquipmentService : IEquipmentService
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<EquipmentService> _logger;
    private static readonly SemaphoreSlim _statusUpdateSemaphore = new(1, 1);

    public EquipmentService(ISqlSugarClient db, ILogger<EquipmentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region 设备管理

    /// <summary>
    /// 创建设备
    /// </summary>
    public async Task<long> CreateEquipmentAsync(Equipment equipment)
    {
        try
        {
            // 检查设备编码是否重复
            var exists = await _db.Queryable<Equipment>()
                .Where(e => e.EquipmentCode == equipment.EquipmentCode)
                .AnyAsync();

            if (exists)
            {
                throw new InvalidOperationException($"设备编码 {equipment.EquipmentCode} 已存在");
            }

            // 插入设备
            var id = await _db.Insertable(equipment).ExecuteReturnSnowflakeIdAsync();

            // 创建初始状态记录
            var statusRecord = new EquipmentStatusRecord
            {
                EquipmentId = id,
                EquipmentCode = equipment.EquipmentCode,
                PreviousStatus = null,
                CurrentStatus = equipment.CurrentStatus,
                StartTime = DateTime.Now,
                Reason = "设备创建"
            };

            await _db.Insertable(statusRecord).ExecuteCommandAsync();

            _logger.LogInformation("设备创建成功: {EquipmentCode}", equipment.EquipmentCode);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建设备失败: {EquipmentCode}", equipment.EquipmentCode);
            throw;
        }
    }

    /// <summary>
    /// 获取设备列表
    /// </summary>
    public async Task<List<Equipment>> GetEquipmentsAsync()
    {
        try
        {
            return await _db.Queryable<Equipment>()
                .Where(e => !e.IsDeleted)
                .OrderBy(e => e.EquipmentCode)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备列表失败");
            throw;
        }
    }

    /// <summary>
    /// 根据ID获取设备
    /// </summary>
    public async Task<Equipment?> GetEquipmentByIdAsync(long id)
    {
        try
        {
            return await _db.Queryable<Equipment>()
                .Where(e => e.Id == id && !e.IsDeleted)
                .FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据ID获取设备失败: {EquipmentId}", id);
            throw;
        }
    }

    /// <summary>
    /// 根据编码获取设备
    /// </summary>
    public async Task<Equipment?> GetEquipmentByCodeAsync(string equipmentCode)
    {
        try
        {
            return await _db.Queryable<Equipment>()
                .Where(e => e.EquipmentCode == equipmentCode && !e.IsDeleted)
                .FirstAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据编码获取设备失败: {EquipmentCode}", equipmentCode);
            throw;
        }
    }

    /// <summary>
    /// 更新设备信息
    /// </summary>
    public async Task<bool> UpdateEquipmentAsync(Equipment equipment)
    {
        try
        {
            var result = await _db.Updateable(equipment)
                .IgnoreColumns(e => new { e.CreatedTime, e.CreatedBy })
                .ExecuteCommandAsync();

            _logger.LogInformation("设备更新成功: {EquipmentCode}", equipment.EquipmentCode);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新设备失败: {EquipmentCode}", equipment.EquipmentCode);
            throw;
        }
    }

    /// <summary>
    /// 删除设备
    /// </summary>
    public async Task<bool> DeleteEquipmentAsync(long id)
    {
        try
        {
            var equipment = await GetEquipmentByIdAsync(id);
            if (equipment == null)
            {
                return false;
            }

            // 检查是否有关联的活动记录
            var hasActiveRecords = await _db.Queryable<EquipmentStatusRecord>()
                .Where(r => r.EquipmentId == id && r.EndTime == null)
                .AnyAsync();

            if (hasActiveRecords)
            {
                throw new InvalidOperationException("设备有活动状态记录，无法删除");
            }

            var result = await _db.Updateable<Equipment>()
                .SetColumns(e => new Equipment { IsDeleted = true, UpdatedTime = DateTime.Now })
                .Where(e => e.Id == id)
                .ExecuteCommandAsync();

            _logger.LogInformation("设备删除成功: {EquipmentCode}", equipment.EquipmentCode);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除设备失败: {EquipmentId}", id);
            throw;
        }
    }

    /// <summary>
    /// 分页查询设备
    /// </summary>
    public async Task<(List<Equipment> Items, int Total)> GetEquipmentsPagedAsync(EquipmentQueryRequest request)
    {
        try
        {
            var query = _db.Queryable<Equipment>()
                .Where(e => !e.IsDeleted);

            // 添加过滤条件
            if (!string.IsNullOrEmpty(request.EquipmentCode))
            {
                query = query.Where(e => e.EquipmentCode.Contains(request.EquipmentCode));
            }

            if (!string.IsNullOrEmpty(request.EquipmentName))
            {
                query = query.Where(e => e.EquipmentName.Contains(request.EquipmentName));
            }

            if (request.EquipmentType.HasValue)
            {
                query = query.Where(e => e.EquipmentType == request.EquipmentType.Value);
            }

            if (request.CurrentStatus.HasValue)
            {
                query = query.Where(e => e.CurrentStatus == request.CurrentStatus.Value);
            }

            if (request.WorkstationId.HasValue)
            {
                query = query.Where(e => e.WorkstationId == request.WorkstationId.Value);
            }

            if (request.IsEnabled.HasValue)
            {
                query = query.Where(e => e.IsEnabled == request.IsEnabled.Value);
            }

            if (request.IsCritical.HasValue)
            {
                query = query.Where(e => e.IsCritical == request.IsCritical.Value);
            }

            // 获取总数
            var total = await query.CountAsync();

            // 分页查询
            var items = await query
                .OrderBy(e => e.EquipmentCode)
                .ToPageListAsync(request.PageIndex, request.PageSize);

            return (items, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询设备失败");
            throw;
        }
    }

    /// <summary>
    /// 根据类型获取设备列表
    /// </summary>
    public async Task<List<Equipment>> GetEquipmentsByTypeAsync(EquipmentType equipmentType)
    {
        try
        {
            return await _db.Queryable<Equipment>()
                .Where(e => e.EquipmentType == equipmentType && !e.IsDeleted && e.IsEnabled)
                .OrderBy(e => e.EquipmentCode)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据类型获取设备列表失败: {EquipmentType}", equipmentType);
            throw;
        }
    }

    /// <summary>
    /// 根据状态获取设备列表
    /// </summary>
    public async Task<List<Equipment>> GetEquipmentsByStatusAsync(EquipmentStatus status)
    {
        try
        {
            return await _db.Queryable<Equipment>()
                .Where(e => e.CurrentStatus == status && !e.IsDeleted)
                .OrderBy(e => e.EquipmentCode)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "根据状态获取设备列表失败: {Status}", status);
            throw;
        }
    }

    /// <summary>
    /// 获取工作站设备列表
    /// </summary>
    public async Task<List<Equipment>> GetEquipmentsByWorkstationAsync(long workstationId)
    {
        try
        {
            return await _db.Queryable<Equipment>()
                .Where(e => e.WorkstationId == workstationId && !e.IsDeleted && e.IsEnabled)
                .OrderBy(e => e.EquipmentCode)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取工作站设备列表失败: {WorkstationId}", workstationId);
            throw;
        }
    }

    #endregion

    #region 设备状态管理

    /// <summary>
    /// 更新设备状态
    /// </summary>
    public async Task<bool> UpdateEquipmentStatusAsync(EquipmentStatusUpdateRequest request)
    {
        await _statusUpdateSemaphore.WaitAsync();
        try
        {
            _db.Ado.BeginTran();

            // 获取设备当前状态
            var equipment = await _db.Queryable<Equipment>()
                .Where(e => e.Id == request.EquipmentId)
                .FirstAsync();

            if (equipment == null)
            {
                throw new InvalidOperationException($"设备不存在: {request.EquipmentId}");
            }

            var previousStatus = equipment.CurrentStatus;

            // 结束当前状态记录
            await _db.Updateable<EquipmentStatusRecord>()
                .SetColumns(r => new EquipmentStatusRecord 
                { 
                    EndTime = DateTime.Now,
                    DurationMinutes = (int)(DateTime.Now - r.StartTime).TotalMinutes
                })
                .Where(r => r.EquipmentId == request.EquipmentId && r.EndTime == null)
                .ExecuteCommandAsync();

            // 更新设备状态
            await _db.Updateable<Equipment>()
                .SetColumns(e => new Equipment 
                { 
                    CurrentStatus = request.NewStatus,
                    UpdatedTime = DateTime.Now
                })
                .Where(e => e.Id == request.EquipmentId)
                .ExecuteCommandAsync();

            // 创建新的状态记录
            var statusRecord = new EquipmentStatusRecord
            {
                EquipmentId = request.EquipmentId,
                EquipmentCode = request.EquipmentCode,
                PreviousStatus = previousStatus,
                CurrentStatus = request.NewStatus,
                StartTime = DateTime.Now,
                Reason = request.Reason,
                OperatorId = request.OperatorId,
                OperatorName = request.OperatorName,
                WorkOrderNumber = request.WorkOrderNumber,
                Remarks = request.Remarks
            };

            await _db.Insertable(statusRecord).ExecuteCommandAsync();

            _db.Ado.CommitTran();

            _logger.LogInformation("设备状态更新成功: {EquipmentCode} {PreviousStatus} -> {NewStatus}", 
                request.EquipmentCode, previousStatus, request.NewStatus);

            return true;
        }
        catch (Exception ex)
        {
            _db.Ado.RollbackTran();
            _logger.LogError(ex, "更新设备状态失败: {EquipmentCode}", request.EquipmentCode);
            throw;
        }
        finally
        {
            _statusUpdateSemaphore.Release();
        }
    }

    /// <summary>
    /// 获取设备当前状态
    /// </summary>
    public async Task<EquipmentStatus> GetEquipmentCurrentStatusAsync(long equipmentId)
    {
        try
        {
            var equipment = await _db.Queryable<Equipment>()
                .Where(e => e.Id == equipmentId)
                .Select(e => e.CurrentStatus)
                .FirstAsync();

            return equipment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备当前状态失败: {EquipmentId}", equipmentId);
            throw;
        }
    }

    /// <summary>
    /// 获取设备状态历史
    /// </summary>
    public async Task<List<EquipmentStatusRecord>> GetEquipmentStatusHistoryAsync(long equipmentId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _db.Queryable<EquipmentStatusRecord>()
                .Where(r => r.EquipmentId == equipmentId);

            if (startDate.HasValue)
            {
                query = query.Where(r => r.StartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(r => r.StartTime <= endDate.Value);
            }

            return await query
                .OrderByDescending(r => r.StartTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备状态历史失败: {EquipmentId}", equipmentId);
            throw;
        }
    }

    /// <summary>
    /// 获取设备状态统计
    /// </summary>
    public async Task<EquipmentStatusStatistics> GetEquipmentStatusStatisticsAsync(long? workstationId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _db.Queryable<Equipment>()
                .Where(e => !e.IsDeleted);

            if (workstationId.HasValue)
            {
                query = query.Where(e => e.WorkstationId == workstationId.Value);
            }

            var equipments = await query.ToListAsync();

            var statistics = new EquipmentStatusStatistics
            {
                TotalEquipments = equipments.Count,
                RunningEquipments = equipments.Count(e => e.CurrentStatus == EquipmentStatus.Running),
                IdleEquipments = equipments.Count(e => e.CurrentStatus == EquipmentStatus.Idle),
                FaultEquipments = equipments.Count(e => e.CurrentStatus == EquipmentStatus.Fault),
                MaintenanceEquipments = equipments.Count(e => e.CurrentStatus == EquipmentStatus.UnderMaintenance),
                OfflineEquipments = equipments.Count(e => e.CurrentStatus == EquipmentStatus.Offline)
            };

            // 计算利用率
            statistics.OverallUtilizationRate = statistics.TotalEquipments > 0 ? 
                (double)statistics.RunningEquipments / statistics.TotalEquipments * 100 : 0;

            // 状态分布
            statistics.StatusDistributions = equipments
                .GroupBy(e => e.CurrentStatus)
                .Select(g => new StatusDistribution
                {
                    Status = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / statistics.TotalEquipments * 100
                })
                .ToList();

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备状态统计失败");
            throw;
        }
    }

    /// <summary>
    /// 获取设备运行时间统计
    /// </summary>
    public async Task<EquipmentRuntimeStatistics> GetEquipmentRuntimeStatisticsAsync(long equipmentId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var equipment = await GetEquipmentByIdAsync(equipmentId);
            if (equipment == null)
            {
                throw new InvalidOperationException($"设备不存在: {equipmentId}");
            }

            var statusRecords = await _db.Queryable<EquipmentStatusRecord>()
                .Where(r => r.EquipmentId == equipmentId && 
                           r.StartTime >= startDate && 
                           r.StartTime <= endDate)
                .OrderBy(r => r.StartTime)
                .ToListAsync();

            var statistics = new EquipmentRuntimeStatistics
            {
                EquipmentId = equipmentId,
                EquipmentCode = equipment.EquipmentCode,
                EquipmentName = equipment.EquipmentName
            };

            var runtimeSegments = new List<RuntimeSegment>();
            TimeSpan totalRuntime = TimeSpan.Zero;
            TimeSpan idleTime = TimeSpan.Zero;
            TimeSpan faultTime = TimeSpan.Zero;
            TimeSpan maintenanceTime = TimeSpan.Zero;

            foreach (var record in statusRecords)
            {
                var duration = record.DurationMinutes.HasValue ? 
                    TimeSpan.FromMinutes(record.DurationMinutes.Value) : 
                    (record.EndTime ?? DateTime.Now) - record.StartTime;

                var segment = new RuntimeSegment
                {
                    Status = record.CurrentStatus,
                    StartTime = record.StartTime,
                    EndTime = record.EndTime,
                    Duration = duration
                };

                runtimeSegments.Add(segment);

                switch (record.CurrentStatus)
                {
                    case EquipmentStatus.Running:
                        totalRuntime = totalRuntime.Add(duration);
                        break;
                    case EquipmentStatus.Idle:
                    case EquipmentStatus.Waiting:
                        idleTime = idleTime.Add(duration);
                        break;
                    case EquipmentStatus.Fault:
                    case EquipmentStatus.Alarm:
                        faultTime = faultTime.Add(duration);
                        break;
                    case EquipmentStatus.UnderMaintenance:
                        maintenanceTime = maintenanceTime.Add(duration);
                        break;
                }
            }

            var totalTime = endDate - startDate;
            statistics.TotalRuntime = totalRuntime;
            statistics.IdleTime = idleTime;
            statistics.FaultTime = faultTime;
            statistics.MaintenanceTime = maintenanceTime;
            statistics.UtilizationRate = totalTime.TotalMinutes > 0 ? totalRuntime.TotalMinutes / totalTime.TotalMinutes * 100 : 0;
            statistics.AvailabilityRate = totalTime.TotalMinutes > 0 ? 
                (totalTime.TotalMinutes - faultTime.TotalMinutes - maintenanceTime.TotalMinutes) / totalTime.TotalMinutes * 100 : 0;
            statistics.RuntimeSegments = runtimeSegments;

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备运行时间统计失败: {EquipmentId}", equipmentId);
            throw;
        }
    }

    #endregion

    #region 设备维护管理

    /// <summary>
    /// 创建维护计划
    /// </summary>
    public async Task<long> CreateMaintenancePlanAsync(EquipmentMaintenanceRecord maintenanceRecord)
    {
        try
        {
            // 生成维护单号
            maintenanceRecord.MaintenanceNumber = await GenerateMaintenanceNumberAsync();

            var id = await _db.Insertable(maintenanceRecord).ExecuteReturnSnowflakeIdAsync();

            _logger.LogInformation("维护计划创建成功: {MaintenanceNumber}", maintenanceRecord.MaintenanceNumber);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建维护计划失败: {EquipmentId}", maintenanceRecord.EquipmentId);
            throw;
        }
    }

    /// <summary>
    /// 开始维护
    /// </summary>
    public async Task<bool> StartMaintenanceAsync(long maintenanceId, MaintenanceStartRequest request)
    {
        try
        {
            _db.Ado.BeginTran();

            var maintenance = await _db.Queryable<EquipmentMaintenanceRecord>()
                .Where(m => m.Id == maintenanceId)
                .FirstAsync();

            if (maintenance == null)
            {
                throw new InvalidOperationException($"维护记录不存在: {maintenanceId}");
            }

            if (maintenance.MaintenanceStatus != MaintenanceStatus.Planned)
            {
                throw new InvalidOperationException($"维护状态不正确，无法开始维护: {maintenance.MaintenanceStatus}");
            }

            // 更新维护记录
            await _db.Updateable<EquipmentMaintenanceRecord>()
                .SetColumns(m => new EquipmentMaintenanceRecord
                {
                    MaintenanceStatus = MaintenanceStatus.InProgress,
                    ActualStartTime = DateTime.Now,
                    MaintenancePersonId = request.MaintenancePersonId,
                    MaintenancePersonName = request.MaintenancePersonName,
                    ExternalCompany = request.ExternalCompany,
                    UpdatedTime = DateTime.Now
                })
                .Where(m => m.Id == maintenanceId)
                .ExecuteCommandAsync();

            // 更新设备状态为维护中
            var statusUpdateRequest = new EquipmentStatusUpdateRequest
            {
                EquipmentId = maintenance.EquipmentId,
                EquipmentCode = maintenance.EquipmentCode,
                NewStatus = EquipmentStatus.UnderMaintenance,
                Reason = $"开始维护: {maintenance.MaintenanceNumber}",
                OperatorName = request.MaintenancePersonName,
                Remarks = request.Remarks
            };

            await UpdateEquipmentStatusAsync(statusUpdateRequest);

            _db.Ado.CommitTran();

            _logger.LogInformation("维护开始成功: {MaintenanceNumber}", maintenance.MaintenanceNumber);
            return true;
        }
        catch (Exception ex)
        {
            _db.Ado.RollbackTran();
            _logger.LogError(ex, "开始维护失败: {MaintenanceId}", maintenanceId);
            throw;
        }
    }

    /// <summary>
    /// 完成维护
    /// </summary>
    public async Task<bool> CompleteMaintenanceAsync(long maintenanceId, MaintenanceCompleteRequest request)
    {
        try
        {
            _db.Ado.BeginTran();

            var maintenance = await _db.Queryable<EquipmentMaintenanceRecord>()
                .Where(m => m.Id == maintenanceId)
                .FirstAsync();

            if (maintenance == null)
            {
                throw new InvalidOperationException($"维护记录不存在: {maintenanceId}");
            }

            if (maintenance.MaintenanceStatus != MaintenanceStatus.InProgress)
            {
                throw new InvalidOperationException($"维护状态不正确，无法完成维护: {maintenance.MaintenanceStatus}");
            }

            // 更新维护记录
            await _db.Updateable<EquipmentMaintenanceRecord>()
                .SetColumns(m => new EquipmentMaintenanceRecord
                {
                    MaintenanceStatus = MaintenanceStatus.Completed,
                    ActualEndTime = DateTime.Now,
                    MaintenanceResult = request.MaintenanceResult,
                    SpareParts = request.SpareParts,
                    MaintenanceCost = request.MaintenanceCost,
                    NextMaintenanceTime = request.NextMaintenanceTime,
                    UpdatedTime = DateTime.Now
                })
                .Where(m => m.Id == maintenanceId)
                .ExecuteCommandAsync();

            // 更新设备状态为空闲
            var statusUpdateRequest = new EquipmentStatusUpdateRequest
            {
                EquipmentId = maintenance.EquipmentId,
                EquipmentCode = maintenance.EquipmentCode,
                NewStatus = EquipmentStatus.Idle,
                Reason = $"完成维护: {maintenance.MaintenanceNumber}",
                OperatorName = maintenance.MaintenancePersonName,
                Remarks = request.Remarks
            };

            await UpdateEquipmentStatusAsync(statusUpdateRequest);

            _db.Ado.CommitTran();

            _logger.LogInformation("维护完成成功: {MaintenanceNumber}", maintenance.MaintenanceNumber);
            return true;
        }
        catch (Exception ex)
        {
            _db.Ado.RollbackTran();
            _logger.LogError(ex, "完成维护失败: {MaintenanceId}", maintenanceId);
            throw;
        }
    }

    /// <summary>
    /// 获取维护记录列表
    /// </summary>
    public async Task<List<EquipmentMaintenanceRecord>> GetMaintenanceRecordsAsync(long? equipmentId = null, MaintenanceStatus? status = null)
    {
        try
        {
            var query = _db.Queryable<EquipmentMaintenanceRecord>()
                .Where(m => !m.IsDeleted);

            if (equipmentId.HasValue)
            {
                query = query.Where(m => m.EquipmentId == equipmentId.Value);
            }

            if (status.HasValue)
            {
                query = query.Where(m => m.MaintenanceStatus == status.Value);
            }

            return await query
                .OrderByDescending(m => m.PlannedStartTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取维护记录列表失败");
            throw;
        }
    }

    /// <summary>
    /// 分页查询维护记录
    /// </summary>
    public async Task<(List<EquipmentMaintenanceRecord> Items, int Total)> GetMaintenanceRecordsPagedAsync(MaintenanceQueryRequest request)
    {
        try
        {
            var query = _db.Queryable<EquipmentMaintenanceRecord>()
                .Where(m => !m.IsDeleted);

            // 添加过滤条件
            if (request.EquipmentId.HasValue)
            {
                query = query.Where(m => m.EquipmentId == request.EquipmentId.Value);
            }

            if (!string.IsNullOrEmpty(request.EquipmentCode))
            {
                query = query.Where(m => m.EquipmentCode.Contains(request.EquipmentCode));
            }

            if (request.MaintenanceType.HasValue)
            {
                query = query.Where(m => m.MaintenanceType == request.MaintenanceType.Value);
            }

            if (request.MaintenanceStatus.HasValue)
            {
                query = query.Where(m => m.MaintenanceStatus == request.MaintenanceStatus.Value);
            }

            if (request.PlannedStartTimeFrom.HasValue)
            {
                query = query.Where(m => m.PlannedStartTime >= request.PlannedStartTimeFrom.Value);
            }

            if (request.PlannedStartTimeTo.HasValue)
            {
                query = query.Where(m => m.PlannedStartTime <= request.PlannedStartTimeTo.Value);
            }

            // 获取总数
            var total = await query.CountAsync();

            // 分页查询
            var items = await query
                .OrderByDescending(m => m.PlannedStartTime)
                .ToPageListAsync(request.PageIndex, request.PageSize);

            return (items, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询维护记录失败");
            throw;
        }
    }

    /// <summary>
    /// 获取维护统计
    /// </summary>
    public async Task<MaintenanceStatistics> GetMaintenanceStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _db.Queryable<EquipmentMaintenanceRecord>()
                .Where(m => !m.IsDeleted);

            if (startDate.HasValue)
            {
                query = query.Where(m => m.PlannedStartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(m => m.PlannedStartTime <= endDate.Value);
            }

            var maintenanceRecords = await query.ToListAsync();

            var statistics = new MaintenanceStatistics
            {
                TotalMaintenanceRecords = maintenanceRecords.Count,
                PlannedMaintenances = maintenanceRecords.Count(m => m.MaintenanceStatus == MaintenanceStatus.Planned),
                CompletedMaintenances = maintenanceRecords.Count(m => m.MaintenanceStatus == MaintenanceStatus.Completed),
                OverdueMaintenances = maintenanceRecords.Count(m => m.MaintenanceStatus == MaintenanceStatus.Planned && m.PlannedStartTime < DateTime.Now),
                EmergencyMaintenances = maintenanceRecords.Count(m => m.MaintenanceType == MaintenanceType.Emergency),
                TotalMaintenanceCost = maintenanceRecords.Where(m => m.MaintenanceCost.HasValue).Sum(m => m.MaintenanceCost.Value)
            };

            // 计算平均维护时间
            var completedMaintenances = maintenanceRecords
                .Where(m => m.MaintenanceStatus == MaintenanceStatus.Completed && 
                           m.ActualStartTime.HasValue && m.ActualEndTime.HasValue)
                .ToList();

            if (completedMaintenances.Any())
            {
                statistics.AverageMaintenanceTime = completedMaintenances
                    .Average(m => (m.ActualEndTime.Value - m.ActualStartTime.Value).TotalHours);
            }

            // 类型统计
            statistics.TypeStatistics = maintenanceRecords
                .GroupBy(m => m.MaintenanceType)
                .Select(g => new MaintenanceTypeStatistics
                {
                    MaintenanceType = g.Key,
                    Count = g.Count(),
                    TotalCost = g.Where(m => m.MaintenanceCost.HasValue).Sum(m => m.MaintenanceCost.Value),
                    AverageTime = g.Where(m => m.ActualStartTime.HasValue && m.ActualEndTime.HasValue)
                        .DefaultIfEmpty()
                        .Average(m => m?.ActualStartTime.HasValue == true && m?.ActualEndTime.HasValue == true ? 
                            (m.ActualEndTime.Value - m.ActualStartTime.Value).TotalHours : 0)
                })
                .ToList();

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取维护统计失败");
            throw;
        }
    }

    /// <summary>
    /// 获取设备维护提醒
    /// </summary>
    public async Task<List<MaintenanceReminder>> GetMaintenanceRemindersAsync()
    {
        try
        {
            var reminders = new List<MaintenanceReminder>();

            // 获取计划中的维护记录
            var plannedMaintenances = await _db.Queryable<EquipmentMaintenanceRecord, Equipment>((m, e) => new JoinQueryInfos(
                JoinType.Inner, m.EquipmentId == e.Id))
                .Where((m, e) => m.MaintenanceStatus == MaintenanceStatus.Planned && !m.IsDeleted && !e.IsDeleted)
                .Select((m, e) => new
                {
                    m.EquipmentId,
                    e.EquipmentCode,
                    e.EquipmentName,
                    m.MaintenanceType,
                    m.PlannedStartTime,
                    e.IsCritical
                })
                .ToListAsync();

            foreach (var maintenance in plannedMaintenances)
            {
                var daysOverdue = (DateTime.Now.Date - maintenance.PlannedStartTime.Date).Days;
                var reminder = new MaintenanceReminder
                {
                    EquipmentId = maintenance.EquipmentId,
                    EquipmentCode = maintenance.EquipmentCode,
                    EquipmentName = maintenance.EquipmentName,
                    MaintenanceType = maintenance.MaintenanceType,
                    PlannedMaintenanceTime = maintenance.PlannedStartTime,
                    DaysOverdue = daysOverdue,
                    IsOverdue = daysOverdue > 0,
                    IsCritical = maintenance.IsCritical
                };

                reminders.Add(reminder);
            }

            return reminders.OrderByDescending(r => r.DaysOverdue).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备维护提醒失败");
            throw;
        }
    }

    #endregion

    #region 设备报警管理

    /// <summary>
    /// 创建设备报警
    /// </summary>
    public async Task<long> CreateAlarmAsync(EquipmentAlarm alarm)
    {
        try
        {
            // 生成报警编号
            alarm.AlarmNumber = await GenerateAlarmNumberAsync();

            var id = await _db.Insertable(alarm).ExecuteReturnSnowflakeIdAsync();

            // 如果是严重级别的报警，自动更新设备状态
            if (alarm.AlarmLevel == AlarmLevel.Critical || alarm.AlarmLevel == AlarmLevel.Emergency)
            {
                var statusUpdateRequest = new EquipmentStatusUpdateRequest
                {
                    EquipmentId = alarm.EquipmentId,
                    EquipmentCode = alarm.EquipmentCode,
                    NewStatus = EquipmentStatus.Alarm,
                    Reason = $"严重报警: {alarm.AlarmDescription}",
                    Remarks = $"报警编号: {alarm.AlarmNumber}"
                };

                await UpdateEquipmentStatusAsync(statusUpdateRequest);
            }

            _logger.LogInformation("设备报警创建成功: {AlarmNumber}", alarm.AlarmNumber);
            return id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建设备报警失败: {EquipmentId}", alarm.EquipmentId);
            throw;
        }
    }

    /// <summary>
    /// 确认报警
    /// </summary>
    public async Task<bool> AcknowledgeAlarmAsync(long alarmId, AlarmAcknowledgeRequest request)
    {
        try
        {
            var result = await _db.Updateable<EquipmentAlarm>()
                .SetColumns(a => new EquipmentAlarm
                {
                    AlarmStatus = AlarmStatus.Acknowledged,
                    AcknowledgedById = request.AcknowledgedById,
                    AcknowledgedByName = request.AcknowledgedByName,
                    AcknowledgedTime = DateTime.Now,
                    UpdatedTime = DateTime.Now
                })
                .Where(a => a.Id == alarmId && a.AlarmStatus == AlarmStatus.Active)
                .ExecuteCommandAsync();

            _logger.LogInformation("报警确认成功: {AlarmId} by {AcknowledgedByName}", alarmId, request.AcknowledgedByName);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确认报警失败: {AlarmId}", alarmId);
            throw;
        }
    }

    /// <summary>
    /// 解决报警
    /// </summary>
    public async Task<bool> ResolveAlarmAsync(long alarmId, AlarmResolveRequest request)
    {
        try
        {
            var result = await _db.Updateable<EquipmentAlarm>()
                .SetColumns(a => new EquipmentAlarm
                {
                    AlarmStatus = AlarmStatus.Resolved,
                    AlarmEndTime = DateTime.Now,
                    DurationMinutes = (int)(DateTime.Now - a.AlarmStartTime).TotalMinutes,
                    HandlingActions = request.HandlingActions,
                    RootCause = request.RootCause,
                    PreventiveMeasures = request.PreventiveMeasures,
                    UpdatedTime = DateTime.Now
                })
                .Where(a => a.Id == alarmId)
                .ExecuteCommandAsync();

            _logger.LogInformation("报警解决成功: {AlarmId} by {ResolvedByName}", alarmId, request.ResolvedByName);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解决报警失败: {AlarmId}", alarmId);
            throw;
        }
    }

    /// <summary>
    /// 关闭报警
    /// </summary>
    public async Task<bool> CloseAlarmAsync(long alarmId, string closedByName)
    {
        try
        {
            var result = await _db.Updateable<EquipmentAlarm>()
                .SetColumns(a => new EquipmentAlarm
                {
                    AlarmStatus = AlarmStatus.Closed,
                    UpdatedTime = DateTime.Now
                })
                .Where(a => a.Id == alarmId)
                .ExecuteCommandAsync();

            _logger.LogInformation("报警关闭成功: {AlarmId} by {ClosedByName}", alarmId, closedByName);
            return result > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "关闭报警失败: {AlarmId}", alarmId);
            throw;
        }
    }

    /// <summary>
    /// 获取活动报警列表
    /// </summary>
    public async Task<List<EquipmentAlarm>> GetActiveAlarmsAsync()
    {
        try
        {
            return await _db.Queryable<EquipmentAlarm>()
                .Where(a => a.AlarmStatus == AlarmStatus.Active || a.AlarmStatus == AlarmStatus.Acknowledged)
                .OrderByDescending(a => a.AlarmLevel)
                .OrderByDescending(a => a.AlarmStartTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取活动报警列表失败");
            throw;
        }
    }

    /// <summary>
    /// 分页查询报警记录
    /// </summary>
    public async Task<(List<EquipmentAlarm> Items, int Total)> GetAlarmsPagedAsync(AlarmQueryRequest request)
    {
        try
        {
            var query = _db.Queryable<EquipmentAlarm>()
                .Where(a => !a.IsDeleted);

            // 添加过滤条件
            if (request.EquipmentId.HasValue)
            {
                query = query.Where(a => a.EquipmentId == request.EquipmentId.Value);
            }

            if (!string.IsNullOrEmpty(request.EquipmentCode))
            {
                query = query.Where(a => a.EquipmentCode.Contains(request.EquipmentCode));
            }

            if (request.AlarmType.HasValue)
            {
                query = query.Where(a => a.AlarmType == request.AlarmType.Value);
            }

            if (request.AlarmLevel.HasValue)
            {
                query = query.Where(a => a.AlarmLevel == request.AlarmLevel.Value);
            }

            if (request.AlarmStatus.HasValue)
            {
                query = query.Where(a => a.AlarmStatus == request.AlarmStatus.Value);
            }

            if (request.AlarmStartTimeFrom.HasValue)
            {
                query = query.Where(a => a.AlarmStartTime >= request.AlarmStartTimeFrom.Value);
            }

            if (request.AlarmStartTimeTo.HasValue)
            {
                query = query.Where(a => a.AlarmStartTime <= request.AlarmStartTimeTo.Value);
            }

            // 获取总数
            var total = await query.CountAsync();

            // 分页查询
            var items = await query
                .OrderByDescending(a => a.AlarmStartTime)
                .ToPageListAsync(request.PageIndex, request.PageSize);

            return (items, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询报警记录失败");
            throw;
        }
    }

    /// <summary>
    /// 获取报警统计
    /// </summary>
    public async Task<AlarmStatistics> GetAlarmStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _db.Queryable<EquipmentAlarm>()
                .Where(a => !a.IsDeleted);

            if (startDate.HasValue)
            {
                query = query.Where(a => a.AlarmStartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.AlarmStartTime <= endDate.Value);
            }

            var alarms = await query.ToListAsync();

            var statistics = new AlarmStatistics
            {
                TotalAlarms = alarms.Count,
                ActiveAlarms = alarms.Count(a => a.AlarmStatus == AlarmStatus.Active),
                AcknowledgedAlarms = alarms.Count(a => a.AlarmStatus == AlarmStatus.Acknowledged),
                ResolvedAlarms = alarms.Count(a => a.AlarmStatus == AlarmStatus.Resolved),
                CriticalAlarms = alarms.Count(a => a.AlarmLevel == AlarmLevel.Critical || a.AlarmLevel == AlarmLevel.Emergency)
            };

            // 计算平均响应时间和解决时间
            var acknowledgedAlarms = alarms.Where(a => a.AcknowledgedTime.HasValue).ToList();
            if (acknowledgedAlarms.Any())
            {
                statistics.AverageResponseTime = acknowledgedAlarms
                    .Average(a => (a.AcknowledgedTime.Value - a.AlarmStartTime).TotalMinutes);
            }

            var resolvedAlarms = alarms.Where(a => a.AlarmEndTime.HasValue).ToList();
            if (resolvedAlarms.Any())
            {
                statistics.AverageResolutionTime = resolvedAlarms
                    .Average(a => (a.AlarmEndTime.Value - a.AlarmStartTime).TotalMinutes);
            }

            // 类型统计
            statistics.TypeStatistics = alarms
                .GroupBy(a => a.AlarmType)
                .Select(g => new AlarmTypeStatistics
                {
                    AlarmType = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / statistics.TotalAlarms * 100
                })
                .ToList();

            // 级别统计
            statistics.LevelStatistics = alarms
                .GroupBy(a => a.AlarmLevel)
                .Select(g => new AlarmLevelStatistics
                {
                    AlarmLevel = g.Key,
                    Count = g.Count(),
                    Percentage = (double)g.Count() / statistics.TotalAlarms * 100
                })
                .ToList();

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取报警统计失败");
            throw;
        }
    }

    /// <summary>
    /// 获取设备报警历史
    /// </summary>
    public async Task<List<EquipmentAlarm>> GetEquipmentAlarmHistoryAsync(long equipmentId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _db.Queryable<EquipmentAlarm>()
                .Where(a => a.EquipmentId == equipmentId && !a.IsDeleted);

            if (startDate.HasValue)
            {
                query = query.Where(a => a.AlarmStartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(a => a.AlarmStartTime <= endDate.Value);
            }

            return await query
                .OrderByDescending(a => a.AlarmStartTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备报警历史失败: {EquipmentId}", equipmentId);
            throw;
        }
    }

    #endregion

    #region 设备操作日志

    /// <summary>
    /// 记录设备操作
    /// </summary>
    public async Task<bool> LogEquipmentOperationAsync(EquipmentOperationLog operationLog)
    {
        try
        {
            await _db.Insertable(operationLog).ExecuteCommandAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "记录设备操作失败: {EquipmentId}", operationLog.EquipmentId);
            throw;
        }
    }

    /// <summary>
    /// 获取设备操作日志
    /// </summary>
    public async Task<List<EquipmentOperationLog>> GetEquipmentOperationLogsAsync(long equipmentId, DateTime? startDate = null, DateTime? endDate = null)
    {
        try
        {
            var query = _db.Queryable<EquipmentOperationLog>()
                .Where(l => l.EquipmentId == equipmentId);

            if (startDate.HasValue)
            {
                query = query.Where(l => l.OperationTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(l => l.OperationTime <= endDate.Value);
            }

            return await query
                .OrderByDescending(l => l.OperationTime)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备操作日志失败: {EquipmentId}", equipmentId);
            throw;
        }
    }

    /// <summary>
    /// 分页查询操作日志
    /// </summary>
    public async Task<(List<EquipmentOperationLog> Items, int Total)> GetOperationLogsPagedAsync(OperationLogQueryRequest request)
    {
        try
        {
            var query = _db.Queryable<EquipmentOperationLog>();

            // 添加过滤条件
            if (request.EquipmentId.HasValue)
            {
                query = query.Where(l => l.EquipmentId == request.EquipmentId.Value);
            }

            if (!string.IsNullOrEmpty(request.EquipmentCode))
            {
                query = query.Where(l => l.EquipmentCode.Contains(request.EquipmentCode));
            }

            if (request.OperationType.HasValue)
            {
                query = query.Where(l => l.OperationType == request.OperationType.Value);
            }

            if (request.OperationTimeFrom.HasValue)
            {
                query = query.Where(l => l.OperationTime >= request.OperationTimeFrom.Value);
            }

            if (request.OperationTimeTo.HasValue)
            {
                query = query.Where(l => l.OperationTime <= request.OperationTimeTo.Value);
            }

            if (request.OperatorId.HasValue)
            {
                query = query.Where(l => l.OperatorId == request.OperatorId.Value);
            }

            if (!string.IsNullOrEmpty(request.RelatedBatchNumber))
            {
                query = query.Where(l => l.RelatedBatchNumber == request.RelatedBatchNumber);
            }

            // 获取总数
            var total = await query.CountAsync();

            // 分页查询
            var items = await query
                .OrderByDescending(l => l.OperationTime)
                .ToPageListAsync(request.PageIndex, request.PageSize);

            return (items, total);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "分页查询操作日志失败");
            throw;
        }
    }

    #endregion

    #region 设备监控面板

    /// <summary>
    /// 获取设备监控面板数据
    /// </summary>
    public async Task<EquipmentMonitoringDashboard> GetMonitoringDashboardAsync()
    {
        try
        {
            var dashboard = new EquipmentMonitoringDashboard();

            // 获取设备状态统计
            dashboard.StatusStatistics = await GetEquipmentStatusStatisticsAsync();

            // 获取报警统计
            dashboard.AlarmStatistics = await GetAlarmStatisticsAsync(DateTime.Today.AddDays(-7), DateTime.Now);

            // 获取维护统计
            dashboard.MaintenanceStatistics = await GetMaintenanceStatisticsAsync(DateTime.Today.AddDays(-30), DateTime.Now);

            // 获取关键设备
            dashboard.CriticalEquipments = await _db.Queryable<Equipment>()
                .Where(e => e.IsCritical && !e.IsDeleted)
                .OrderBy(e => e.EquipmentCode)
                .ToListAsync();

            // 获取最近报警
            dashboard.RecentAlarms = await _db.Queryable<EquipmentAlarm>()
                .Where(a => a.AlarmStartTime >= DateTime.Today.AddDays(-1))
                .OrderByDescending(a => a.AlarmStartTime)
                .Take(10)
                .ToListAsync();

            // 获取维护提醒
            dashboard.MaintenanceReminders = await GetMaintenanceRemindersAsync();

            // 计算总体OEE（简化计算）
            var totalEquipments = dashboard.StatusStatistics.TotalEquipments;
            var runningEquipments = dashboard.StatusStatistics.RunningEquipments;
            dashboard.OverallOee = totalEquipments > 0 ? (double)runningEquipments / totalEquipments * 100 : 0;

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备监控面板数据失败");
            throw;
        }
    }

    /// <summary>
    /// 获取设备OEE统计
    /// </summary>
    public async Task<EquipmentOeeStatistics> GetEquipmentOeeStatisticsAsync(long equipmentId, DateTime startDate, DateTime endDate)
    {
        try
        {
            var equipment = await GetEquipmentByIdAsync(equipmentId);
            if (equipment == null)
            {
                throw new InvalidOperationException($"设备不存在: {equipmentId}");
            }

            var runtimeStats = await GetEquipmentRuntimeStatisticsAsync(equipmentId, startDate, endDate);

            var oeeStats = new EquipmentOeeStatistics
            {
                EquipmentId = equipmentId,
                EquipmentCode = equipment.EquipmentCode,
                EquipmentName = equipment.EquipmentName,
                CalculationPeriodStart = startDate,
                CalculationPeriodEnd = endDate,
                PlannedProductionTime = endDate - startDate,
                ActualProductionTime = runtimeStats.TotalRuntime,
                DownTime = runtimeStats.FaultTime.Add(runtimeStats.MaintenanceTime)
            };

            // 计算可用率
            var totalTime = oeeStats.PlannedProductionTime;
            oeeStats.Availability = totalTime.TotalMinutes > 0 ? 
                (totalTime.TotalMinutes - oeeStats.DownTime.TotalMinutes) / totalTime.TotalMinutes * 100 : 0;

            // 简化的性能率计算（假设理想运行时间等于实际运行时间）
            oeeStats.Performance = oeeStats.Availability > 0 ? 95.0 : 0; // 假设值

            // 简化的质量率计算（需要根据实际生产数据计算）
            oeeStats.Quality = 98.0; // 假设值

            // 计算OEE
            oeeStats.Oee = oeeStats.Availability * oeeStats.Performance * oeeStats.Quality / 10000;

            return oeeStats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备OEE统计失败: {EquipmentId}", equipmentId);
            throw;
        }
    }

    /// <summary>
    /// 获取设备利用率分析
    /// </summary>
    public async Task<List<EquipmentUtilizationAnalysis>> GetEquipmentUtilizationAnalysisAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var equipments = await GetEquipmentsAsync();
            var analysisList = new List<EquipmentUtilizationAnalysis>();

            foreach (var equipment in equipments)
            {
                try
                {
                    var runtimeStats = await GetEquipmentRuntimeStatisticsAsync(equipment.Id, startDate, endDate);
                    var alarms = await GetEquipmentAlarmHistoryAsync(equipment.Id, startDate, endDate);

                    var faultAlarms = alarms.Where(a => a.AlarmType == AlarmType.EquipmentFault).ToList();

                    var analysis = new EquipmentUtilizationAnalysis
                    {
                        EquipmentId = equipment.Id,
                        EquipmentCode = equipment.EquipmentCode,
                        EquipmentName = equipment.EquipmentName,
                        EquipmentType = equipment.EquipmentType,
                        UtilizationRate = runtimeStats.UtilizationRate,
                        TotalRuntime = runtimeStats.TotalRuntime,
                        IdleTime = runtimeStats.IdleTime,
                        FaultTime = runtimeStats.FaultTime,
                        FaultCount = faultAlarms.Count
                    };

                    // 计算MTBF和MTTR
                    if (analysis.FaultCount > 0)
                    {
                        analysis.MeanTimeBetweenFailures = analysis.TotalRuntime.TotalHours / analysis.FaultCount;
                        
                        var avgFaultDuration = faultAlarms
                            .Where(a => a.DurationMinutes.HasValue)
                            .Average(a => a.DurationMinutes.Value);
                        analysis.MeanTimeToRepair = avgFaultDuration / 60.0; // 转换为小时
                    }

                    analysisList.Add(analysis);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取设备利用率分析失败: {EquipmentCode}", equipment.EquipmentCode);
                }
            }

            return analysisList.OrderByDescending(a => a.UtilizationRate).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备利用率分析失败");
            throw;
        }
    }

    /// <summary>
    /// 获取设备故障分析
    /// </summary>
    public async Task<List<EquipmentFaultAnalysis>> GetEquipmentFaultAnalysisAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            var equipments = await GetEquipmentsAsync();
            var analysisList = new List<EquipmentFaultAnalysis>();

            foreach (var equipment in equipments)
            {
                try
                {
                    var alarms = await GetEquipmentAlarmHistoryAsync(equipment.Id, startDate, endDate);
                    var faultAlarms = alarms.Where(a => a.AlarmType == AlarmType.EquipmentFault || 
                                                        a.AlarmLevel == AlarmLevel.Critical ||
                                                        a.AlarmLevel == AlarmLevel.Emergency).ToList();

                    if (faultAlarms.Any())
                    {
                        var analysis = new EquipmentFaultAnalysis
                        {
                            EquipmentId = equipment.Id,
                            EquipmentCode = equipment.EquipmentCode,
                            EquipmentName = equipment.EquipmentName,
                            TotalFaults = faultAlarms.Count,
                            TotalFaultTime = TimeSpan.FromMinutes(faultAlarms.Where(a => a.DurationMinutes.HasValue).Sum(a => a.DurationMinutes.Value)),
                            AverageFaultDuration = faultAlarms.Where(a => a.DurationMinutes.HasValue).DefaultIfEmpty().Average(a => a?.DurationMinutes ?? 0),
                            MostFrequentFaultType = faultAlarms.GroupBy(a => a.AlarmType).OrderByDescending(g => g.Count()).First().Key
                        };

                        // 故障频率统计
                        analysis.FaultFrequencies = faultAlarms
                            .GroupBy(a => a.AlarmType)
                            .Select(g => new FaultFrequencyStatistics
                            {
                                AlarmType = g.Key,
                                Count = g.Count(),
                                TotalDuration = TimeSpan.FromMinutes(g.Where(a => a.DurationMinutes.HasValue).Sum(a => a.DurationMinutes.Value)),
                                AverageDuration = g.Where(a => a.DurationMinutes.HasValue).DefaultIfEmpty().Average(a => a?.DurationMinutes ?? 0)
                            })
                            .ToList();

                        analysisList.Add(analysis);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "获取设备故障分析失败: {EquipmentCode}", equipment.EquipmentCode);
                }
            }

            return analysisList.OrderByDescending(a => a.TotalFaults).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备故障分析失败");
            throw;
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 生成维护单号
    /// </summary>
    private async Task<string> GenerateMaintenanceNumberAsync()
    {
        var prefix = "MT";
        var dateStr = DateTime.Now.ToString("yyyyMMdd");
        
        var count = await _db.Queryable<EquipmentMaintenanceRecord>()
            .Where(m => m.MaintenanceNumber.StartsWith($"{prefix}{dateStr}"))
            .CountAsync();
        
        var sequence = (count + 1).ToString("D3");
        return $"{prefix}{dateStr}{sequence}";
    }

    /// <summary>
    /// 生成报警编号
    /// </summary>
    private async Task<string> GenerateAlarmNumberAsync()
    {
        var prefix = "AL";
        var dateStr = DateTime.Now.ToString("yyyyMMdd");
        
        var count = await _db.Queryable<EquipmentAlarm>()
            .Where(a => a.AlarmNumber.StartsWith($"{prefix}{dateStr}"))
            .CountAsync();
        
        var sequence = (count + 1).ToString("D4");
        return $"{prefix}{dateStr}{sequence}";
    }

    #endregion
} 