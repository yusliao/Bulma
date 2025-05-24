# Redis Features Test Script
$baseUrl = "http://localhost:5000/api/v2"

Write-Host "=== Battery Packing MES Redis Features Test ===" -ForegroundColor Green

# Wait for API to start
Write-Host "Waiting for API to start..." -ForegroundColor Yellow
Start-Sleep -Seconds 10

# Test 1: Production Daily Report
Write-Host "`n1. Testing Reports - Production Daily" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/reports/production-daily" -Method GET
    Write-Host "Success: Production Daily Report" -ForegroundColor Green
    Write-Host "  Report Date: $($response.data.reportDate)" -ForegroundColor Gray
    Write-Host "  Total Production: $($response.data.totalProduction)" -ForegroundColor Gray
    Write-Host "  Quality Rate: $($response.data.qualityRate)%" -ForegroundColor Gray
} catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 2: Quality Statistics Report
Write-Host "`n2. Testing Reports - Quality Statistics" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/reports/quality-statistics" -Method GET
    Write-Host "Success: Quality Statistics Report" -ForegroundColor Green
    Write-Host "  Total Tests: $($response.data.totalTests)" -ForegroundColor Gray
    Write-Host "  Passed Tests: $($response.data.passedTests)" -ForegroundColor Gray
} catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 3: Equipment Efficiency Report
Write-Host "`n3. Testing Reports - Equipment Efficiency" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/reports/equipment-efficiency" -Method GET
    Write-Host "Success: Equipment Efficiency Report" -ForegroundColor Green
    Write-Host "  Average Efficiency: $($response.data.averageEfficiency)%" -ForegroundColor Gray
} catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 4: Message Queue - Production Line Status
Write-Host "`n4. Testing Message Queue - Production Line Status" -ForegroundColor Cyan
try {
    $productionMessage = @{
        LineId = "LINE001"
        Status = "Running"
        CurrentProduct = "BATTERY_PACK_001"
        Efficiency = 85.5
        Timestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
    }
    
    $response = Invoke-RestMethod -Uri "$baseUrl/messagequeue/production-line-status" -Method POST -Body ($productionMessage | ConvertTo-Json) -ContentType "application/json"
    Write-Host "Success: Production Line Status Message Published" -ForegroundColor Green
} catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 5: Message Queue - Quality Check
Write-Host "`n5. Testing Message Queue - Quality Check" -ForegroundColor Cyan
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
    Write-Host "Success: Quality Check Message Published" -ForegroundColor Green
} catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Test 6: Message History
Write-Host "`n6. Testing Message Queue - Message History" -ForegroundColor Cyan
try {
    $response = Invoke-RestMethod -Uri "$baseUrl/messagequeue/history?messageType=ProductionLineStatus&limit=5" -Method GET
    Write-Host "Success: Message History Retrieved, Count: $($response.data.Count)" -ForegroundColor Green
} catch {
    Write-Host "Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "`n=== Redis Features Test Completed ===" -ForegroundColor Green 