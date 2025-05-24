# ========================================
# Battery Packing MES System - Database Initialization Guide (for GUI tools)
# ========================================

param(
    [string]$MySqlHost = "localhost",
    [int]$MySqlPort = 3371,
    [string]$MySqlUser = "root",
    [string]$MySqlPassword = "root",
    [string]$DatabaseName = "BatteryPackingMES_Dev"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Battery Packing MES System - Database Init Guide" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

Write-Host "Detected GUI database tool (like Navicat)." -ForegroundColor Yellow
Write-Host "Please follow these steps to initialize the database manually:" -ForegroundColor White
Write-Host ""

# Check if SQL script file exists
$sqlScriptPath = "scripts/init-database.sql"
if (Test-Path $sqlScriptPath) {
    Write-Host "Found database script: $sqlScriptPath" -ForegroundColor Green
    
    # Display connection information
    Write-Host ""
    Write-Host "Database Connection Information:" -ForegroundColor Cyan
    Write-Host "  Host: $MySqlHost" -ForegroundColor White
    Write-Host "  Port: $MySqlPort" -ForegroundColor White
    Write-Host "  Username: $MySqlUser" -ForegroundColor White
    Write-Host "  Password: $MySqlPassword" -ForegroundColor White
    Write-Host "  Database Name: $DatabaseName" -ForegroundColor White
    Write-Host ""
    
    # Display operation steps
    Write-Host "Steps to execute in Navicat:" -ForegroundColor Yellow
    Write-Host "1. Open Navicat Premium 17" -ForegroundColor White
    Write-Host "2. Create a new MySQL connection:" -ForegroundColor White
    Write-Host "   - Connection Name: Battery_MES_Dev" -ForegroundColor Gray
    Write-Host "   - Host: $MySqlHost" -ForegroundColor Gray
    Write-Host "   - Port: $MySqlPort" -ForegroundColor Gray
    Write-Host "   - User Name: $MySqlUser" -ForegroundColor Gray
    Write-Host "   - Password: $MySqlPassword" -ForegroundColor Gray
    Write-Host "3. Test connection and save" -ForegroundColor White
    Write-Host "4. Open Query Editor" -ForegroundColor White
    Write-Host "5. Copy and execute the SQL script content" -ForegroundColor White
    Write-Host ""
    
    # Read and display first few lines of SQL script
    try {
        $sqlContent = Get-Content $sqlScriptPath -TotalCount 10 -ErrorAction Stop
        Write-Host "SQL Script Preview:" -ForegroundColor Cyan
        Write-Host "----------------------------------------" -ForegroundColor Gray
        foreach ($line in $sqlContent) {
            Write-Host $line -ForegroundColor Gray
        }
        Write-Host "..." -ForegroundColor Gray
        Write-Host "----------------------------------------" -ForegroundColor Gray
        Write-Host ""
    } catch {
        Write-Host "Could not read SQL script preview" -ForegroundColor Yellow
    }
    
    Write-Host "6. Full SQL script location: $(Resolve-Path $sqlScriptPath)" -ForegroundColor White
    
    # Ask if user wants to open the script file
    $openScript = Read-Host "Do you want to open the SQL script file with notepad? (y/N)"
    if ($openScript -eq 'y' -or $openScript -eq 'Y') {
        try {
            Start-Process notepad.exe -ArgumentList $sqlScriptPath
            Write-Host "SQL script file opened in notepad" -ForegroundColor Green
        } catch {
            Write-Host "Failed to open notepad" -ForegroundColor Yellow
        }
    }
    
} else {
    Write-Host "SQL script file not found: $sqlScriptPath" -ForegroundColor Red
    Write-Host "Please ensure the script file exists!" -ForegroundColor Yellow
}

Write-Host ""

# Update configuration file function
function Update-ConnectionString {
    param($ServerHost, $Port, $User, $Password, $DatabaseName)
    
    $configFile = "src/BatteryPackingMES.Api/appsettings.Development.json"
    if (Test-Path $configFile) {
        Write-Host "Updating application configuration file..." -ForegroundColor Yellow
        
        $connectionString = "Server=$ServerHost;Port=$Port;Database=$DatabaseName;Uid=$User;Pwd=$Password;charset=utf8mb4;SslMode=none;AllowPublicKeyRetrieval=true;"
        
        try {
            $config = Get-Content $configFile -Raw | ConvertFrom-Json
            if (-not $config.ConnectionStrings) {
                $config | Add-Member -Type NoteProperty -Name ConnectionStrings -Value @{}
            }
            $config.ConnectionStrings.DefaultConnection = $connectionString
            $config.ConnectionStrings.SlaveConnection = $connectionString
            
            $config | ConvertTo-Json -Depth 10 | Set-Content $configFile -Encoding UTF8
            Write-Host "Configuration file updated successfully" -ForegroundColor Green
            Write-Host "   Config file location: $(Resolve-Path $configFile)" -ForegroundColor Gray
        } catch {
            Write-Host "Configuration file update failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "Configuration file not found: $configFile" -ForegroundColor Yellow
    }
}

# Update configuration file
Write-Host "Configuring application connection string:" -ForegroundColor Cyan
Update-ConnectionString -ServerHost $MySqlHost -Port $MySqlPort -User $MySqlUser -Password $MySqlPassword -DatabaseName $DatabaseName

Write-Host ""
Write-Host "Next steps after database initialization:" -ForegroundColor Cyan
Write-Host "1. Confirm successful execution of SQL script in Navicat" -ForegroundColor White
Write-Host "2. Start API service:" -ForegroundColor White
Write-Host "   cd src/BatteryPackingMES.Api" -ForegroundColor Gray
Write-Host "   dotnet run" -ForegroundColor Gray
Write-Host "3. Access Swagger documentation: https://localhost:5001" -ForegroundColor White
Write-Host "4. Login with default account: admin / Admin@123" -ForegroundColor White
Write-Host ""
Write-Host "WARNING: Please change default password in production!" -ForegroundColor Yellow

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "Database initialization guide completed!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green 