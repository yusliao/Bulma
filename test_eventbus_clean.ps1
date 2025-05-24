param(
    [string]$BaseUrl = "https://localhost:60162",
    [string]$Username = "admin", 
    [string]$Password = "Admin123!"
)

Write-Host "Starting Event-Driven Architecture Test..." -ForegroundColor Cyan
Write-Host "Target URL: $BaseUrl" -ForegroundColor Cyan

# Ignore SSL certificate validation
if ($PSVersionTable.PSVersion.Major -ge 6) {
    $null = [System.Net.ServicePointManager]::ServerCertificateValidationCallback = { $true }
}

# Global variables
$Global:Headers = @{ "Content-Type" = "application/json" }

# Authentication
try {
    Write-Host "Starting authentication..." -ForegroundColor Cyan
    
    $loginData = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json

    $loginResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v1/Auth/login" -Method Post -Body $loginData -ContentType "application/json"
    
    if ($loginResponse.success) {
        $Global:Headers["Authorization"] = "Bearer $($loginResponse.data.token)"
        Write-Host "Authentication successful" -ForegroundColor Green
    } else {
        Write-Host "Authentication failed: $($loginResponse.message)" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Authentication request failed: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Test EventBus health
try {
    Write-Host "Testing EventBus health..." -ForegroundColor Cyan
    
    $response = Invoke-RestMethod -Uri "$BaseUrl/api/v2/eventbus/health" -Method Get -Headers $Global:Headers
    
    if ($response.success -or $response.Status -eq "Healthy") {
        Write-Host "EventBus is healthy" -ForegroundColor Green
    } else {
        Write-Host "EventBus status is abnormal" -ForegroundColor Yellow
    }
}
catch {
    Write-Host "Health check failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Publish test event
try {
    Write-Host "Publishing test event..." -ForegroundColor Cyan
    
    $testData = @{
        batchNumber = "TEST_BATCH_$(Get-Date -Format 'yyyyMMddHHmmss')"
        productModel = "BT-18650-3000mAh"
        plannedQuantity = 1000
        workshopCode = "WS001"
    } | ConvertTo-Json

    $eventResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v2/eventbus/publish-test?eventType=productionbatchcreated" -Method Post -Body $testData -Headers $Global:Headers
    
    if ($eventResponse.success) {
        Write-Host "Event published successfully" -ForegroundColor Green
        Write-Host "Event ID: $($eventResponse.eventId)" -ForegroundColor Cyan
    } else {
        Write-Host "Event publishing failed: $($eventResponse.message)" -ForegroundColor Red
    }
}
catch {
    Write-Host "Event publishing request failed: $($_.Exception.Message)" -ForegroundColor Red
}

# Get event statistics
try {
    Write-Host "Getting event statistics..." -ForegroundColor Cyan
    
    $statsResponse = Invoke-RestMethod -Uri "$BaseUrl/api/v2/eventbus/statistics" -Method Get -Headers $Global:Headers
    
    if ($statsResponse.success) {
        Write-Host "Statistics retrieved successfully" -ForegroundColor Green
        Write-Host "Total Events: $($statsResponse.data.TotalEvents)" -ForegroundColor Cyan
    } else {
        Write-Host "Failed to get statistics: $($statsResponse.message)" -ForegroundColor Red
    }
}
catch {
    Write-Host "Statistics request failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host "Test completed!" -ForegroundColor Green 