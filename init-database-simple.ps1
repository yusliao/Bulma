# 简化版数据库初始化脚本
Write-Host "正在初始化数据库..." -ForegroundColor Cyan

# 执行完整版脚本
& "scripts/init-database.ps1" -SkipUserPrompt

Write-Host "数据库初始化完成!" -ForegroundColor Green 