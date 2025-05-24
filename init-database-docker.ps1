# ========================================
# Battery Packing MES System - Database Initialization Script (Docker)
# ========================================

param(
    [string]$ContainerName = "mysql-container",
    [string]$MySqlHost = "localhost",
    [int]$MySqlPort = 3371,
    [string]$MySqlUser = "root",
    [string]$MySqlPassword = "root",
    [string]$DatabaseName = "BatteryPackingMES_Dev",
    [switch]$SkipUserPrompt = $false
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Battery Packing MES System - Docker MySQL Init" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Check if Docker is running
function Test-DockerRunning {
    try {
        docker version | Out-Null
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Docker is running" -ForegroundColor Green
            return $true
        } else {
            Write-Host "Docker is not responding" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "Docker is not available: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Find MySQL container
function Get-MySqlContainer {
    try {
        # First try to find running MySQL containers
        $runningContainers = docker ps --filter "ancestor=mysql:latest" --format "{{.ID}} {{.Names}} {{.Status}}"
        
        if ($runningContainers) {
            $containerInfo = $runningContainers[0] -split ' '
            Write-Host "Found running MySQL container" -ForegroundColor Green
            return @{
                ID = $containerInfo[0]
                Name = if ($containerInfo[1]) { $containerInfo[1] } else { $containerInfo[0] }
                Status = "Up"
            }
        }
        
        # If no running containers, try to find stopped ones
        $stoppedContainers = docker ps -a --filter "ancestor=mysql:latest" --format "{{.ID}} {{.Names}} {{.Status}}"
        
        if ($stoppedContainers) {
            $containerInfo = $stoppedContainers[0] -split ' '
            Write-Host "Found stopped MySQL container" -ForegroundColor Yellow
            return @{
                ID = $containerInfo[0]
                Name = if ($containerInfo[1]) { $containerInfo[1] } else { $containerInfo[0] }
                Status = $containerInfo[2]
            }
        }
        
        # Try to find by port mapping
        $portContainers = docker ps -a --filter "publish=3371" --format "{{.ID}} {{.Names}} {{.Status}}"
        if ($portContainers) {
            $containerInfo = $portContainers[0] -split ' '
            Write-Host "Found MySQL container by port mapping" -ForegroundColor Green
            return @{
                ID = $containerInfo[0]
                Name = if ($containerInfo[1]) { $containerInfo[1] } else { $containerInfo[0] }
                Status = if ($containerInfo[2] -like "*Up*") { "Up" } else { "Stopped" }
            }
        }
        
        return $null
    } catch {
        Write-Host "Error finding MySQL container: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# Start MySQL container if not running
function Start-MySqlContainer {
    param($ContainerInfo)
    
    if ($ContainerInfo.Status -like "*Up*") {
        Write-Host "MySQL container is already running: $($ContainerInfo.Name)" -ForegroundColor Green
        return $true
    }
    
    Write-Host "Starting MySQL container: $($ContainerInfo.Name)" -ForegroundColor Yellow
    try {
        docker start $ContainerInfo.ID
        if ($LASTEXITCODE -eq 0) {
            Write-Host "MySQL container started successfully" -ForegroundColor Green
            Start-Sleep -Seconds 5  # Wait for MySQL to be ready
            return $true
        } else {
            Write-Host "Failed to start MySQL container" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "Error starting container: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Create new MySQL container if not exists
function New-MySqlContainer {
    Write-Host "Creating new MySQL container..." -ForegroundColor Yellow
    
    try {
        $dockerCmd = @(
            "docker", "run",
            "--name", $ContainerName,
            "-p", "${MySqlPort}:3306",
            "-e", "MYSQL_ROOT_PASSWORD=$MySqlPassword",
            "-v", "mysql-data:/var/lib/mysql",
            "--restart=always",
            "-d",
            "mysql:latest"
        )
        
        Write-Host "Executing: $($dockerCmd -join ' ')" -ForegroundColor Gray
        & $dockerCmd[0] $dockerCmd[1..($dockerCmd.Length-1)]
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "MySQL container created successfully" -ForegroundColor Green
            Write-Host "Waiting for MySQL to be ready..." -ForegroundColor Yellow
            Start-Sleep -Seconds 30  # Wait for MySQL to initialize
            return $true
        } else {
            Write-Host "Failed to create MySQL container" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "Error creating container: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Test database connection using docker exec
function Test-DatabaseConnection {
    param($ContainerInfo)
    
    Write-Host "Testing database connection..." -ForegroundColor Yellow
    
    try {
        $testCmd = "mysql -u$MySqlUser -p$MySqlPassword -e 'SELECT 1'"
        docker exec $ContainerInfo.ID sh -c $testCmd 2>&1 | Out-Null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Database connection successful" -ForegroundColor Green
            return $true
        } else {
            Write-Host "Database connection failed" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "Database connection test failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Execute SQL script using docker exec
function Execute-SqlScript {
    param($ContainerInfo, $ScriptPath)
    
    if (-not (Test-Path $ScriptPath)) {
        Write-Host "SQL script file not found: $ScriptPath" -ForegroundColor Red
        return $false
    }
    
    Write-Host "Executing SQL script: $ScriptPath" -ForegroundColor Yellow
    
    try {
        # Copy SQL script to container
        docker cp $ScriptPath "${ContainerInfo.ID}:/tmp/init-database.sql"
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Failed to copy SQL script to container" -ForegroundColor Red
            return $false
        }
        
        # Execute SQL script
        $execCmd = "mysql -u$MySqlUser -p$MySqlPassword < /tmp/init-database.sql"
        docker exec $ContainerInfo.ID sh -c $execCmd
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "SQL script execution successful" -ForegroundColor Green
            
            # Cleanup
            docker exec $ContainerInfo.ID rm /tmp/init-database.sql
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

# Verify database initialization
function Test-DatabaseInitialization {
    param($ContainerInfo)
    
    Write-Host "Verifying database initialization results..." -ForegroundColor Yellow
    
    try {
        $verifyCmd = "mysql -u$MySqlUser -p$MySqlPassword -e 'USE $DatabaseName; SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = `'$DatabaseName`';'"
        $result = docker exec $ContainerInfo.ID sh -c $verifyCmd 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Database verification successful" -ForegroundColor Green
            
            # Get table count
            $tableCountCmd = "mysql -u$MySqlUser -p$MySqlPassword -e 'USE $DatabaseName; SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = `'$DatabaseName`';' -N"
            $tableCount = docker exec $ContainerInfo.ID sh -c $tableCountCmd 2>&1
            
            Write-Host "Created table count: $tableCount" -ForegroundColor Cyan
            Write-Host "Default admin account:" -ForegroundColor Cyan
            Write-Host "   Username: admin" -ForegroundColor White
            Write-Host "   Password: Admin@123" -ForegroundColor White
            Write-Host "   WARNING: Please change default password in production!" -ForegroundColor Yellow
            
            return $true
        } else {
            Write-Host "Database verification failed" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "Database verification exception: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Update configuration file
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
        } catch {
            Write-Host "Configuration file update failed: $($_.Exception.Message)" -ForegroundColor Red
        }
    } else {
        Write-Host "Configuration file not found: $configFile" -ForegroundColor Yellow
    }
}

# Main function
function Main {
    Write-Host "Configuration:" -ForegroundColor White
    Write-Host "  MySQL Host: $MySqlHost" -ForegroundColor Gray
    Write-Host "  MySQL Port: $MySqlPort" -ForegroundColor Gray
    Write-Host "  MySQL User: $MySqlUser" -ForegroundColor Gray
    Write-Host "  Database Name: $DatabaseName" -ForegroundColor Gray
    Write-Host "  Container Name: $ContainerName" -ForegroundColor Gray
    Write-Host ""
    
    # Check Docker
    if (-not (Test-DockerRunning)) {
        Write-Host "Please start Docker Desktop and try again" -ForegroundColor Yellow
        exit 1
    }
    
    # Find or create MySQL container
    $containerInfo = Get-MySqlContainer
    
    if (-not $containerInfo) {
        Write-Host "No MySQL container found, creating new one..." -ForegroundColor Yellow
        if (-not (New-MySqlContainer)) {
            exit 1
        }
        $containerInfo = Get-MySqlContainer
    }
    
    if (-not $containerInfo) {
        Write-Host "Failed to find or create MySQL container" -ForegroundColor Red
        exit 1
    }
    
    Write-Host "Found MySQL container: $($containerInfo.Name) ($($containerInfo.ID))" -ForegroundColor Green
    
    # Start container if needed
    if (-not (Start-MySqlContainer -ContainerInfo $containerInfo)) {
        exit 1
    }
    
    # Test database connection
    if (-not (Test-DatabaseConnection -ContainerInfo $containerInfo)) {
        Write-Host "Please check:" -ForegroundColor Yellow
        Write-Host "1. MySQL container is running properly" -ForegroundColor White
        Write-Host "2. MySQL service inside container is ready" -ForegroundColor White
        Write-Host "3. Connection parameters are correct" -ForegroundColor White
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
    
    # Execute SQL script
    $scriptPath = "scripts/init-database.sql"
    if (-not (Execute-SqlScript -ContainerInfo $containerInfo -ScriptPath $scriptPath)) {
        exit 1
    }
    
    # Verify initialization
    if (-not (Test-DatabaseInitialization -ContainerInfo $containerInfo)) {
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
    Write-Host ""
    Write-Host "Docker container info:" -ForegroundColor Cyan
    Write-Host "  Container ID: $($containerInfo.ID)" -ForegroundColor Gray
    Write-Host "  Container Name: $($containerInfo.Name)" -ForegroundColor Gray
    Write-Host "  Port Mapping: ${MySqlPort}:3306" -ForegroundColor Gray
}

# Execute main function
Main 