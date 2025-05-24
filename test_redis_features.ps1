# Redis功能测试脚本
# 测试消息队列、缓存和报告系统

$baseUrl = "http://localhost:5000/api/v2"

Write-Host "=== 锂电池包装MES系统 Redis功能测试 ===" -ForegroundColor Green

# 等待API启动
Write-Host "等待API启动..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# 测试1: 发布生产线状态消息
Write-Host "`n1. 测试消息队列 - 发布生产线状态消息" -ForegroundColor Cyan
try {
    $productionMessage = @{
        LineId = "LINE001"
        Status = "Running"
        CurrentProduct = "BATTERY_PACK_001"
        Efficiency = 85.5
        Timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    }
    
    $response = Invoke-RestMethod -Uri "$baseUrl/messagequeue/production-line-status" -Method POST -Body ($productionMessage | ConvertTo-Json) -ContentType "application/json"
    Write-Host "✓ 生产线状态消息发布成功: $($response.message)" -ForegroundColor Green
} catch {
    Write-Host "✗ 生产线状态消息发布失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试2: 发布质量检测消息
Write-Host "`n2. 测试消息队列 - 发布质量检测消息" -ForegroundColor Cyan
try {
    $qualityMessage = @{
        ProductId = "BATTERY_PACK_001"
        TestResult = "Pass"
        TestType = "VoltageTest"
        TestValue = 48.2
        StandardValue = 48.0
        Tolerance = 0.5
        Timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    }
    
    $response = Invoke-RestMethod -Uri "$baseUrl/messagequeue/quality-check" -Method POST -Body ($qualityMessage | ConvertTo-Json) -ContentType "application/json"
    Write-Host "✓ 质量检测消息发布成功: $($response.message)" -ForegroundColor Green
} catch {
    Write-Host "✗ 质量检测消息发布失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试3: 获取消息历史
Write-Host "`n3. 测试消息队列 - 获取消息历史" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/messagequeue/history?messageType=ProductionLineStatus&limit=5" -Method GET
    Write-Host "✓ 获取消息历史成功，共 $($response.data.Count) 条消息" -ForegroundColor Green
    if ($response.data.Count -gt 0) {
        Write-Host "  最新消息: $($response.data[0].content)" -ForegroundColor Gray
    }
} catch {
    Write-Host "✗ 获取消息历史失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试4: 生产日报
Write-Host "`n4. 测试报告系统 - 生产日报" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/reports/production-daily" -Method GET
    Write-Host "✓ 生产日报获取成功" -ForegroundColor Green
    Write-Host "  报告日期: $($response.data.reportDate)" -ForegroundColor Gray
    Write-Host "  总产量: $($response.data.totalProduction)" -ForegroundColor Gray
    Write-Host "  合格率: $($response.data.qualityRate)%" -ForegroundColor Gray
} catch {
    Write-Host "✗ 生产日报获取失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试5: 质量统计报告
Write-Host "`n5. 测试报告系统 - 质量统计报告" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/reports/quality-statistics" -Method GET
    Write-Host "✓ 质量统计报告获取成功" -ForegroundColor Green
    Write-Host "  总检测数: $($response.data.totalTests)" -ForegroundColor Gray
    Write-Host "  合格数: $($response.data.passedTests)" -ForegroundColor Gray
    Write-Host "  不合格数: $($response.data.failedTests)" -ForegroundColor Gray
} catch {
    Write-Host "✗ 质量统计报告获取失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试6: 设备效率报告
Write-Host "`n6. 测试报告系统 - 设备效率报告" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/reports/equipment-efficiency" -Method GET
    Write-Host "✓ 设备效率报告获取成功" -ForegroundColor Green
    Write-Host "  平均效率: $($response.data.averageEfficiency)%" -ForegroundColor Gray
    Write-Host "  设备数量: $($response.data.equipmentData.Count)" -ForegroundColor Gray
} catch {
    Write-Host "✗ 设备效率报告获取失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 测试7: 缓存功能测试
Write-Host "`n7. 测试缓存系统" -ForegroundColor Cyan
Write-Host "  注意: 缓存功能已集成在报告系统中，重复请求应该更快" -ForegroundColor Gray

# 重复请求生产日报测试缓存
$startTime = Get-Date
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/reports/production-daily" -Method GET
    $endTime = Get-Date
    $duration = ($endTime - $startTime).TotalMilliseconds
    Write-Host "✓ 缓存测试成功，响应时间: $([math]::Round($duration, 2))ms" -ForegroundColor Green
} catch {
    Write-Host "✗ 缓存测试失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Redis功能测试完成 ===" -ForegroundColor Green
Write-Host "如需查看详细的API文档，请参考 REDIS_MESSAGING_CACHE_REPORT_GUIDE.md" -ForegroundColor Yellow 