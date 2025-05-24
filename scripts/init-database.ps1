# ========================================
# Battery Packing MES System - Database Initialization Script (PowerShell)
# ========================================

param(
    [string]$MySqlHost = "localhost",
    [int]$MySqlPort = 3371,
    [string]$MySqlUser = "root",
    [string]$MySqlPassword = "root",
    [string]$DatabaseName = "BatteryPackingMES_Dev",
    [switch]$SkipUserPrompt = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Battery Packing MES System - Database Init" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Check MySQL installation
function Test-MySqlInstallation {
    try {
        $mysqlPath = Get-Command mysql -ErrorAction Stop
        Write-Host "Found MySQL client: $($mysqlPath.Source)" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "MySQL client not found" -ForegroundColor Red
        Write-Host "Please ensure MySQL is installed and added to PATH environment variable" -ForegroundColor Yellow
        return $false
    }
}

# Test database connection
function Test-DatabaseConnection {
    param($ServerHost, $Port, $User, $Password)
    
    Write-Host "Testing database connection..." -ForegroundColor Yellow
    
    try {
        $connectionTest = mysql -h $ServerHost -P $Port -u $User -p$Password -e "SELECT 1" 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Database connection successful" -ForegroundColor Green
            return $true
        } else {
            Write-Host "Database connection failed: $connectionTest" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "Database connection test failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Backup existing database (if exists)
function Backup-ExistingDatabase {
    param($ServerHost, $Port, $User, $Password, $DatabaseName)
    
    Write-Host "Checking for existing database..." -ForegroundColor Yellow
    
    $checkDb = mysql -h $ServerHost -P $Port -u $User -p$Password -e "SHOW DATABASES LIKE '$DatabaseName'" 2>&1
    if ($checkDb -match $DatabaseName) {
        Write-Host "Found existing database: $DatabaseName" -ForegroundColor Yellow
        
        if (-not $SkipUserPrompt) {
            $backup = Read-Host "Do you want to backup existing database? (y/N)"
            if ($backup -eq 'y' -or $backup -eq 'Y') {
                $backupFile = "backup_${DatabaseName}_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
                Write-Host "Backing up to: $backupFile" -ForegroundColor Yellow
                
                mysqldump -h $ServerHost -P $Port -u $User -p$Password $DatabaseName | Out-File -FilePath $backupFile
                if ($LASTEXITCODE -eq 0) {
                    Write-Host "Database backup successful: $backupFile" -ForegroundColor Green
                } else {
                    Write-Host "Database backup failed" -ForegroundColor Red
                    return $false
                }
            }
        }
    }
    return $true
}

# Execute SQL script
function Execute-SqlScript {
    param($ServerHost, $Port, $User, $Password, $ScriptPath)
    
    if (-not (Test-Path $ScriptPath)) {
        Write-Host "SQL script file not found: $ScriptPath" -ForegroundColor Red
        return $false
    }
    
    Write-Host "Executing SQL script: $ScriptPath" -ForegroundColor Yellow
    
    try {
        Get-Content $ScriptPath | mysql -h $ServerHost -P $Port -u $User -p$Password
        if ($LASTEXITCODE -eq 0) {
            Write-Host "SQL script execution successful" -ForegroundColor Green
            return $true
        } else {
            Write-Host "SQL script execution failed, error code: $LASTEXITCODE" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "SQL script execution exception: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Verify database initialization results
function Test-DatabaseInitialization {
    param($ServerHost, $Port, $User, $Password, $DatabaseName)
    
    Write-Host "Verifying database initialization results..." -ForegroundColor Yellow
    
    # Check if database exists
    $checkDb = mysql -h $ServerHost -P $Port -u $User -p$Password -e "USE $DatabaseName; SELECT COUNT(*) as TableCount FROM information_schema.tables WHERE table_schema = '$DatabaseName';" 2>&1
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Database verification successful" -ForegroundColor Green
        
        # Display table statistics
        $tableCount = mysql -h $ServerHost -P $Port -u $User -p$Password -e "USE $DatabaseName; SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = '$DatabaseName';" -N 2>&1
        Write-Host "Created table count: $tableCount" -ForegroundColor Cyan
        
        # Display default user information
        Write-Host "Default admin account:" -ForegroundColor Cyan
        Write-Host "   Username: admin" -ForegroundColor White
        Write-Host "   Password: Admin@123" -ForegroundColor White
        Write-Host "   WARNING: Please change default password in production!" -ForegroundColor Yellow
        
        return $true
    } else {
        Write-Host "Database verification failed" -ForegroundColor Red
        return $false
    }
}

# Update connection string in configuration file
function Update-ConnectionString {
    param($ServerHost, $Port, $User, $Password, $DatabaseName)
    
    $configFile = "src/BatteryPackingMES.Api/appsettings.Development.json"
    if (Test-Path $configFile) {
        Write-Host "Updating configuration file..." -ForegroundColor Yellow
        
        $connectionString = "Server=$ServerHost;Port=$Port;Database=$DatabaseName;Uid=$User;Pwd=$Password;charset=utf8mb4;SslMode=none;AllowPublicKeyRetrieval=true;"
        
        try {
            $config = Get-Content $configFile -Raw | ConvertFrom-Json
            $config.ConnectionStrings.DefaultConnection = $connectionString
            $config.ConnectionStrings.SlaveConnection = $connectionString
            
            $config | ConvertTo-Json -Depth 10 | Set-Content $configFile
            Write-Host "Configuration file updated successfully" -ForegroundColor Green
        } catch {
            Write-Host "Configuration file update failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# Main function
function Main {
    Write-Host "Configuration:" -ForegroundColor White
    Write-Host "  MySQL Host: $MySqlHost" -ForegroundColor Gray
    Write-Host "  MySQL Port: $MySqlPort" -ForegroundColor Gray
    Write-Host "  MySQL User: $MySqlUser" -ForegroundColor Gray
    Write-Host "  Database Name: $DatabaseName" -ForegroundColor Gray
    Write-Host ""
    
    # Check MySQL installation
    if (-not (Test-MySqlInstallation)) {
        exit 1
    }
    
    # Test database connection
    if (-not (Test-DatabaseConnection -ServerHost $MySqlHost -Port $MySqlPort -User $MySqlUser -Password $MySqlPassword)) {
        Write-Host "Please check the following:" -ForegroundColor Yellow
        Write-Host "1. MySQL service is running" -ForegroundColor White
        Write-Host "2. Connection parameters are correct (host, port, username, password)" -ForegroundColor White
        Write-Host "3. Firewall settings allow the connection" -ForegroundColor White
        exit 1
    }
    
    # Confirm execution
    if (-not $SkipUserPrompt) {
        Write-Host "This operation will:" -ForegroundColor Yellow
        Write-Host "  1. Drop existing $DatabaseName database (if exists)" -ForegroundColor White
        Write-Host "  2. Create new database and table structure" -ForegroundColor White
        Write-Host "  3. Insert initial data" -ForegroundColor White
        Write-Host ""
        $confirm = Read-Host "Are you sure you want to continue? (y/N)"
        if ($confirm -ne 'y' -and $confirm -ne 'Y') {
            Write-Host "Operation cancelled" -ForegroundColor Yellow
            exit 0
        }
    }
    
    # Backup existing database
    if (-not (Backup-ExistingDatabase -ServerHost $MySqlHost -Port $MySqlPort -User $MySqlUser -Password $MySqlPassword -DatabaseName $DatabaseName)) {
        exit 1
    }
    
    # Execute SQL script
    $scriptPath = "scripts/init-database.sql"
    if (-not (Execute-SqlScript -ServerHost $MySqlHost -Port $MySqlPort -User $MySqlUser -Password $MySqlPassword -ScriptPath $scriptPath)) {
        exit 1
    }
    
    # Verify initialization results
    if (-not (Test-DatabaseInitialization -ServerHost $MySqlHost -Port $MySqlPort -User $MySqlUser -Password $MySqlPassword -DatabaseName $DatabaseName)) {
        exit 1
    }
    
    # Update configuration file
    Update-ConnectionString -ServerHost $MySqlHost -Port $MySqlPort -User $MySqlUser -Password $MySqlPassword -DatabaseName $DatabaseName
    
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Database initialization completed!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "1. Start API service: cd src/BatteryPackingMES.Api; dotnet run" -ForegroundColor White
    Write-Host "2. Access Swagger documentation: https://localhost:5001" -ForegroundColor White
    Write-Host "3. Login with default account: admin / Admin@123" -ForegroundColor White
}

# Execute main function
Main 