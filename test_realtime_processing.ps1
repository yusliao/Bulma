#!/usr/bin/env pwsh

<#
.SYNOPSIS
    测试实时参数处理功能
.DESCRIPTION
    这个脚本用于测试锂电池包装MES系统的实时参数处理功能，包括参数采集、异常检测、预警和聚合等功能。
.PARAMETER BaseUrl
    API基础URL，默认为 https://localhost:5001
.PARAMETER ProcessId
    测试的工序ID，默认为 1
.PARAMETER ParameterName
    测试的参数名称，默认为 "温度"
.PARAMETER BatchNumber
    批次号，默认为 "TEST_BATCH_001"
.PARAMETER TestDuration
    测试持续时间（秒），默认为 300 秒（5分钟）
.PARAMETER DataInterval
    数据发送间隔（秒），默认为 5 秒
#>

param(
    [string]$BaseUrl = "https://localhost:5001",
    [long]$ProcessId = 1,
    [string]$ParameterName = "温度",
    [string]$BatchNumber = "TEST_BATCH_001",
    [int]$TestDuration = 300,
    [int]$DataInterval = 5
)

# 忽略SSL证书错误（仅用于开发测试）
if (-not ("TrustAllCertsPolicy" -as [type])) {
    $trustAllCertsPolicy = @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAllCertsPolicy : ICertificatePolicy {
    public bool CheckValidationResult(
        ServicePoint srvPoint, X509Certificate certificate,
        WebRequest request, int certificateProblem) {
        return true;
    }
}
"@
    Add-Type -TypeDefinition $trustAllCertsPolicy
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
}

# 全局变量
$global:AuthToken = $null
$global:Headers = @{ "Content-Type" = "application/json" }

# 登录获取JWT令牌
function Get-AuthToken {
    param([string]$BaseUrl)
    
    Write-Host "正在获取认证令牌..." -ForegroundColor Yellow
    
    $loginData = @{
        username = "admin"
        password = "Admin@123"
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v1.0/auth/login" -Method Post -Body $loginData -ContentType "application/json"
        
        if ($response.success -and $response.data.token) {
            $global:AuthToken = $response.data.token
            $global:Headers["Authorization"] = "Bearer $($global:AuthToken)"
            Write-Host "认证成功" -ForegroundColor Green
            return $true
        } else {
            Write-Host "登录失败: $($response.message)" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "登录失败: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# 生成测试参数数据
function Generate-TestParameterData {
    param(
        [long]$ProcessId,
        [string]$ParameterName,
        [string]$BatchNumber,
        [bool]$GenerateAnomaly = $false
    )
    
    # 基础值：温度为22.5°C，压力为2.3bar等
    $baseValues = @{
        "温度" = 22.5
        "压力" = 2.3
        "湿度" = 45.0
        "电压" = 3.7
        "电流" = 1.2
    }
    
    $baseValue = $baseValues[$ParameterName]
    if ($null -eq $baseValue) {
        $baseValue = 25.0
    }
    
    # 正常范围内的随机变化（±5%）
    $normalVariation = (Get-Random -Minimum -0.05 -Maximum 0.05) * $baseValue
    $value = $baseValue + $normalVariation
    
    # 如果需要生成异常数据
    if ($GenerateAnomaly) {
        $anomalyTypes = @("high", "low", "spike")
        $anomalyType = Get-Random -InputObject $anomalyTypes
        
        switch ($anomalyType) {
            "high" { $value = $baseValue * (1 + (Get-Random -Minimum 0.15 -Maximum 0.3)) }
            "low" { $value = $baseValue * (1 - (Get-Random -Minimum 0.15 -Maximum 0.3)) }
            "spike" { $value = $baseValue * (1 + (Get-Random -Minimum 0.4 -Maximum 0.8)) }
        }
    }
    
    # 设置上下限
    $upperLimit = $baseValue * 1.1
    $lowerLimit = $baseValue * 0.9
    
    return @{
        processId = $ProcessId
        parameterName = $ParameterName
        parameterValue = [math]::Round($value, 2)
        parameterUnit = if ($ParameterName -eq "温度") { "℃" } elseif ($ParameterName -eq "压力") { "bar" } elseif ($ParameterName -eq "湿度") { "%" } elseif ($ParameterName -eq "电压") { "V" } elseif ($ParameterName -eq "电流") { "A" } else { "unit" }
        upperLimit = [math]::Round($upperLimit, 2)
        lowerLimit = [math]::Round($lowerLimit, 2)
        batchNumber = $BatchNumber
        recordTime = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        equipmentId = Get-Random -Minimum 1001 -Maximum 1010
    }
}

# 发送参数数据
function Send-ParameterData {
    param(
        [string]$BaseUrl,
        [array]$Parameters
    )
    
    $requestData = @{
        parameters = $Parameters
    } | ConvertTo-Json -Depth 3
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v2.0/process-parameters/batch-record" -Method Post -Body $requestData -Headers $global:Headers
        
        if ($response.success) {
            Write-Host "✓ 成功发送 $($Parameters.Length) 条参数数据" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ 发送参数数据失败: $($response.message)" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "✗ 发送参数数据异常: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# 获取实时参数数据
function Get-RealTimeParameters {
    param(
        [string]$BaseUrl,
        [long]$ProcessId
    )
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v2.0/realtime/parameters/$ProcessId" -Method Get -Headers $global:Headers
        
        if ($response.success) {
            return $response.data
        } else {
            Write-Host "获取实时参数失败: $($response.message)" -ForegroundColor Yellow
            return $null
        }
    } catch {
        Write-Host "获取实时参数异常: $($_.Exception.Message)" -ForegroundColor Yellow
        return $null
    }
}

# 获取聚合数据
function Get-AggregatedData {
    param(
        [string]$BaseUrl,
        [long]$ProcessId,
        [string]$ParameterName
    )
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v2.0/realtime/aggregated/$ProcessId/$ParameterName" -Method Get -Headers $global:Headers
        
        if ($response.success) {
            return $response.data
        } else {
            Write-Host "获取聚合数据失败: $($response.message)" -ForegroundColor Yellow
            return $null
        }
    } catch {
        Write-Host "获取聚合数据异常: $($_.Exception.Message)" -ForegroundColor Yellow
        return $null
    }
}

# 获取系统状态
function Get-SystemStatus {
    param([string]$BaseUrl)
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v2.0/realtime/system-status" -Method Get -Headers $global:Headers
        
        if ($response.success) {
            return $response.data
        } else {
            Write-Host "获取系统状态失败: $($response.message)" -ForegroundColor Yellow
            return $null
        }
    } catch {
        Write-Host "获取系统状态异常: $($_.Exception.Message)" -ForegroundColor Yellow
        return $null
    }
}

# 触发聚合
function Trigger-Aggregation {
    param(
        [string]$BaseUrl,
        [long]$ProcessId,
        [string]$ParameterName
    )
    
    try {
        $response = Invoke-RestMethod -Uri "$BaseUrl/api/v2.0/realtime/trigger-aggregation?processId=$ProcessId&parameterName=$ParameterName" -Method Post -Headers $global:Headers
        
        if ($response.success) {
            Write-Host "✓ 成功触发聚合: $ParameterName" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ 触发聚合失败: $($response.message)" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "✗ 触发聚合异常: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# 显示测试统计
function Show-TestStatistics {
    param(
        [int]$TotalSent,
        [int]$SuccessCount,
        [int]$FailureCount,
        [int]$AnomalyCount
    )
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "测试统计信息" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "总发送数据量: $TotalSent" -ForegroundColor White
    Write-Host "成功发送: $SuccessCount" -ForegroundColor Green
    Write-Host "发送失败: $FailureCount" -ForegroundColor Red
    Write-Host "异常数据: $AnomalyCount" -ForegroundColor Yellow
    Write-Host "成功率: $([math]::Round($SuccessCount / $TotalSent * 100, 2))%" -ForegroundColor White
    Write-Host ""
}

# 主测试函数
function Start-RealTimeProcessingTest {
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "实时参数处理功能测试" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "基础URL: $BaseUrl" -ForegroundColor Gray
    Write-Host "工序ID: $ProcessId" -ForegroundColor Gray
    Write-Host "参数名称: $ParameterName" -ForegroundColor Gray
    Write-Host "批次号: $BatchNumber" -ForegroundColor Gray
    Write-Host "测试时长: $TestDuration 秒" -ForegroundColor Gray
    Write-Host "数据间隔: $DataInterval 秒" -ForegroundColor Gray
    Write-Host ""
    
    # 获取认证令牌
    if (-not (Get-AuthToken -BaseUrl $BaseUrl)) {
        Write-Host "无法获取认证令牌，测试终止" -ForegroundColor Red
        return
    }
    
    # 测试统计变量
    $totalSent = 0
    $successCount = 0
    $failureCount = 0
    $anomalyCount = 0
    
    # 获取初始系统状态
    Write-Host "获取初始系统状态..." -ForegroundColor Yellow
    $initialStatus = Get-SystemStatus -BaseUrl $BaseUrl
    if ($initialStatus) {
        Write-Host "系统状态: $($initialStatus.processingStatus)" -ForegroundColor Green
        Write-Host "消息队列: $($initialStatus.messageQueueStatus)" -ForegroundColor Green
        Write-Host "缓存状态: $($initialStatus.cacheStatus)" -ForegroundColor Green
    }
    
    Write-Host ""
    Write-Host "开始发送测试数据..." -ForegroundColor Yellow
    
    $startTime = Get-Date
    $endTime = $startTime.AddSeconds($TestDuration)
    
    while ((Get-Date) -lt $endTime) {
        # 生成测试数据
        $generateAnomaly = (Get-Random -Minimum 1 -Maximum 100) -le 15  # 15%概率生成异常数据
        $paramData = Generate-TestParameterData -ProcessId $ProcessId -ParameterName $ParameterName -BatchNumber $BatchNumber -GenerateAnomaly $generateAnomaly
        
        if ($generateAnomaly) {
            $anomalyCount++
            Write-Host "🔥 生成异常数据: $($paramData.parameterValue) $($paramData.parameterUnit)" -ForegroundColor Red
        } else {
            Write-Host "📊 生成正常数据: $($paramData.parameterValue) $($paramData.parameterUnit)" -ForegroundColor White
        }
        
        # 发送数据
        $success = Send-ParameterData -BaseUrl $BaseUrl -Parameters @($paramData)
        $totalSent++
        
        if ($success) {
            $successCount++
        } else {
            $failureCount++
        }
        
        # 每30秒获取一次实时数据和聚合数据
        if ($totalSent % 6 -eq 0) {  # 30秒 / 5秒间隔 = 6次
            Write-Host ""
            Write-Host "--- 获取实时监控数据 ---" -ForegroundColor Cyan
            
            $realtimeData = Get-RealTimeParameters -BaseUrl $BaseUrl -ProcessId $ProcessId
            if ($realtimeData) {
                Write-Host "工序状态: $($realtimeData.status)" -ForegroundColor $(if ($realtimeData.status -eq "正常") { "Green" } else { "Red" })
                Write-Host "参数数量: $($realtimeData.parameters.Length)" -ForegroundColor White
                Write-Host "最后更新: $($realtimeData.lastUpdateTime)" -ForegroundColor Gray
                
                foreach ($param in $realtimeData.parameters) {
                    $statusColor = if ($param.isQualified) { "Green" } else { "Red" }
                    Write-Host "  $($param.parameterName): $($param.currentValue) $($param.unit) [$($param.status)]" -ForegroundColor $statusColor
                }
            }
            
            # 获取聚合数据
            $aggregatedData = Get-AggregatedData -BaseUrl $BaseUrl -ProcessId $ProcessId -ParameterName $ParameterName
            if ($aggregatedData) {
                Write-Host "--- 聚合数据 ---" -ForegroundColor Cyan
                Write-Host "数据量: $($aggregatedData.dataCount)" -ForegroundColor White
                Write-Host "平均值: $($aggregatedData.averageValue)" -ForegroundColor White
                Write-Host "合格率: $($aggregatedData.qualificationRate)%" -ForegroundColor White
                Write-Host "标准差: $($aggregatedData.standardDeviation)" -ForegroundColor White
            }
            
            Write-Host ""
        }
        
        # 等待下一次发送
        Start-Sleep -Seconds $DataInterval
    }
    
    Write-Host ""
    Write-Host "测试数据发送完成，等待处理..." -ForegroundColor Yellow
    Start-Sleep -Seconds 10
    
    # 手动触发聚合
    Write-Host "手动触发聚合..." -ForegroundColor Yellow
    Trigger-Aggregation -BaseUrl $BaseUrl -ProcessId $ProcessId -ParameterName $ParameterName
    Start-Sleep -Seconds 5
    
    # 获取最终状态
    Write-Host "获取最终状态..." -ForegroundColor Yellow
    $finalStatus = Get-SystemStatus -BaseUrl $BaseUrl
    if ($finalStatus) {
        Write-Host "最终系统状态:" -ForegroundColor Cyan
        Write-Host "  处理状态: $($finalStatus.processingStatus)" -ForegroundColor White
        Write-Host "  消息队列: $($finalStatus.messageQueueStatus)" -ForegroundColor White
        Write-Host "  缓存状态: $($finalStatus.cacheStatus)" -ForegroundColor White
    }
    
    # 显示统计信息
    Show-TestStatistics -TotalSent $totalSent -SuccessCount $successCount -FailureCount $failureCount -AnomalyCount $anomalyCount
    
    Write-Host "实时参数处理功能测试完成！" -ForegroundColor Green
}

# 执行测试
try {
    Start-RealTimeProcessingTest
} catch {
    Write-Host "测试执行过程中发生错误: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host $_.ScriptStackTrace -ForegroundColor Red
} 