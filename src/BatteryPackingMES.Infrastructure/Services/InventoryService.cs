using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Enums;
using BatteryPackingMES.Core.Interfaces;
using SqlSugar;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 库存管理服务实现
/// </summary>
public class InventoryService : IInventoryService
{
    private readonly ISqlSugarClient _db;
    private readonly ILogger<InventoryService> _logger;
    private static readonly SemaphoreSlim _transactionSemaphore = new(1, 1);

    public InventoryService(ISqlSugarClient db, ILogger<InventoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    #region 仓库管理

    public async Task<long> CreateWarehouseAsync(Warehouse warehouse)
    {
        try
        {
            // 检查编码唯一性
            var existingWarehouse = await _db.Queryable<Warehouse>()
                .Where(w => w.WarehouseCode == warehouse.WarehouseCode && !w.IsDeleted)
                .FirstAsync();

            if (existingWarehouse != null)
            {
                throw new InvalidOperationException($"仓库编码 {warehouse.WarehouseCode} 已存在");
            }

            return await _db.Insertable(warehouse).ExecuteReturnSnowflakeIdAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建仓库失败: {WarehouseCode}", warehouse.WarehouseCode);
            throw;
        }
    }

    public async Task<List<Warehouse>> GetWarehousesAsync()
    {
        return await _db.Queryable<Warehouse>()
            .Where(w => !w.IsDeleted)
            .Includes(w => w.Manager)
            .OrderBy(w => w.WarehouseCode)
            .ToListAsync();
    }

    public async Task<Warehouse?> GetWarehouseByIdAsync(long id)
    {
        return await _db.Queryable<Warehouse>()
            .Where(w => w.Id == id && !w.IsDeleted)
            .Includes(w => w.Manager)
            .Includes(w => w.Locations)
            .FirstAsync();
    }

    public async Task<Warehouse?> GetWarehouseByCodeAsync(string warehouseCode)
    {
        return await _db.Queryable<Warehouse>()
            .Where(w => w.WarehouseCode == warehouseCode && !w.IsDeleted)
            .Includes(w => w.Manager)
            .FirstAsync();
    }

    public async Task<bool> UpdateWarehouseAsync(Warehouse warehouse)
    {
        try
        {
            warehouse.UpdatedTime = DateTime.Now;
            return await _db.Updateable(warehouse).ExecuteCommandHasChangeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新仓库失败: {WarehouseId}", warehouse.Id);
            return false;
        }
    }

    public async Task<bool> DeleteWarehouseAsync(long id)
    {
        try
        {
            // 检查是否有库存记录
            var hasInventory = await _db.Queryable<InventoryRecord>()
                .Where(i => i.WarehouseId == id && !i.IsDeleted && i.CurrentQuantity > 0)
                .AnyAsync();

            if (hasInventory)
            {
                throw new InvalidOperationException("仓库有库存记录，无法删除");
            }

            return await _db.Updateable<Warehouse>()
                .SetColumns(w => new Warehouse { IsDeleted = true, UpdatedTime = DateTime.Now })
                .Where(w => w.Id == id)
                .ExecuteCommandHasChangeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除仓库失败: {WarehouseId}", id);
            return false;
        }
    }

    #endregion

    #region 库位管理

    public async Task<long> CreateLocationAsync(WarehouseLocation location)
    {
        try
        {
            // 检查编码唯一性
            var existingLocation = await _db.Queryable<WarehouseLocation>()
                .Where(l => l.LocationCode == location.LocationCode && !l.IsDeleted)
                .FirstAsync();

            if (existingLocation != null)
            {
                throw new InvalidOperationException($"库位编码 {location.LocationCode} 已存在");
            }

            return await _db.Insertable(location).ExecuteReturnSnowflakeIdAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建库位失败: {LocationCode}", location.LocationCode);
            throw;
        }
    }

    public async Task<List<WarehouseLocation>> GetLocationsByWarehouseAsync(long warehouseId)
    {
        return await _db.Queryable<WarehouseLocation>()
            .Where(l => l.WarehouseId == warehouseId && !l.IsDeleted)
            .OrderBy(l => l.LocationCode)
            .ToListAsync();
    }

    public async Task<WarehouseLocation?> GetLocationByIdAsync(long id)
    {
        return await _db.Queryable<WarehouseLocation>()
            .Where(l => l.Id == id && !l.IsDeleted)
            .Includes(l => l.Warehouse)
            .FirstAsync();
    }

    public async Task<WarehouseLocation?> GetLocationByCodeAsync(string locationCode)
    {
        return await _db.Queryable<WarehouseLocation>()
            .Where(l => l.LocationCode == locationCode && !l.IsDeleted)
            .Includes(l => l.Warehouse)
            .FirstAsync();
    }

    public async Task<bool> UpdateLocationAsync(WarehouseLocation location)
    {
        try
        {
            location.UpdatedTime = DateTime.Now;
            return await _db.Updateable(location).ExecuteCommandHasChangeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新库位失败: {LocationId}", location.Id);
            return false;
        }
    }

    public async Task<bool> DeleteLocationAsync(long id)
    {
        try
        {
            // 检查是否有库存记录
            var hasInventory = await _db.Queryable<InventoryRecord>()
                .Where(i => i.LocationId == id && !i.IsDeleted && i.CurrentQuantity > 0)
                .AnyAsync();

            if (hasInventory)
            {
                throw new InvalidOperationException("库位有库存记录，无法删除");
            }

            return await _db.Updateable<WarehouseLocation>()
                .SetColumns(l => new WarehouseLocation { IsDeleted = true, UpdatedTime = DateTime.Now })
                .Where(l => l.Id == id)
                .ExecuteCommandHasChangeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除库位失败: {LocationId}", id);
            return false;
        }
    }

    public async Task<List<WarehouseLocation>> GetAvailableLocationsAsync(long warehouseId)
    {
        return await _db.Queryable<WarehouseLocation>()
            .Where(l => l.WarehouseId == warehouseId && !l.IsDeleted && 
                       l.IsEnabled && l.LocationStatus == LocationStatus.Available)
            .OrderBy(l => l.LocationCode)
            .ToListAsync();
    }

    public async Task<bool> CheckLocationCapacityAsync(long locationId, decimal requiredCapacity)
    {
        var location = await GetLocationByIdAsync(locationId);
        if (location == null) return false;

        return (location.MaxCapacity - location.UsedCapacity) >= requiredCapacity;
    }

    #endregion

    #region 库存操作

    public async Task<bool> InboundAsync(InboundRequest request)
    {
        await _transactionSemaphore.WaitAsync();
        try
        {
            var result = await _db.Ado.UseTranAsync(async () =>
            {
                // 检查库位容量
                if (!await CheckLocationCapacityAsync(request.LocationId, request.Quantity))
                {
                    throw new InvalidOperationException("库位容量不足");
                }

                // 查找现有库存记录
                var existingRecord = await _db.Queryable<InventoryRecord>()
                    .Where(i => i.WarehouseId == request.WarehouseId &&
                               i.LocationId == request.LocationId &&
                               i.MaterialCode == request.MaterialCode &&
                               i.BatchNumber == request.BatchNumber &&
                               !i.IsDeleted)
                    .FirstAsync();

                var transactionNumber = await GenerateTransactionNumberAsync();

                if (existingRecord != null)
                {
                    // 更新现有记录
                    var quantityBefore = existingRecord.CurrentQuantity;
                    existingRecord.CurrentQuantity += request.Quantity;
                    existingRecord.AvailableQuantity += request.Quantity;
                    existingRecord.TotalValue = existingRecord.CurrentQuantity * (request.UnitPrice ?? 0);
                    existingRecord.UpdatedTime = DateTime.Now;

                    await _db.Updateable(existingRecord).ExecuteCommandAsync();

                    // 记录事务
                    await CreateInventoryTransactionAsync(new InventoryTransaction
                    {
                        TransactionNumber = transactionNumber,
                        TransactionType = TransactionType.Inbound,
                        WarehouseId = request.WarehouseId,
                        LocationId = request.LocationId,
                        MaterialCode = request.MaterialCode,
                        MaterialName = request.MaterialName,
                        BatchNumber = request.BatchNumber,
                        SerialNumber = request.SerialNumber,
                        QuantityBefore = quantityBefore,
                        QuantityChanged = request.Quantity,
                        QuantityAfter = existingRecord.CurrentQuantity,
                        Unit = request.Unit,
                        UnitPrice = request.UnitPrice,
                        ValueChanged = request.Quantity * (request.UnitPrice ?? 0),
                        RelatedDocumentNumber = request.RelatedDocumentNumber,
                        OperatorId = request.OperatorId,
                        OperatorName = request.OperatorName,
                        Reason = "入库",
                        Remarks = request.Remarks
                    });
                }
                else
                {
                    // 创建新库存记录
                    var inventoryRecord = new InventoryRecord
                    {
                        WarehouseId = request.WarehouseId,
                        LocationId = request.LocationId,
                        MaterialCode = request.MaterialCode,
                        MaterialName = request.MaterialName,
                        MaterialType = request.MaterialType,
                        BatchNumber = request.BatchNumber,
                        SerialNumber = request.SerialNumber,
                        SupplierCode = request.SupplierCode,
                        ProductionDate = request.ProductionDate,
                        ExpiryDate = request.ExpiryDate,
                        InboundDate = DateTime.Now,
                        CurrentQuantity = request.Quantity,
                        AvailableQuantity = request.Quantity,
                        Unit = request.Unit,
                        UnitPrice = request.UnitPrice,
                        TotalValue = request.Quantity * (request.UnitPrice ?? 0),
                        QualityStatus = request.QualityStatus,
                        InventoryStatus = InventoryStatus.Available,
                        IsBatchManaged = !string.IsNullOrEmpty(request.BatchNumber),
                        IsSerialManaged = !string.IsNullOrEmpty(request.SerialNumber),
                        Remarks = request.Remarks
                    };

                    await _db.Insertable(inventoryRecord).ExecuteCommandAsync();

                    // 记录事务
                    await CreateInventoryTransactionAsync(new InventoryTransaction
                    {
                        TransactionNumber = transactionNumber,
                        TransactionType = TransactionType.Inbound,
                        WarehouseId = request.WarehouseId,
                        LocationId = request.LocationId,
                        MaterialCode = request.MaterialCode,
                        MaterialName = request.MaterialName,
                        BatchNumber = request.BatchNumber,
                        SerialNumber = request.SerialNumber,
                        QuantityBefore = 0,
                        QuantityChanged = request.Quantity,
                        QuantityAfter = request.Quantity,
                        Unit = request.Unit,
                        UnitPrice = request.UnitPrice,
                        ValueChanged = request.Quantity * (request.UnitPrice ?? 0),
                        RelatedDocumentNumber = request.RelatedDocumentNumber,
                        OperatorId = request.OperatorId,
                        OperatorName = request.OperatorName,
                        Reason = "入库",
                        Remarks = request.Remarks
                    });
                }

                // 更新库位占用容量
                await _db.Updateable<WarehouseLocation>()
                    .SetColumns(l => new WarehouseLocation 
                    { 
                        UsedCapacity = l.UsedCapacity + request.Quantity,
                        LocationStatus = LocationStatus.PartiallyOccupied
                    })
                    .Where(l => l.Id == request.LocationId)
                    .ExecuteCommandAsync();

                return true;
            });

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "入库操作失败: {MaterialCode}", request.MaterialCode);
            return false;
        }
        finally
        {
            _transactionSemaphore.Release();
        }
    }

    public async Task<bool> OutboundAsync(OutboundRequest request)
    {
        await _transactionSemaphore.WaitAsync();
        try
        {
            var result = await _db.Ado.UseTranAsync(async () =>
            {
                // 查找库存记录
                var inventoryRecord = await _db.Queryable<InventoryRecord>()
                    .Where(i => i.WarehouseId == request.WarehouseId &&
                               i.LocationId == request.LocationId &&
                               i.MaterialCode == request.MaterialCode &&
                               (string.IsNullOrEmpty(request.BatchNumber) || i.BatchNumber == request.BatchNumber) &&
                               (string.IsNullOrEmpty(request.SerialNumber) || i.SerialNumber == request.SerialNumber) &&
                               !i.IsDeleted)
                    .OrderBy(i => i.InboundDate) // FIFO
                    .FirstAsync();

                if (inventoryRecord == null)
                {
                    throw new InvalidOperationException("未找到对应的库存记录");
                }

                if (inventoryRecord.AvailableQuantity < request.Quantity)
                {
                    throw new InvalidOperationException($"可用库存不足，当前可用: {inventoryRecord.AvailableQuantity}，需要: {request.Quantity}");
                }

                var transactionNumber = await GenerateTransactionNumberAsync();
                var quantityBefore = inventoryRecord.CurrentQuantity;

                // 更新库存记录
                inventoryRecord.CurrentQuantity -= request.Quantity;
                inventoryRecord.AvailableQuantity -= request.Quantity;
                inventoryRecord.TotalValue = inventoryRecord.CurrentQuantity * (inventoryRecord.UnitPrice ?? 0);
                inventoryRecord.UpdatedTime = DateTime.Now;

                if (inventoryRecord.CurrentQuantity == 0)
                {
                    inventoryRecord.InventoryStatus = InventoryStatus.OutOfStock;
                }

                await _db.Updateable(inventoryRecord).ExecuteCommandAsync();

                // 记录事务
                await CreateInventoryTransactionAsync(new InventoryTransaction
                {
                    TransactionNumber = transactionNumber,
                    TransactionType = TransactionType.Outbound,
                    WarehouseId = request.WarehouseId,
                    LocationId = request.LocationId,
                    MaterialCode = request.MaterialCode,
                    MaterialName = inventoryRecord.MaterialName,
                    BatchNumber = request.BatchNumber,
                    SerialNumber = request.SerialNumber,
                    QuantityBefore = quantityBefore,
                    QuantityChanged = -request.Quantity,
                    QuantityAfter = inventoryRecord.CurrentQuantity,
                    Unit = inventoryRecord.Unit,
                    UnitPrice = inventoryRecord.UnitPrice,
                    ValueChanged = -request.Quantity * (inventoryRecord.UnitPrice ?? 0),
                    RelatedDocumentNumber = request.RelatedDocumentNumber,
                    OperatorId = request.OperatorId,
                    OperatorName = request.OperatorName,
                    Reason = request.Reason ?? "出库",
                    Remarks = request.Remarks
                });

                // 更新库位占用容量
                await _db.Updateable<WarehouseLocation>()
                    .SetColumns(l => new WarehouseLocation 
                    { 
                        UsedCapacity = l.UsedCapacity - request.Quantity
                    })
                    .Where(l => l.Id == request.LocationId)
                    .ExecuteCommandAsync();

                return true;
            });

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "出库操作失败: {MaterialCode}", request.MaterialCode);
            return false;
        }
        finally
        {
            _transactionSemaphore.Release();
        }
    }

    public async Task<bool> TransferAsync(TransferRequest request)
    {
        await _transactionSemaphore.WaitAsync();
        try
        {
            var result = await _db.Ado.UseTranAsync(async () =>
            {
                // 出库操作
                var outboundRequest = new OutboundRequest
                {
                    WarehouseId = request.FromWarehouseId,
                    LocationId = request.FromLocationId,
                    MaterialCode = request.MaterialCode,
                    BatchNumber = request.BatchNumber,
                    SerialNumber = request.SerialNumber,
                    Quantity = request.Quantity,
                    Reason = request.Reason,
                    Remarks = request.Remarks,
                    OperatorId = request.OperatorId,
                    OperatorName = request.OperatorName
                };

                var outboundSuccess = await OutboundAsync(outboundRequest);
                if (!outboundSuccess)
                {
                    throw new InvalidOperationException("移库出库操作失败");
                }

                // 获取物料信息用于入库
                var materialInfo = await _db.Queryable<InventoryRecord>()
                    .Where(i => i.MaterialCode == request.MaterialCode && !i.IsDeleted)
                    .Select(i => new { i.MaterialName, i.MaterialType, i.Unit, i.UnitPrice })
                    .FirstAsync();

                // 入库操作
                var inboundRequest = new InboundRequest
                {
                    WarehouseId = request.ToWarehouseId,
                    LocationId = request.ToLocationId,
                    MaterialCode = request.MaterialCode,
                    MaterialName = materialInfo?.MaterialName ?? "",
                    MaterialType = materialInfo?.MaterialType ?? MaterialType.RawMaterial,
                    BatchNumber = request.BatchNumber,
                    SerialNumber = request.SerialNumber,
                    Quantity = request.Quantity,
                    Unit = materialInfo?.Unit ?? "",
                    UnitPrice = materialInfo?.UnitPrice,
                    QualityStatus = QualityStatus.Qualified,
                    Remarks = request.Remarks,
                    OperatorId = request.OperatorId,
                    OperatorName = request.OperatorName
                };

                var inboundSuccess = await InboundAsync(inboundRequest);
                if (!inboundSuccess)
                {
                    throw new InvalidOperationException("移库入库操作失败");
                }

                return true;
            });

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移库操作失败: {MaterialCode}", request.MaterialCode);
            return false;
        }
        finally
        {
            _transactionSemaphore.Release();
        }
    }

    public async Task<bool> AdjustInventoryAsync(AdjustmentRequest request)
    {
        await _transactionSemaphore.WaitAsync();
        try
        {
            var result = await _db.Ado.UseTranAsync(async () =>
            {
                var inventoryRecord = await _db.Queryable<InventoryRecord>()
                    .Where(i => i.Id == request.InventoryRecordId && !i.IsDeleted)
                    .FirstAsync();

                if (inventoryRecord == null)
                {
                    throw new InvalidOperationException("未找到库存记录");
                }

                var transactionNumber = await GenerateTransactionNumberAsync();
                var quantityBefore = inventoryRecord.CurrentQuantity;
                var quantityChanged = request.NewQuantity - inventoryRecord.CurrentQuantity;

                // 更新库存记录
                inventoryRecord.CurrentQuantity = request.NewQuantity;
                inventoryRecord.AvailableQuantity = request.NewQuantity - inventoryRecord.FrozenQuantity;
                inventoryRecord.TotalValue = request.NewQuantity * (inventoryRecord.UnitPrice ?? 0);
                inventoryRecord.UpdatedTime = DateTime.Now;

                await _db.Updateable(inventoryRecord).ExecuteCommandAsync();

                // 记录事务
                await CreateInventoryTransactionAsync(new InventoryTransaction
                {
                    TransactionNumber = transactionNumber,
                    TransactionType = TransactionType.InventoryAdjustment,
                    WarehouseId = inventoryRecord.WarehouseId,
                    LocationId = inventoryRecord.LocationId,
                    MaterialCode = inventoryRecord.MaterialCode,
                    MaterialName = inventoryRecord.MaterialName,
                    BatchNumber = inventoryRecord.BatchNumber,
                    SerialNumber = inventoryRecord.SerialNumber,
                    QuantityBefore = quantityBefore,
                    QuantityChanged = quantityChanged,
                    QuantityAfter = request.NewQuantity,
                    Unit = inventoryRecord.Unit,
                    UnitPrice = inventoryRecord.UnitPrice,
                    ValueChanged = quantityChanged * (inventoryRecord.UnitPrice ?? 0),
                    OperatorId = request.OperatorId,
                    OperatorName = request.OperatorName,
                    Reason = request.Reason,
                    Remarks = request.Remarks
                });

                return true;
            });

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "库存调整失败: {InventoryRecordId}", request.InventoryRecordId);
            return false;
        }
        finally
        {
            _transactionSemaphore.Release();
        }
    }

    public async Task<bool> FreezeInventoryAsync(FreezeRequest request)
    {
        await _transactionSemaphore.WaitAsync();
        try
        {
            var result = await _db.Ado.UseTranAsync(async () =>
            {
                var inventoryRecord = await _db.Queryable<InventoryRecord>()
                    .Where(i => i.Id == request.InventoryRecordId && !i.IsDeleted)
                    .FirstAsync();

                if (inventoryRecord == null)
                {
                    throw new InvalidOperationException("未找到库存记录");
                }

                if (inventoryRecord.AvailableQuantity < request.Quantity)
                {
                    throw new InvalidOperationException("可用库存不足");
                }

                var transactionNumber = await GenerateTransactionNumberAsync();

                // 更新库存记录
                inventoryRecord.FrozenQuantity += request.Quantity;
                inventoryRecord.AvailableQuantity -= request.Quantity;
                inventoryRecord.UpdatedTime = DateTime.Now;

                await _db.Updateable(inventoryRecord).ExecuteCommandAsync();

                // 记录事务
                await CreateInventoryTransactionAsync(new InventoryTransaction
                {
                    TransactionNumber = transactionNumber,
                    TransactionType = TransactionType.Freeze,
                    WarehouseId = inventoryRecord.WarehouseId,
                    LocationId = inventoryRecord.LocationId,
                    MaterialCode = inventoryRecord.MaterialCode,
                    MaterialName = inventoryRecord.MaterialName,
                    BatchNumber = inventoryRecord.BatchNumber,
                    SerialNumber = inventoryRecord.SerialNumber,
                    QuantityBefore = inventoryRecord.CurrentQuantity,
                    QuantityChanged = 0,
                    QuantityAfter = inventoryRecord.CurrentQuantity,
                    Unit = inventoryRecord.Unit,
                    OperatorId = request.OperatorId,
                    OperatorName = request.OperatorName,
                    Reason = request.Reason,
                    Remarks = $"冻结数量: {request.Quantity}. {request.Remarks}"
                });

                return true;
            });

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "冻结库存失败: {InventoryRecordId}", request.InventoryRecordId);
            return false;
        }
        finally
        {
            _transactionSemaphore.Release();
        }
    }

    public async Task<bool> UnfreezeInventoryAsync(UnfreezeRequest request)
    {
        await _transactionSemaphore.WaitAsync();
        try
        {
            var result = await _db.Ado.UseTranAsync(async () =>
            {
                var inventoryRecord = await _db.Queryable<InventoryRecord>()
                    .Where(i => i.Id == request.InventoryRecordId && !i.IsDeleted)
                    .FirstAsync();

                if (inventoryRecord == null)
                {
                    throw new InvalidOperationException("未找到库存记录");
                }

                if (inventoryRecord.FrozenQuantity < request.Quantity)
                {
                    throw new InvalidOperationException("冻结库存不足");
                }

                var transactionNumber = await GenerateTransactionNumberAsync();

                // 更新库存记录
                inventoryRecord.FrozenQuantity -= request.Quantity;
                inventoryRecord.AvailableQuantity += request.Quantity;
                inventoryRecord.UpdatedTime = DateTime.Now;

                await _db.Updateable(inventoryRecord).ExecuteCommandAsync();

                // 记录事务
                await CreateInventoryTransactionAsync(new InventoryTransaction
                {
                    TransactionNumber = transactionNumber,
                    TransactionType = TransactionType.Unfreeze,
                    WarehouseId = inventoryRecord.WarehouseId,
                    LocationId = inventoryRecord.LocationId,
                    MaterialCode = inventoryRecord.MaterialCode,
                    MaterialName = inventoryRecord.MaterialName,
                    BatchNumber = inventoryRecord.BatchNumber,
                    SerialNumber = inventoryRecord.SerialNumber,
                    QuantityBefore = inventoryRecord.CurrentQuantity,
                    QuantityChanged = 0,
                    QuantityAfter = inventoryRecord.CurrentQuantity,
                    Unit = inventoryRecord.Unit,
                    OperatorId = request.OperatorId,
                    OperatorName = request.OperatorName,
                    Reason = request.Reason,
                    Remarks = $"解冻数量: {request.Quantity}. {request.Remarks}"
                });

                return true;
            });

            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "解冻库存失败: {InventoryRecordId}", request.InventoryRecordId);
            return false;
        }
        finally
        {
            _transactionSemaphore.Release();
        }
    }

    public async Task<bool> ReserveInventoryAsync(ReserveRequest request)
    {
        try
        {
            // 查找可用库存
            var inventoryRecords = await _db.Queryable<InventoryRecord>()
                .Where(i => i.MaterialCode == request.MaterialCode &&
                           i.WarehouseId == request.WarehouseId &&
                           (string.IsNullOrEmpty(request.BatchNumber) || i.BatchNumber == request.BatchNumber) &&
                           !i.IsDeleted &&
                           i.AvailableQuantity > 0)
                .OrderBy(i => i.InboundDate)
                .ToListAsync();

            var totalAvailable = inventoryRecords.Sum(i => i.AvailableQuantity);
            if (totalAvailable < request.Quantity)
            {
                throw new InvalidOperationException($"可用库存不足，需要: {request.Quantity}，可用: {totalAvailable}");
            }

            var remainingQuantity = request.Quantity;
            foreach (var record in inventoryRecords)
            {
                if (remainingQuantity <= 0) break;

                var reserveQuantity = Math.Min(remainingQuantity, record.AvailableQuantity);
                
                // 这里可以实现具体的预留逻辑
                // 例如创建预留记录表等
                
                remainingQuantity -= reserveQuantity;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "预留库存失败: {MaterialCode}", request.MaterialCode);
            return false;
        }
    }

    public async Task<bool> CancelReservationAsync(CancelReservationRequest request)
    {
        try
        {
            // 这里实现取消预留的逻辑
            // 通常需要预留记录表来跟踪预留信息
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消预留失败: {InventoryRecordId}", request.InventoryRecordId);
            return false;
        }
    }

    #endregion

    #region 库存查询

    public async Task<List<InventoryRecord>> GetInventoryByMaterialAsync(string materialCode, long? warehouseId = null)
    {
        var query = _db.Queryable<InventoryRecord>()
            .Where(i => i.MaterialCode == materialCode && !i.IsDeleted && i.CurrentQuantity > 0);

        if (warehouseId.HasValue)
        {
            query = query.Where(i => i.WarehouseId == warehouseId.Value);
        }

        return await query
            .Includes(i => i.Warehouse)
            .Includes(i => i.Location)
            .OrderBy(i => i.InboundDate)
            .ToListAsync();
    }

    public async Task<List<InventoryRecord>> GetInventoryByBatchAsync(string batchNumber)
    {
        return await _db.Queryable<InventoryRecord>()
            .Where(i => i.BatchNumber == batchNumber && !i.IsDeleted && i.CurrentQuantity > 0)
            .Includes(i => i.Warehouse)
            .Includes(i => i.Location)
            .OrderBy(i => i.InboundDate)
            .ToListAsync();
    }

    public async Task<InventoryRecord?> GetInventoryBySerialNumberAsync(string serialNumber)
    {
        return await _db.Queryable<InventoryRecord>()
            .Where(i => i.SerialNumber == serialNumber && !i.IsDeleted)
            .Includes(i => i.Warehouse)
            .Includes(i => i.Location)
            .FirstAsync();
    }

    public async Task<WarehouseInventorySummary> GetWarehouseInventorySummaryAsync(long warehouseId)
    {
        var warehouse = await GetWarehouseByIdAsync(warehouseId);
        if (warehouse == null)
        {
            throw new InvalidOperationException("仓库不存在");
        }

        var inventoryRecords = await _db.Queryable<InventoryRecord>()
            .Where(i => i.WarehouseId == warehouseId && !i.IsDeleted && i.CurrentQuantity > 0)
            .ToListAsync();

        var materialSummaries = inventoryRecords
            .GroupBy(i => new { i.MaterialCode, i.MaterialName, i.MaterialType, i.Unit })
            .Select(g => new MaterialInventorySummary
            {
                MaterialCode = g.Key.MaterialCode,
                MaterialName = g.Key.MaterialName,
                MaterialType = g.Key.MaterialType,
                TotalQuantity = g.Sum(i => i.CurrentQuantity),
                AvailableQuantity = g.Sum(i => i.AvailableQuantity),
                FrozenQuantity = g.Sum(i => i.FrozenQuantity),
                Unit = g.Key.Unit,
                TotalValue = g.Sum(i => i.TotalValue),
                BatchCount = g.Count(),
                OldestInboundDate = g.Min(i => i.InboundDate),
                LatestInboundDate = g.Max(i => i.InboundDate)
            })
            .ToList();

        var locations = await GetLocationsByWarehouseAsync(warehouseId);
        var availableLocations = locations.Count(l => l.LocationStatus == LocationStatus.Available);
        var occupiedLocations = locations.Count(l => l.LocationStatus != LocationStatus.Available);

        return new WarehouseInventorySummary
        {
            WarehouseId = warehouseId,
            WarehouseName = warehouse.WarehouseName,
            TotalMaterials = materialSummaries.Count,
            TotalBatches = inventoryRecords.Count,
            TotalValue = materialSummaries.Sum(m => m.TotalValue ?? 0),
            AvailableLocations = availableLocations,
            OccupiedLocations = occupiedLocations,
            UtilizationRate = locations.Any() ? (decimal)occupiedLocations / locations.Count * 100 : 0,
            MaterialSummaries = materialSummaries
        };
    }

    public async Task<(List<InventoryRecord> Items, int Total)> GetInventoryPagedAsync(InventoryQueryRequest request)
    {
        var query = _db.Queryable<InventoryRecord>()
            .Where(i => !i.IsDeleted);

        // 添加查询条件
        if (request.WarehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == request.WarehouseId.Value);

        if (request.LocationId.HasValue)
            query = query.Where(i => i.LocationId == request.LocationId.Value);

        if (!string.IsNullOrEmpty(request.MaterialCode))
            query = query.Where(i => i.MaterialCode.Contains(request.MaterialCode));

        if (request.MaterialType.HasValue)
            query = query.Where(i => i.MaterialType == request.MaterialType.Value);

        if (!string.IsNullOrEmpty(request.BatchNumber))
            query = query.Where(i => i.BatchNumber == request.BatchNumber);

        if (!string.IsNullOrEmpty(request.SerialNumber))
            query = query.Where(i => i.SerialNumber == request.SerialNumber);

        if (request.QualityStatus.HasValue)
            query = query.Where(i => i.QualityStatus == request.QualityStatus.Value);

        if (request.InventoryStatus.HasValue)
            query = query.Where(i => i.InventoryStatus == request.InventoryStatus.Value);

        if (request.InboundDateFrom.HasValue)
            query = query.Where(i => i.InboundDate >= request.InboundDateFrom.Value);

        if (request.InboundDateTo.HasValue)
            query = query.Where(i => i.InboundDate <= request.InboundDateTo.Value);

        if (request.IsExpired.HasValue)
        {
            var now = DateTime.Now;
            if (request.IsExpired.Value)
                query = query.Where(i => i.ExpiryDate.HasValue && i.ExpiryDate.Value < now);
            else
                query = query.Where(i => !i.ExpiryDate.HasValue || i.ExpiryDate.Value >= now);
        }

        var total = await query.CountAsync();

        var items = await query
            .Includes(i => i.Warehouse)
            .Includes(i => i.Location)
            .OrderByDescending(i => i.InboundDate)
            .ToPageListAsync(request.PageIndex, request.PageSize);

        return (items, total);
    }

    public async Task<List<InventoryDetail>> GetInventoryDetailsAsync(InventoryDetailRequest request)
    {
        var query = _db.Queryable<InventoryRecord>()
            .Where(i => !i.IsDeleted && i.CurrentQuantity > 0);

        if (request.WarehouseId.HasValue)
            query = query.Where(i => i.WarehouseId == request.WarehouseId.Value);

        if (!string.IsNullOrEmpty(request.MaterialCode))
            query = query.Where(i => i.MaterialCode == request.MaterialCode);

        if (!string.IsNullOrEmpty(request.BatchNumber))
            query = query.Where(i => i.BatchNumber == request.BatchNumber);

        var inventoryRecords = await query
            .Includes(i => i.Warehouse)
            .Includes(i => i.Location)
            .ToListAsync();

        var details = new List<InventoryDetail>();

        foreach (var record in inventoryRecords)
        {
            var detail = new InventoryDetail
            {
                InventoryRecord = record,
                Location = record.Location!,
                Warehouse = record.Warehouse!
            };

            if (request.IncludeTransactionHistory)
            {
                detail.TransactionHistory = await GetInventoryTransactionsAsync(
                    record.MaterialCode, null, null);
            }

            details.Add(detail);
        }

        return details;
    }

    public async Task<List<InventoryTransaction>> GetInventoryTransactionsAsync(string materialCode, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _db.Queryable<InventoryTransaction>()
            .Where(t => t.MaterialCode == materialCode && !t.IsDeleted);

        if (startDate.HasValue)
            query = query.Where(t => t.TransactionTime >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(t => t.TransactionTime <= endDate.Value);

        return await query
            .OrderByDescending(t => t.TransactionTime)
            .ToListAsync();
    }

    #endregion

    #region 库存盘点

    public async Task<long> CreateInventoryCountPlanAsync(InventoryCountPlan plan)
    {
        try
        {
            // 生成盘点计划编号
            plan.PlanNumber = await GenerateCountPlanNumberAsync();

            // 将计划对象插入数据库（这里需要创建对应的实体类）
            // 暂时返回模拟ID
            return DateTime.Now.Ticks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建盘点计划失败: {PlanName}", plan.PlanName);
            throw;
        }
    }

    public async Task<bool> ExecuteInventoryCountAsync(long planId, List<InventoryCountDetail> countDetails)
    {
        try
        {
            // 这里实现盘点执行逻辑
            // 需要创建盘点明细表来存储盘点结果
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "执行盘点失败: {PlanId}", planId);
            return false;
        }
    }

    public async Task<bool> ConfirmInventoryDifferencesAsync(long planId, List<InventoryDifferenceConfirmation> confirmations)
    {
        try
        {
            // 这里实现盘点差异确认逻辑
            // 对于确认的差异，需要创建库存调整记录
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "确认盘点差异失败: {PlanId}", planId);
            return false;
        }
    }

    public async Task<List<InventoryCountPlan>> GetInventoryCountPlansAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // 这里需要从盘点计划表查询
        // 暂时返回空列表
        return new List<InventoryCountPlan>();
    }

    #endregion

    #region 库存预警

    public async Task<bool> ConfigureInventoryAlertAsync(InventoryAlert alert)
    {
        try
        {
            // 检查是否已存在相同配置
            var existingAlert = await _db.Queryable<InventoryAlert>()
                .Where(a => a.MaterialCode == alert.MaterialCode &&
                           a.WarehouseId == alert.WarehouseId &&
                           a.AlertType == alert.AlertType &&
                           !a.IsDeleted)
                .FirstAsync();

            if (existingAlert != null)
            {
                // 更新现有配置
                existingAlert.MinQuantity = alert.MinQuantity;
                existingAlert.MaxQuantity = alert.MaxQuantity;
                existingAlert.SafetyStockQuantity = alert.SafetyStockQuantity;
                existingAlert.ExpiryWarningDays = alert.ExpiryWarningDays;
                existingAlert.IsEnabled = alert.IsEnabled;
                existingAlert.AlertLevel = alert.AlertLevel;
                existingAlert.NotificationMethods = alert.NotificationMethods;
                existingAlert.Remarks = alert.Remarks;
                existingAlert.UpdatedTime = DateTime.Now;

                return await _db.Updateable(existingAlert).ExecuteCommandHasChangeAsync();
            }
            else
            {
                // 创建新配置
                await _db.Insertable(alert).ExecuteCommandAsync();
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "配置库存预警失败: {MaterialCode}", alert.MaterialCode);
            return false;
        }
    }

    public async Task<List<InventoryAlert>> GetInventoryAlertsAsync(long? warehouseId = null)
    {
        var query = _db.Queryable<InventoryAlert>()
            .Where(a => !a.IsDeleted && a.IsEnabled);

        if (warehouseId.HasValue)
            query = query.Where(a => a.WarehouseId == warehouseId.Value);

        return await query
            .Includes(a => a.Warehouse)
            .OrderBy(a => a.MaterialCode)
            .ToListAsync();
    }

    public async Task<List<InventoryAlertResult>> CheckInventoryAlertsAsync()
    {
        var alertResults = new List<InventoryAlertResult>();

        try
        {
            var alerts = await GetInventoryAlertsAsync();

            foreach (var alert in alerts)
            {
                var inventoryRecords = await GetInventoryByMaterialAsync(alert.MaterialCode, alert.WarehouseId);
                var totalQuantity = inventoryRecords.Sum(i => i.CurrentQuantity);

                bool isTriggered = false;
                string alertMessage = "";

                switch (alert.AlertType)
                {
                    case AlertType.LowStock:
                        if (alert.MinQuantity.HasValue && totalQuantity <= alert.MinQuantity.Value)
                        {
                            isTriggered = true;
                            alertMessage = $"物料 {alert.MaterialCode} 库存不足，当前: {totalQuantity}，最小: {alert.MinQuantity}";
                        }
                        break;

                    case AlertType.HighStock:
                        if (alert.MaxQuantity.HasValue && totalQuantity >= alert.MaxQuantity.Value)
                        {
                            isTriggered = true;
                            alertMessage = $"物料 {alert.MaterialCode} 库存过多，当前: {totalQuantity}，最大: {alert.MaxQuantity}";
                        }
                        break;

                    case AlertType.ZeroStock:
                        if (totalQuantity == 0)
                        {
                            isTriggered = true;
                            alertMessage = $"物料 {alert.MaterialCode} 零库存";
                        }
                        break;

                    case AlertType.SafetyStock:
                        if (alert.SafetyStockQuantity.HasValue && totalQuantity <= alert.SafetyStockQuantity.Value)
                        {
                            isTriggered = true;
                            alertMessage = $"物料 {alert.MaterialCode} 低于安全库存，当前: {totalQuantity}，安全库存: {alert.SafetyStockQuantity}";
                        }
                        break;

                    case AlertType.ExpiryWarning:
                        if (alert.ExpiryWarningDays.HasValue)
                        {
                            var expiringRecords = inventoryRecords.Where(i => 
                                i.ExpiryDate.HasValue && 
                                i.ExpiryDate.Value <= DateTime.Now.AddDays(alert.ExpiryWarningDays.Value))
                                .ToList();

                            if (expiringRecords.Any())
                            {
                                isTriggered = true;
                                alertMessage = $"物料 {alert.MaterialCode} 有 {expiringRecords.Count} 个批次即将过期";
                            }
                        }
                        break;
                }

                if (isTriggered)
                {
                    alertResults.Add(new InventoryAlertResult
                    {
                        AlertId = alert.Id,
                        AlertType = alert.AlertType,
                        AlertLevel = alert.AlertLevel,
                        MaterialCode = alert.MaterialCode,
                        MaterialName = inventoryRecords.FirstOrDefault()?.MaterialName ?? "",
                        WarehouseId = alert.WarehouseId,
                        WarehouseName = alert.Warehouse?.WarehouseName ?? "",
                        CurrentQuantity = totalQuantity,
                        ThresholdQuantity = alert.MinQuantity ?? alert.MaxQuantity ?? alert.SafetyStockQuantity,
                        AlertMessage = alertMessage,
                        AlertTime = DateTime.Now
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "检查库存预警失败");
        }

        return alertResults;
    }

    public async Task<List<InventoryAlertHistory>> GetAlertHistoryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        // 这里需要从预警历史表查询
        // 暂时返回空列表
        return new List<InventoryAlertHistory>();
    }

    #endregion

    #region 库存分析

    public async Task<List<InventoryTurnoverAnalysis>> GetInventoryTurnoverAnalysisAsync(DateTime startDate, DateTime endDate)
    {
        var turnoverAnalysis = new List<InventoryTurnoverAnalysis>();

        try
        {
            // 获取所有物料的出库记录
            var outboundTransactions = await _db.Queryable<InventoryTransaction>()
                .Where(t => t.TransactionType == TransactionType.Outbound &&
                           t.TransactionTime >= startDate &&
                           t.TransactionTime <= endDate &&
                           !t.IsDeleted)
                .ToListAsync();

            var materialGroups = outboundTransactions.GroupBy(t => t.MaterialCode);

            foreach (var group in materialGroups)
            {
                var materialCode = group.Key;
                var totalConsumption = group.Sum(t => Math.Abs(t.QuantityChanged));

                // 获取平均库存
                var inventoryRecords = await GetInventoryByMaterialAsync(materialCode);
                var averageInventory = inventoryRecords.Sum(i => i.CurrentQuantity);

                if (averageInventory > 0)
                {
                    var turnoverRate = totalConsumption / averageInventory;
                    var turnoverDays = (int)((endDate - startDate).TotalDays / (double)turnoverRate);

                    var materialInfo = inventoryRecords.FirstOrDefault();
                    
                    turnoverAnalysis.Add(new InventoryTurnoverAnalysis
                    {
                        MaterialCode = materialCode,
                        MaterialName = materialInfo?.MaterialName ?? "",
                        AverageInventory = averageInventory,
                        TotalConsumption = totalConsumption,
                        TurnoverRate = turnoverRate,
                        TurnoverDays = turnoverDays,
                        InventoryValue = inventoryRecords.Sum(i => i.TotalValue ?? 0),
                        TurnoverGrade = turnoverDays <= 30 ? "Fast" : turnoverDays <= 90 ? "Normal" : "Slow"
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "库存周转率分析失败");
        }

        return turnoverAnalysis.OrderBy(t => t.TurnoverDays).ToList();
    }

    public async Task<InventoryValueAnalysis> GetInventoryValueAnalysisAsync(long? warehouseId = null)
    {
        var analysis = new InventoryValueAnalysis();

        try
        {
            var query = _db.Queryable<InventoryRecord>()
                .Where(i => !i.IsDeleted && i.CurrentQuantity > 0);

            if (warehouseId.HasValue)
                query = query.Where(i => i.WarehouseId == warehouseId.Value);

            var inventoryRecords = await query
                .Includes(i => i.Warehouse)
                .ToListAsync();

            analysis.TotalInventoryValue = inventoryRecords.Sum(i => i.TotalValue ?? 0);
            analysis.RawMaterialValue = inventoryRecords
                .Where(i => i.MaterialType == MaterialType.RawMaterial)
                .Sum(i => i.TotalValue ?? 0);
            analysis.WorkInProgressValue = inventoryRecords
                .Where(i => i.MaterialType == MaterialType.WorkInProgress)
                .Sum(i => i.TotalValue ?? 0);
            analysis.FinishedGoodsValue = inventoryRecords
                .Where(i => i.MaterialType == MaterialType.FinishedGoods)
                .Sum(i => i.TotalValue ?? 0);

            // 物料价值排行
            var materialValueGroups = inventoryRecords
                .GroupBy(i => new { i.MaterialCode, i.MaterialName })
                .Select(g => new
                {
                    MaterialCode = g.Key.MaterialCode,
                    MaterialName = g.Key.MaterialName,
                    TotalValue = g.Sum(i => i.TotalValue ?? 0)
                })
                .OrderByDescending(g => g.TotalValue)
                .Take(20)
                .ToList();

            analysis.MaterialValueRankings = materialValueGroups
                .Select((g, index) => new MaterialValueRanking
                {
                    Ranking = index + 1,
                    MaterialCode = g.MaterialCode,
                    MaterialName = g.MaterialName,
                    InventoryValue = g.TotalValue,
                    Percentage = analysis.TotalInventoryValue > 0 ? g.TotalValue / analysis.TotalInventoryValue * 100 : 0
                })
                .ToList();

            // 仓库价值汇总
            if (!warehouseId.HasValue)
            {
                var warehouseValueGroups = inventoryRecords
                    .GroupBy(i => new { i.WarehouseId, i.Warehouse!.WarehouseName })
                    .Select(g => new
                    {
                        WarehouseId = g.Key.WarehouseId,
                        WarehouseName = g.Key.WarehouseName,
                        TotalValue = g.Sum(i => i.TotalValue ?? 0)
                    })
                    .ToList();

                analysis.WarehouseValueSummaries = warehouseValueGroups
                    .Select(g => new WarehouseValueSummary
                    {
                        WarehouseId = g.WarehouseId,
                        WarehouseName = g.WarehouseName,
                        TotalValue = g.TotalValue,
                        Percentage = analysis.TotalInventoryValue > 0 ? g.TotalValue / analysis.TotalInventoryValue * 100 : 0
                    })
                    .ToList();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "库存价值分析失败");
        }

        return analysis;
    }

    public async Task<List<AbcAnalysisResult>> GetAbcAnalysisAsync(DateTime startDate, DateTime endDate)
    {
        var abcResults = new List<AbcAnalysisResult>();

        try
        {
            // 获取消耗数据 - 先获取所有原始数据
            var transactions = await _db.Queryable<InventoryTransaction>()
                .Where(t => t.TransactionType == TransactionType.Outbound &&
                           t.TransactionTime >= startDate &&
                           t.TransactionTime <= endDate &&
                           !t.IsDeleted)
                .Select(t => new 
                {
                    t.MaterialCode,
                    t.ValueChanged,
                    t.QuantityChanged
                })
                .ToListAsync();

            // 在内存中进行GroupBy聚合
            var consumptionData = transactions
                .GroupBy(t => t.MaterialCode)
                .Select(g => new
                {
                    MaterialCode = g.Key,
                    ConsumptionValue = g.Sum(t => Math.Abs(t.ValueChanged ?? 0)),
                    ConsumptionQuantity = g.Sum(t => Math.Abs(t.QuantityChanged))
                })
                .ToList();

            var totalValue = consumptionData.Sum(d => d.ConsumptionValue);

            var sortedDataList = consumptionData
                .OrderByDescending(d => d.ConsumptionValue)
                .ToList();

            decimal cumulativeValue = 0;
            for (int i = 0; i < sortedDataList.Count; i++)
            {
                var data = sortedDataList[i];
                cumulativeValue += data.ConsumptionValue;
                var cumulativePercentage = totalValue > 0 ? cumulativeValue / totalValue * 100 : 0;

                var materialInfo = await _db.Queryable<InventoryRecord>()
                    .Where(ir => ir.MaterialCode == data.MaterialCode && !ir.IsDeleted)
                    .Select(ir => ir.MaterialName)
                    .FirstAsync();

                string category = "C";
                if (cumulativePercentage <= 80) category = "A";
                else if (cumulativePercentage <= 95) category = "B";

                abcResults.Add(new AbcAnalysisResult
                {
                    MaterialCode = data.MaterialCode,
                    MaterialName = materialInfo ?? "",
                    ConsumptionValue = data.ConsumptionValue,
                    ConsumptionQuantity = data.ConsumptionQuantity,
                    ValuePercentage = totalValue > 0 ? data.ConsumptionValue / totalValue * 100 : 0,
                    CumulativePercentage = cumulativePercentage,
                    AbcCategory = category
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ABC分析失败");
        }

        return abcResults;
    }

    public async Task<List<InventoryAgingAnalysis>> GetInventoryAgingAnalysisAsync(long? warehouseId = null)
    {
        var agingAnalysis = new List<InventoryAgingAnalysis>();

        try
        {
            var query = _db.Queryable<InventoryRecord>()
                .Where(i => !i.IsDeleted && i.CurrentQuantity > 0);

            if (warehouseId.HasValue)
                query = query.Where(i => i.WarehouseId == warehouseId.Value);

            var inventoryRecords = await query.ToListAsync();
            var now = DateTime.Now;

            foreach (var record in inventoryRecords)
            {
                var agingDays = (int)(now - record.InboundDate).TotalDays;
                
                string agingLevel = "Fresh";
                if (agingDays > 365) agingLevel = "Expired";
                else if (agingDays > 180) agingLevel = "Aging";
                else if (agingDays > 90) agingLevel = "Normal";

                agingAnalysis.Add(new InventoryAgingAnalysis
                {
                    MaterialCode = record.MaterialCode,
                    MaterialName = record.MaterialName,
                    BatchNumber = record.BatchNumber ?? "",
                    InboundDate = record.InboundDate,
                    AgingDays = agingDays,
                    Quantity = record.CurrentQuantity,
                    InventoryValue = record.TotalValue ?? 0,
                    AgingLevel = agingLevel
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "库存老化分析失败");
        }

        return agingAnalysis.OrderByDescending(a => a.AgingDays).ToList();
    }

    #endregion

    #region 私有方法

    private async Task<bool> CreateInventoryTransactionAsync(InventoryTransaction transaction)
    {
        try
        {
            await _db.Insertable(transaction).ExecuteCommandAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建库存事务记录失败");
            return false;
        }
    }

    private async Task<string> GenerateTransactionNumberAsync()
    {
        var now = DateTime.Now;
        var dateStr = now.ToString("yyyyMMdd");
        
        var lastTransaction = await _db.Queryable<InventoryTransaction>()
            .Where(t => t.TransactionNumber.StartsWith($"TXN{dateStr}"))
            .OrderByDescending(t => t.TransactionNumber)
            .Select(t => t.TransactionNumber)
            .FirstAsync();

        int sequence = 1;
        if (!string.IsNullOrEmpty(lastTransaction))
        {
            var lastSequence = lastTransaction.Substring(11); // TXN20231201001
            if (int.TryParse(lastSequence, out int seq))
            {
                sequence = seq + 1;
            }
        }

        return $"TXN{dateStr}{sequence:D3}";
    }

    private async Task<string> GenerateCountPlanNumberAsync()
    {
        var now = DateTime.Now;
        var dateStr = now.ToString("yyyyMMdd");
        
        // 这里需要从盘点计划表查询
        // 暂时返回固定格式
        return $"CP{dateStr}001";
    }

    #endregion
} 