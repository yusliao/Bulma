#!/usr/bin/env pwsh

# 锂电池包装MES系统 - 事件驱动架构测试脚本
# 测试事件发布、订阅、处理、重试、死信队列等功能

param(
    [string]$BaseUrl = "https://localhost:60162",
    [string]$Username = "admin",
    [string]$Password = "Admin123!",
    [switch]$Detailed,
    [switch]$SkipAuth
)

# 颜色输出函数
function Write-ColorOutput($ForegroundColor) {
    $fc = $host.UI.RawUI.ForegroundColor
    $host.UI.RawUI.ForegroundColor = $ForegroundColor
    if ($args) {
        Write-Output $args
    }
    else {
        $input | Write-Output
    }
    $host.UI.RawUI.ForegroundColor = $fc
}

function Write-Success { Write-ColorOutput Green $args }
function Write-Warning { Write-ColorOutput Yellow $args }
function Write-Error { Write-ColorOutput Red $args }
function Write-Info { Write-ColorOutput Cyan $args }

# 忽略SSL证书验证（开发环境）
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $null = [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
}

Write-Info "🚀 开始事件驱动架构测试..."
Write-Info "目标URL: $BaseUrl"

# 全局变量
$Global:Token = $null
$Global:Headers = @{ "Content-Type" = "application/json" }

# 获取认证令牌
function Get-AuthToken {
    if ($SkipAuth) {
        Write-Info "跳过认证"
        return
    }

    try {
        $loginData = @{
            username = $Username
            password = $Password
        } | ConvertTo-Json

        $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/Auth/login" -Method Post -Body $loginData -ContentType "application/json"
        
        if ($loginResponse.success) {
            $Global:Token = $loginResponse.data.token
            $Global:Headers["Authorization"] = "Bearer $Global:Token"
            Write-Success "✅ 认证成功"
        }
        else {
            Write-Error "❌ 认证失败: $($loginResponse.message)"
            return $false
        }
    }
    catch {
        Write-Error "❌ 认证请求失败: $($_.Exception.Message)"
        return $false
    }
    return $true
}

# 测试事件总线健康状态
function Test-EventBusHealth {
    Write-Info "🔍 测试事件总线健康状态..."
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v2/EventBus/health" -Method Get -Headers $Global:Headers
        
        if ($response.success) {
            $health = $response.data
            Write-Success "✅ 事件总线健康状态: $($health.overallHealth)"
            
            if ($Detailed) {
                Write-Info "Redis状态: $($health.components.redis.Status) (响应时间: $($health.components.redis.ResponseTimeMs)ms)"
                Write-Info "事件存储状态: $($health.components.eventStore.Status) (响应时间: $($health.components.eventStore.ResponseTimeMs)ms)"
                Write-Info "进程内存: $($health.metrics.processMemoryMB)MB"
                Write-Info "Redis内存: $($health.metrics.redisMemory)"
            }
            
            return $health.overallHealth -eq "Healthy"
        }
        else {
            Write-Error "❌ 获取健康状态失败: $($response.message)"
            return $false
        }
    }
    catch {
        Write-Error "❌ 健康检查请求失败: $($_.Exception.Message)"
        return $false
    }
}

# 发布测试事件
function Publish-TestEvent {
    param(
        [string]$EventType,
        [object]$EventData = @{}
    )
    
    Write-Info "📤 发布测试事件: $EventType"
    
    try {
        $eventDataJson = $EventData | ConvertTo-Json -Depth 3
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v2/EventBus/publish-test?eventType=$EventType" -Method Post -Body $eventDataJson -Headers $Global:Headers
        
        if ($response.success) {
            Write-Success "✅ 事件发布成功: $($response.eventId)"
            
            if ($Detailed) {
                Write-Info "事件ID: $($response.eventId)"
                Write-Info "事件类型: $($response.eventType)"
                Write-Info "发生时间: $($response.occurredOn)"
            }
            
            return $response
        }
        else {
            Write-Error "❌ 事件发布失败: $($response.message)"
            return $null
        }
    }
    catch {
        Write-Error "❌ 发布事件请求失败: $($_.Exception.Message)"
        return $null
    }
}

# 获取事件统计
function Get-EventStatistics {
    param(
        [DateTime]$From = (Get-Date).AddDays(-1),
        [DateTime]$To = (Get-Date)
    )
    
    Write-Info "📊 获取事件统计信息..."
    
    try {
        $fromStr = $From.ToString("yyyy-MM-ddTHH:mm:ssZ")
        $toStr = $To.ToString("yyyy-MM-ddTHH:mm:ssZ")
        
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v2/EventBus/statistics?from=$fromStr&to=$toStr" -Method Get -Headers $Global:Headers
        
        if ($response.success) {
            $stats = $response.data
            Write-Success "✅ 事件统计获取成功"
            Write-Info "时间范围: $($stats.period.from) 到 $($stats.period.to)"
            Write-Info "总事件数: $($stats.totalEvents)"
            
            if ($stats.eventTypeCounts -and $stats.eventTypeCounts.Count -gt 0) {
                Write-Info "事件类型分布:"
                $stats.eventTypeCounts.PSObject.Properties | ForEach-Object {
                    Write-Info "  $($_.Name): $($_.Value)"
                }
            }
            
            if ($Detailed -and $stats.realtimeMetrics) {
                Write-Info "实时指标:"
                $stats.realtimeMetrics.PSObject.Properties | ForEach-Object {
                    if ($_.Value -is [PSCustomObject]) {
                        Write-Info "  $($_.Name):"
                        $_.Value.PSObject.Properties | ForEach-Object {
                            Write-Info "    $($_.Name): $($_.Value)"
                        }
                    }
                }
            }
            
            return $stats
        }
        else {
            Write-Error "❌ 获取事件统计失败: $($response.message)"
            return $null
        }
    }
    catch {
        Write-Error "❌ 获取统计请求失败: $($_.Exception.Message)"
        return $null
    }
}

# 检查死信队列
function Get-DeadLetterQueue {
    param(
        [string]$EventType = "",
        [int]$Limit = 10
    )
    
    Write-Info "☠️ 检查死信队列..."
    
    try {
        $uri = "$BaseUrl/api/v2/EventBus/dead-letter-queue?limit=$Limit"
        if ($EventType) {
            $uri += "`&eventType=$EventType"
        }
        
        $response = Invoke-RestMethod -Uri $uri -Method Get -Headers $Global:Headers
        
        if ($response.success) {
            $dlq = $response.data
            Write-Success "✅ 死信队列检查完成"
            Write-Info "队列数量: $($dlq.totalKeys)"
            Write-Info "事件数量: $($dlq.totalEvents)"
            
            if ($dlq.events -and $dlq.events.Count -gt 0 -and $Detailed) {
                Write-Info "死信事件:"
                $dlq.events | ForEach-Object {
                    Write-Warning "  事件类型: $($_.EventType), 失败时间: $($_.DeadAt)"
                }
            }
            
            return $dlq
        }
        else {
            Write-Error "❌ 检查死信队列失败: $($response.message)"
            return $null
        }
    }
    catch {
        Write-Error "❌ 死信队列检查请求失败: $($_.Exception.Message)"
        return $null
    }
}

# 获取事件历史
function Get-EventHistory {
    param(
        [string]$BaseUrl,
        [hashtable]$Headers,
        [string]$AggregateId = "",
        [string]$EventType = "",
        [int]$Skip = 0,
        [int]$Take = 100
    )
    
    try {
        $uri = "${BaseUrl}/api/v2/eventbus/history?skip=${Skip}`&take=${Take}"
        if ($AggregateId) { $uri += "`&aggregateId=${AggregateId}" }
        if ($EventType) { $uri += "`&eventType=${EventType}" }
        
        $response = Invoke-RestMethod -Uri $uri -Method GET -Headers $Headers
        return $response
    }
    catch {
        Write-Host "获取事件历史失败: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# 综合测试场景
function Run-ComprehensiveTest {
    Write-Info "🎯 运行综合测试场景..."
    
    $testResults = @{
        PublishedEvents = 0
        SuccessfulPublications = 0
        FailedPublications = 0
        EventTypes = @()
    }
    
    # 测试场景1: 生产批次创建事件
    Write-Info "测试场景1: 生产批次创建事件"
    $batchEvent = Publish-TestEvent "productionbatchcreated" @{
        batchNumber = "TEST_BATCH_$(Get-Date -Format 'yyyyMMddHHmmss')"
        productModel = "BT-18650-3000mAh"
        plannedQuantity = 1000
        workshopCode = "WS001"
    }
    
    if ($batchEvent) {
        $testResults.SuccessfulPublications++
        $testResults.EventTypes += "ProductionBatchCreated"
    } else {
        $testResults.FailedPublications++
    }
    $testResults.PublishedEvents++
    
    Start-Sleep -Seconds 2
    
    # 测试场景2: 设备故障事件
    Write-Info "测试场景2: 设备故障事件"
    $equipmentEvent = Publish-TestEvent "equipmentfailure" @{
        equipmentCode = "EQ001"
        faultType = "TemperatureOverheat"
        description = "设备温度过高，需要立即检查"
        severity = "High"
    }
    
    if ($equipmentEvent) {
        $testResults.SuccessfulPublications++
        $testResults.EventTypes += "EquipmentFailure"
    } else {
        $testResults.FailedPublications++
    }
    $testResults.PublishedEvents++
    
    Start-Sleep -Seconds 2
    
    # 测试场景3: 质量检测事件
    Write-Info "测试场景3: 质量检测事件"
    $qualityEvent = Publish-TestEvent "qualityinspection" @{
        productBarcode = "PROD_$(Get-Date -Format 'HHmmss')"
        batchNumber = "TEST_BATCH_001"
        isQualified = $true
        testResults = @{
            voltage = 3.7
            capacity = 3000
            resistance = 0.025
        }
    }
    
    if ($qualityEvent) {
        $testResults.SuccessfulPublications++
        $testResults.EventTypes += "QualityInspection"
    } else {
        $testResults.FailedPublications++
    }
    $testResults.PublishedEvents++
    
    # 等待事件处理
    Write-Info "⏳ 等待事件处理..."
    Start-Sleep -Seconds 5
    
    return $testResults
}

# 性能测试
function Run-PerformanceTest {
    param([int]$EventCount = 50)
    
    Write-Info "⚡ 运行性能测试: 发布 $EventCount 个事件"
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $successCount = 0
    $failureCount = 0
    
    for ($i = 1; $i -le $EventCount; $i++) {
        $eventType = @("productionbatchcreated", "equipmentfailure", "qualityinspection")[(Get-Random -Minimum 0 -Maximum 3)]
        
        $result = Publish-TestEvent $eventType @{
            testIndex = $i
            timestamp = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
        }
        
        if ($result) {
            $successCount++
        } else {
            $failureCount++
        }
        
        if ($i % 10 -eq 0) {
            Write-Info "进度: $i/$EventCount"
        }
    }
    
    $stopwatch.Stop()
    $totalTime = $stopwatch.ElapsedMilliseconds
    $eventsPerSecond = [math]::Round(($EventCount * 1000) / $totalTime, 2)
    
    Write-Success "✅ 性能测试完成"
    Write-Info "总事件数: $EventCount"
    Write-Info "成功发布: $successCount"
    Write-Info "发布失败: $failureCount"
    Write-Info "总耗时: ${totalTime}ms"
    Write-Info "事件/秒: $eventsPerSecond"
    
    return @{
        TotalEvents = $EventCount
        SuccessCount = $successCount
        FailureCount = $failureCount
        TotalTimeMs = $totalTime
        EventsPerSecond = $eventsPerSecond
    }
}

# 主测试流程
function Start-EventDrivenArchitectureTest {
    Write-Info "=" * 60
    Write-Info "锂电池包装MES系统 - 事件驱动架构测试"
    Write-Info "=" * 60
    
    # 认证
    if (-not (Get-AuthToken)) {
        Write-Error "❌ 认证失败，测试终止"
        return
    }
    
    # 1. 健康检查
    Write-Info "`n1️⃣ 系统健康检查"
    $isHealthy = Test-EventBusHealth
    if (-not $isHealthy) {
        Write-Warning "⚠️ 系统健康状态异常，继续测试..."
    }
    
    # 2. 基础功能测试
    Write-Info "`n2️⃣ 基础功能测试"
    $comprehensiveResults = Run-ComprehensiveTest
    
    # 3. 事件统计检查
    Write-Info "`n3️⃣ 事件统计检查"
    $statistics = Get-EventStatistics
    
    # 4. 事件历史检查
    Write-Info "`n4️⃣ 事件历史检查"
    $history = Get-EventHistory -BaseUrl $BaseUrl -Headers $Global:Headers -Take 5
    
    # 5. 死信队列检查
    Write-Info "`n5️⃣ 死信队列检查"
    $deadLetterQueue = Get-DeadLetterQueue
    
    # 6. 性能测试（可选）
    if ($Detailed) {
        Write-Info "`n6️⃣ 性能测试"
        $performanceResults = Run-PerformanceTest -EventCount 20
    }
    
    # 生成测试报告
    Write-Info "`n📋 测试报告"
    Write-Info "=" * 40
    Write-Success "✅ 基础功能测试:"
    Write-Info "  发布事件数: $($comprehensiveResults.PublishedEvents)"
    Write-Info "  成功发布: $($comprehensiveResults.SuccessfulPublications)"
    Write-Info "  失败发布: $($comprehensiveResults.FailedPublications)"
    Write-Info "  事件类型: $($comprehensiveResults.EventTypes -join ', ')"
    
    if ($statistics) {
        Write-Success "✅ 事件统计:"
        Write-Info "  总事件数: $($statistics.totalEvents)"
        Write-Info "  时间范围: 最近24小时"
    }
    
    if ($history) {
        Write-Success "✅ 事件历史:"
        Write-Info "  历史事件数: $($history.totalCount)"
    }
    
    if ($deadLetterQueue) {
        if ($deadLetterQueue.totalEvents -gt 0) {
            Write-Warning "⚠️ 死信队列:"
            Write-Warning "  队列数量: $($deadLetterQueue.totalKeys)"
            Write-Warning "  失败事件数: $($deadLetterQueue.totalEvents)"
        } else {
            Write-Success "✅ 死信队列: 无失败事件"
        }
    }
    
    if ($performanceResults) {
        Write-Success "✅ 性能测试:"
        Write-Info "  事件/秒: $($performanceResults.EventsPerSecond)"
        Write-Info "  成功率: $([math]::Round(($performanceResults.SuccessCount / $performanceResults.TotalEvents) * 100, 2))%"
    }
    
    $overallSuccess = $comprehensiveResults.FailedPublications -eq 0 -and $isHealthy
    
    Write-Info "`n" + "=" * 60
    if ($overallSuccess) {
        Write-Success "🎉 事件驱动架构测试完成 - 所有测试通过！"
    } else {
        Write-Warning "⚠️ 事件驱动架构测试完成 - 存在部分问题，请检查日志"
    }
    Write-Info "=" * 60
    
    return $overallSuccess
}

# 运行测试
try {
    $result = Start-EventDrivenArchitectureTest
    
    if ($result) {
        exit 0
    } else {
        exit 1
    }
}
catch {
    Write-Error "❌ 测试执行失败: $($_.Exception.Message)"
    exit 1
} 