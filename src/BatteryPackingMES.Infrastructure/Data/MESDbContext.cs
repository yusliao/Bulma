using SqlSugar;
using BatteryPackingMES.Core.Entities;
using Microsoft.Extensions.Configuration;

namespace BatteryPackingMES.Infrastructure.Data;

/// <summary>
/// MES数据库上下文
/// </summary>
public class MESDbContext
{
    private readonly IConfiguration _configuration;

    public MESDbContext(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// 获取主数据库连接
    /// </summary>
    public ISqlSugarClient GetMasterDb()
    {
        var connectionString = _configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("DefaultConnection not found");

        var db = new SqlSugarClient(new ConnectionConfig()
        {
            ConnectionString = connectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            MoreSettings = new ConnMoreSettings()
            {
                IsAutoRemoveDataCache = true
            }
        });

        // 配置全局过滤器
        db.QueryFilter.AddTableFilter<BaseEntity>(it => it.IsDeleted == false);

        // 配置自动填充
        db.Aop.OnLogExecuting = (sql, pars) =>
        {
            Console.WriteLine($"SQL: {sql}");
            if (pars != null && pars.Any())
            {
                Console.WriteLine($"Parameters: {string.Join(",", pars.Select(p => $"{p.ParameterName}={p.Value}"))}");
            }
        };

        return db;
    }

    /// <summary>
    /// 获取从数据库连接（只读）
    /// </summary>
    public ISqlSugarClient GetSlaveDb()
    {
        var connectionString = _configuration.GetConnectionString("SlaveConnection") 
            ?? _configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection not found");

        var db = new SqlSugarClient(new ConnectionConfig()
        {
            ConnectionString = connectionString,
            DbType = DbType.MySql,
            IsAutoCloseConnection = true,
            InitKeyType = InitKeyType.Attribute,
            MoreSettings = new ConnMoreSettings()
            {
                IsAutoRemoveDataCache = true
            }
        });

        // 配置全局过滤器
        db.QueryFilter.AddTableFilter<BaseEntity>(it => it.IsDeleted == false);

        return db;
    }

    /// <summary>
    /// 初始化数据库
    /// </summary>
    public Task InitializeDatabaseAsync()
    {
        var db = GetMasterDb();

        // 创建数据库表
        db.CodeFirst.InitTables(
            // 认证相关表
            typeof(User),
            typeof(Role),
            typeof(Permission),
            typeof(UserRole),
            typeof(RolePermission),
            // 生产相关表
            typeof(Process),
            typeof(ProcessRoute),
            typeof(ProductionBatch),
            typeof(ProductionParameter),
            // 编码管理表
            typeof(CodeGenerationRule),
            typeof(CodeGenerationHistory),
            // 产品追溯表
            typeof(ProductItem),
            typeof(ProductTraceability),
            // 系统功能表
            typeof(SystemMetric),
            typeof(AuditLog),
            typeof(AuditConfiguration),
            typeof(DataValidationRule),
            typeof(DataValidationResult),
            typeof(LocalizationResource),
            typeof(SupportedLanguageConfig)
        );

        // 创建分表
        db.CodeFirst.SetStringDefaultLength(200).InitTables(typeof(HighFrequencyParameter));

        Console.WriteLine("数据库初始化完成");
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 种子数据
    /// </summary>
    public async Task SeedDataAsync()
    {
        var db = GetMasterDb();

        // 检查是否已有数据
        if (await db.Queryable<Process>().AnyAsync())
        {
            return;
        }

        // 插入默认权限数据
        var permissions = new List<Permission>
        {
            new() { Name = "用户查看", Code = "users.view", Description = "查看用户信息", Module = "用户管理", IsSystemPermission = true },
            new() { Name = "用户管理", Code = "users.manage", Description = "创建、编辑、删除用户", Module = "用户管理", IsSystemPermission = true },
            new() { Name = "工序查看", Code = "processes.view", Description = "查看工序信息", Module = "工序管理", IsSystemPermission = true },
            new() { Name = "工序管理", Code = "processes.manage", Description = "创建、编辑、删除工序", Module = "工序管理", IsSystemPermission = true },
            new() { Name = "生产查看", Code = "production.view", Description = "查看生产批次", Module = "生产管理", IsSystemPermission = true },
            new() { Name = "生产管理", Code = "production.manage", Description = "管理生产批次", Module = "生产管理", IsSystemPermission = true },
            new() { Name = "编码管理", Code = "codes.manage", Description = "管理批次号和序列号", Module = "编码管理", IsSystemPermission = true },
            new() { Name = "产品追溯", Code = "traceability.view", Description = "查看产品追溯信息", Module = "质量管理", IsSystemPermission = true },
            new() { Name = "报表查看", Code = "reports.view", Description = "查看各类报表", Module = "报表管理", IsSystemPermission = true }
        };

        await db.Insertable(permissions).ExecuteCommandAsync();

        // 插入默认角色数据
        var roles = new List<Role>
        {
            new() { Name = "系统管理员", Description = "拥有系统所有权限", IsSystemRole = true },
            new() { Name = "操作员", Description = "生产操作权限", IsSystemRole = true },
            new() { Name = "质量员", Description = "质量检测和追溯权限", IsSystemRole = true },
            new() { Name = "查看者", Description = "只读权限", IsSystemRole = true }
        };

        await db.Insertable(roles).ExecuteCommandAsync();

        // 插入默认工序数据
        var processes = new List<Process>
        {
            new() { ProcessCode = "P001", ProcessName = "电芯包装", ProcessType = Core.Enums.ProcessType.CellPacking, StandardTime = 30, SortOrder = 1 },
            new() { ProcessCode = "P002", ProcessName = "模组包装", ProcessType = Core.Enums.ProcessType.ModulePacking, StandardTime = 60, SortOrder = 2 },
            new() { ProcessCode = "P003", ProcessName = "Pack包装", ProcessType = Core.Enums.ProcessType.PackPacking, StandardTime = 90, SortOrder = 3 },
            new() { ProcessCode = "P004", ProcessName = "栈板包装", ProcessType = Core.Enums.ProcessType.PalletPacking, StandardTime = 45, SortOrder = 4 }
        };

        await db.Insertable(processes).ExecuteCommandAsync();

        // 插入默认编码生成规则
        var codeRules = new List<CodeGenerationRule>
        {
            new()
            {
                RuleName = "电芯批次号规则",
                CodeType = "BatchCode",
                ProductType = "Cell",
                Prefix = "CELL",
                DateFormat = "yyyyMMdd",
                SequenceLength = 3,
                StartNumber = 1,
                ResetDaily = true,
                Template = "{Prefix}{Date}{Sequence}",
                ValidationPattern = @"^CELL\d{8}\d{3}$",
                IsEnabled = true,
                Description = "电芯产品批次号自动生成规则"
            },
            new()
            {
                RuleName = "模组批次号规则",
                CodeType = "BatchCode",
                ProductType = "Module",
                Prefix = "MOD",
                DateFormat = "yyyyMMdd",
                SequenceLength = 3,
                StartNumber = 1,
                ResetDaily = true,
                Template = "{Prefix}{Date}{Sequence}",
                ValidationPattern = @"^MOD\d{8}\d{3}$",
                IsEnabled = true,
                Description = "模组产品批次号自动生成规则"
            },
            new()
            {
                RuleName = "Pack批次号规则",
                CodeType = "BatchCode",
                ProductType = "Pack",
                Prefix = "PACK",
                DateFormat = "yyyyMMdd",
                SequenceLength = 3,
                StartNumber = 1,
                ResetDaily = true,
                Template = "{Prefix}{Date}{Sequence}",
                ValidationPattern = @"^PACK\d{8}\d{3}$",
                IsEnabled = true,
                Description = "Pack产品批次号自动生成规则"
            },
            new()
            {
                RuleName = "电芯序列号规则",
                CodeType = "SerialNumber",
                ProductType = "Cell",
                SequenceLength = 6,
                StartNumber = 1,
                ResetDaily = false,
                Template = "{BatchNumber}-{Sequence}",
                ValidationPattern = @"^.+-\d{6}$",
                IsEnabled = true,
                Description = "电芯产品序列号自动生成规则"
            }
        };

        await db.Insertable(codeRules).ExecuteCommandAsync();

        // 插入默认语言配置
        var languages = new List<SupportedLanguageConfig>
        {
            new()
            {
                LanguageCode = "zh-CN",
                Name = "Chinese (Simplified)",
                NativeName = "简体中文",
                Flag = "🇨🇳",
                IsDefault = true,
                IsEnabled = true,
                DisplayOrder = 1,
                CompletionPercentage = 100,
                CurrencyCode = "CNY",
                DateFormat = "yyyy-MM-dd",
                TimeFormat = "HH:mm:ss",
                NumberFormat = "#,##0.##"
            },
            new()
            {
                LanguageCode = "en-US",
                Name = "English (United States)",
                NativeName = "English",
                Flag = "🇺🇸",
                IsDefault = false,
                IsEnabled = true,
                DisplayOrder = 2,
                CompletionPercentage = 95,
                CurrencyCode = "USD",
                DateFormat = "MM/dd/yyyy",
                TimeFormat = "h:mm:ss tt",
                NumberFormat = "#,##0.##"
            }
        };

        await db.Insertable(languages).ExecuteCommandAsync();

        // 创建默认管理员用户
        var salt = GenerateSalt();
        var passwordHash = HashPassword("admin123", salt);

        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = passwordHash,
            Salt = salt,
            RealName = "系统管理员",
            Email = "admin@battery-mes.com",
            IsEnabled = true,
            CreatedTime = DateTime.UtcNow
        };

        await db.Insertable(adminUser).ExecuteCommandAsync();

        Console.WriteLine("种子数据初始化完成");
    }

    /// <summary>
    /// 生成盐值
    /// </summary>
    private string GenerateSalt()
    {
        var saltBytes = new byte[32];
        using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        return Convert.ToBase64String(saltBytes);
    }

    /// <summary>
    /// 生成密码哈希
    /// </summary>
    private string HashPassword(string password, string salt)
    {
        using var hmac = new System.Security.Cryptography.HMACSHA256(System.Text.Encoding.UTF8.GetBytes(salt));
        var hash = hmac.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hash);
    }
} 