# BatteryPackingMES 监控功能测试脚本

Write-Host "=== BatteryPackingMES 监控功能测试 ===" -ForegroundColor Green

# 设置API基础URL
$baseUrl = "https://localhost:5001/api/v2.0/monitoring"

# 忽略SSL证书验证（仅用于开发测试）
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}

Write-Host "`n1. 测试系统健康状态检查..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/health" -Method GET -ContentType "application/json"
    Write-Host "✓ 系统健康状态检查成功" -ForegroundColor Green
    Write-Host "状态: $($healthResponse.Data.status)" -ForegroundColor Cyan
    Write-Host "服务: $($healthResponse.Data.service)" -ForegroundColor Cyan
    Write-Host "环境: $($healthResponse.Data.environment)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ 系统健康状态检查失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n2. 测试记录自定义指标..." -ForegroundColor Yellow
try {
    $metricData = @{
        metricName = "test.metric"
        value = 100.5
        metricType = "gauge"
        tags = @{
            environment = "test"
            service = "battery-mes"
        }
    } | ConvertTo-Json

    $metricResponse = Invoke-RestMethod -Uri "$baseUrl/metrics" -Method POST -Body $metricData -ContentType "application/json"
    Write-Host "✓ 记录自定义指标成功" -ForegroundColor Green
} catch {
    Write-Host "✗ 记录自定义指标失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n3. 测试获取指标数据..." -ForegroundColor Yellow
try {
    $startTime = (Get-Date).AddDays(-1).ToString("yyyy-MM-ddTHH:mm:ss")
    $endTime = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss")
    $metricsUrl = "$baseUrl/metrics?metricName=test.metric&startTime=$startTime&endTime=$endTime"
    
    $metricsResponse = Invoke-RestMethod -Uri $metricsUrl -Method GET -ContentType "application/json"
    Write-Host "✓ 获取指标数据成功" -ForegroundColor Green
    Write-Host "指标数量: $($metricsResponse.Data.Count)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ 获取指标数据失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n4. 测试数据质量报告..." -ForegroundColor Yellow
try {
    $qualityResponse = Invoke-RestMethod -Uri "$baseUrl/data-quality/report" -Method GET -ContentType "application/json"
    Write-Host "✓ 获取数据质量报告成功" -ForegroundColor Green
} catch {
    Write-Host "✗ 获取数据质量报告失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n5. 测试审计日志查询..." -ForegroundColor Yellow
try {
    $auditResponse = Invoke-RestMethod -Uri "$baseUrl/audit/logs?pageSize=10" -Method GET -ContentType "application/json"
    Write-Host "✓ 获取审计日志成功" -ForegroundColor Green
    Write-Host "日志数量: $($auditResponse.Data.Count)" -ForegroundColor Cyan
} catch {
    Write-Host "✗ 获取审计日志失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n6. 测试审计统计报告..." -ForegroundColor Yellow
try {
    $auditReportResponse = Invoke-RestMethod -Uri "$baseUrl/audit/report" -Method GET -ContentType "application/json"
    Write-Host "✓ 获取审计统计报告成功" -ForegroundColor Green
} catch {
    Write-Host "✗ 获取审计统计报告失败: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== 测试完成 ===" -ForegroundColor Green
Write-Host "请检查上述测试结果，确保所有功能正常工作。" -ForegroundColor White 