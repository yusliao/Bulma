# Test Snowflake ID Generation
# Create a process to verify ID generation

$baseUrl = "https://localhost:60162"
$processEndpoint = "$baseUrl/api/v1/Process"

# Ignore SSL certificate validation (dev environment)
[System.Net.ServicePointManager]::ServerCertificateValidationCallback = {$true}
add-type @"
    using System.Net;
    using System.Security.Cryptography.X509Certificates;
    public class TrustAllCertsPolicy : ICertificatePolicy {
        public bool CheckValidationResult(
            ServicePoint svcPoint, X509Certificate certificate,
            WebRequest request, int certificateProblem) {
            return true;
        }
    }
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy

Write-Host "=== Testing Snowflake ID Generation ===" -ForegroundColor Green

try {
    # Test data
    $testProcess = @{
        ProcessCode = "TEST" + (Get-Date -Format "HHmmss")
        ProcessName = "Test Process"
        ProcessType = 0
        StandardTime = 30
        SortOrder = 1
        IsEnabled = $true
        Description = "Snowflake ID test process"
    }
    
    $jsonBody = $testProcess | ConvertTo-Json -Depth 10
    Write-Host "Sending test data: $jsonBody" -ForegroundColor Yellow
    
    # Send POST request to create process
    $response = Invoke-RestMethod -Uri $processEndpoint -Method Post -Body $jsonBody -ContentType "application/json" -ErrorAction Stop
    
    Write-Host "API Response:" -ForegroundColor Cyan
    $response | ConvertTo-Json -Depth 10 | Write-Host
    
    if ($response.Success -eq $true -and $response.Data) {
        $generatedId = $response.Data
        Write-Host "✅ Snowflake ID generation successful!" -ForegroundColor Green
        Write-Host "   Generated ID: $generatedId" -ForegroundColor Green
        Write-Host "   ID Type: $(($generatedId).GetType().Name)" -ForegroundColor Green
        Write-Host "   ID Length: $($generatedId.ToString().Length) digits" -ForegroundColor Green
        
        # Verify if ID has snowflake characteristics (64-bit long integer)
        if ($generatedId -is [long] -and $generatedId -gt 0) {
            Write-Host "✅ ID format validation passed - matches snowflake characteristics" -ForegroundColor Green
        } else {
            Write-Host "❌ ID format abnormal" -ForegroundColor Red
        }
    } else {
        Write-Host "❌ API call failed" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Error occurred during testing:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "Error response: $responseBody" -ForegroundColor Red
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Green 