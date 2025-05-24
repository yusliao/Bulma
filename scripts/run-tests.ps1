# 锂电池包装工序MES系统 - 测试运行脚本
# 运行单元测试、集成测试并生成覆盖率报告

param(
    [string]$Configuration = "Debug",
    [string]$TestFilter = "",
    [switch]$Coverage = $false,
    [switch]$Integration = $false,
    [switch]$Verbose = $false
)

# 设置错误处理
$ErrorActionPreference = "Stop"

Write-Host "🧪 锂电池包装工序MES系统 - 测试运行器" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green

# 获取脚本目录和解决方案根目录
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootDir = Split-Path -Parent $scriptDir
$testDir = Join-Path $rootDir "tests"
$outputDir = Join-Path $rootDir "TestResults"

# 确保输出目录存在
if (!(Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

Write-Host "📂 配置信息:" -ForegroundColor Cyan
Write-Host "   - 配置: $Configuration" -ForegroundColor White
Write-Host "   - 根目录: $rootDir" -ForegroundColor White
Write-Host "   - 测试目录: $testDir" -ForegroundColor White
Write-Host "   - 输出目录: $outputDir" -ForegroundColor White

# 构建解决方案
Write-Host "`n🔨 构建解决方案..." -ForegroundColor Yellow
try {
    Set-Location $rootDir
    dotnet build --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        throw "构建失败"
    }
    Write-Host "✅ 构建成功" -ForegroundColor Green
}
catch {
    Write-Host "❌ 构建失败: $_" -ForegroundColor Red
    exit 1
}

# 准备测试命令参数
$testArgs = @(
    "test"
    "--configuration", $Configuration
    "--no-build"
    "--results-directory", $outputDir
    "--logger", "trx"
    "--logger", "console;verbosity=normal"
)

# 添加测试过滤器
if ($TestFilter) {
    $testArgs += "--filter", $TestFilter
    Write-Host "🔍 测试过滤器: $TestFilter" -ForegroundColor Cyan
}

# 配置覆盖率收集
if ($Coverage) {
    Write-Host "📊 启用代码覆盖率收集..." -ForegroundColor Cyan
    $testArgs += "--collect", "XPlat Code Coverage"
    $testArgs += "--settings", (Join-Path $rootDir "coverlet.runsettings")
}

# 配置详细输出
if ($Verbose) {
    $testArgs += "--verbosity", "detailed"
}

# 运行单元测试
Write-Host "`n🧪 运行单元测试..." -ForegroundColor Yellow
try {
    $unitTestArgs = $testArgs + @("--filter", "Category!=Integration")
    
    Write-Host "执行命令: dotnet $($unitTestArgs -join ' ')" -ForegroundColor Gray
    & dotnet @unitTestArgs
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "⚠️  一些单元测试失败" -ForegroundColor Yellow
    } else {
        Write-Host "✅ 所有单元测试通过" -ForegroundColor Green
    }
}
catch {
    Write-Host "❌ 单元测试执行失败: $_" -ForegroundColor Red
    exit 1
}

# 运行集成测试（可选）
if ($Integration) {
    Write-Host "`n🔗 运行集成测试..." -ForegroundColor Yellow
    try {
        $integrationTestArgs = $testArgs + @("--filter", "Category=Integration")
        
        Write-Host "执行命令: dotnet $($integrationTestArgs -join ' ')" -ForegroundColor Gray
        & dotnet @integrationTestArgs
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "⚠️  一些集成测试失败" -ForegroundColor Yellow
        } else {
            Write-Host "✅ 所有集成测试通过" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "❌ 集成测试执行失败: $_" -ForegroundColor Red
        exit 1
    }
}

# 生成覆盖率报告
if ($Coverage) {
    Write-Host "`n📈 生成覆盖率报告..." -ForegroundColor Yellow
    try {
        # 查找覆盖率文件
        $coverageFiles = Get-ChildItem -Path $outputDir -Filter "coverage.cobertura.xml" -Recurse
        
        if ($coverageFiles.Count -gt 0) {
            Write-Host "✅ 找到 $($coverageFiles.Count) 个覆盖率文件" -ForegroundColor Green
            
            # 安装 ReportGenerator 工具（如果未安装）
            $reportGenerator = "reportgenerator"
            try {
                & $reportGenerator --version | Out-Null
            }
            catch {
                Write-Host "📦 安装 ReportGenerator 工具..." -ForegroundColor Cyan
                dotnet tool install -g dotnet-reportgenerator-globaltool
            }
            
            # 生成HTML报告
            $reportDir = Join-Path $outputDir "CoverageReport"
            $coverageFileArgs = ($coverageFiles | ForEach-Object { $_.FullName }) -join ";"
            
            & $reportGenerator "-reports:$coverageFileArgs" "-targetdir:$reportDir" "-reporttypes:Html;Cobertura" "-classfilters:-System.*;-Microsoft.*"
            
            Write-Host "📊 覆盖率报告已生成: $reportDir" -ForegroundColor Green
            Write-Host "🌐 在浏览器中打开: $reportDir\index.html" -ForegroundColor Cyan
        } else {
            Write-Host "⚠️  未找到覆盖率文件" -ForegroundColor Yellow
        }
    }
    catch {
        Write-Host "❌ 覆盖率报告生成失败: $_" -ForegroundColor Red
    }
}

# 测试结果汇总
Write-Host "`n📋 测试结果汇总:" -ForegroundColor Cyan
$trxFiles = Get-ChildItem -Path $outputDir -Filter "*.trx" -Recurse | Sort-Object LastWriteTime -Descending

if ($trxFiles.Count -gt 0) {
    $latestTrx = $trxFiles[0]
    Write-Host "📄 最新测试结果文件: $($latestTrx.Name)" -ForegroundColor White
    Write-Host "📁 测试结果目录: $outputDir" -ForegroundColor White
} else {
    Write-Host "⚠️  未找到测试结果文件" -ForegroundColor Yellow
}

Write-Host "`n🎉 测试执行完成!" -ForegroundColor Green

# 显示使用帮助
if (!$TestFilter -and !$Coverage -and !$Integration) {
    Write-Host "`n💡 使用提示:" -ForegroundColor Cyan
    Write-Host "   运行所有测试: .\scripts\run-tests.ps1" -ForegroundColor White
    Write-Host "   运行特定测试: .\scripts\run-tests.ps1 -TestFilter 'AuthenticationServiceTests'" -ForegroundColor White
    Write-Host "   生成覆盖率报告: .\scripts\run-tests.ps1 -Coverage" -ForegroundColor White
    Write-Host "   运行集成测试: .\scripts\run-tests.ps1 -Integration" -ForegroundColor White
    Write-Host "   详细输出: .\scripts\run-tests.ps1 -Verbose" -ForegroundColor White
} 