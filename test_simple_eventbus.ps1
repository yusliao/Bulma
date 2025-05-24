#!/usr/bin/env pwsh

# 简化版事件驱动架构测试脚本

param(
    [string]$BaseUrl = "https://localhost:60162",
    [string]$Username = "admin", 
    [string]$Password = "Admin123!"
)

Write-Host "🚀 开始事件驱动架构测试..." -ForegroundColor Cyan
Write-Host "目标URL: $BaseUrl" -ForegroundColor Cyan

# 忽略SSL证书验证
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $null = [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
}

# 全局变量
$Global:Headers = @{ "Content-Type" = "application/json" }

# 认证
try {
    Write-Host "🔐 开始认证..." -ForegroundColor Cyan
    
    $loginData = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/Auth/login" -Method Post -Body $loginData -ContentType "application/json"
    
    if ($loginResponse.success) {
        $Global:Headers["Authorization"] = "Bearer $($loginResponse.data.token)"
        Write-Host "✅ 认证成功" -ForegroundColor Green
    } else {
        Write-Host "❌ 认证失败: $($loginResponse.message)" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "❌ 认证请求失败: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# 测试事件总线健康状态
try {
    Write-Host "🔍 测试事件总线健康状态..." -ForegroundColor Cyan
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/v2/eventbus/health" -Method Get -Headers $Global:Headers
    
    if ($response.success -or $response.Status -eq "Healthy") {
        Write-Host "✅ 事件总线健康" -ForegroundColor Green
    } else {
        Write-Host "⚠️ 事件总线状态异常" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "❌ 健康检查失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 发布测试事件
try {
    Write-Host "📤 发布测试事件..." -ForegroundColor Cyan
    
    $testData = @{
        batchNumber = "TEST_BATCH_$(Get-Date -Format 'yyyyMMddHHmmss')"
        productModel = "BT-18650-3000mAh"
        plannedQuantity = 1000
        workshopCode = "WS001"
    } | ConvertTo-Json

    $eventResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v2/eventbus/publish-test?eventType=productionbatchcreated" -Method Post -Body $testData -Headers $Global:Headers
    
    if ($eventResponse.success) {
        Write-Host "✅ 事件发布成功" -ForegroundColor Green
    } else {
        Write-Host "❌ 事件发布失败: $($eventResponse.message)" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ 事件发布请求失败: $($_.Exception.Message)" -ForegroundColor Red
}

# 获取事件统计
try {
    Write-Host "📊 获取事件统计..." -ForegroundColor Cyan
    
    $statsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v2/eventbus/statistics" -Method Get -Headers $Global:Headers
    
    if ($statsResponse.success) {
        Write-Host "✅ 统计信息获取成功" -ForegroundColor Green
        Write-Host "总事件数: $($statsResponse.data.TotalEvents)" -ForegroundColor Cyan
    } else {
        Write-Host "❌ 获取统计失败: $($statsResponse.message)" -ForegroundColor Red
    }
}
catch {
    Write-Host "❌ 统计请求失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "测试完成！" -ForegroundColor Green 