using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Infrastructure.Data;
using BatteryPackingMES.Infrastructure.Repositories;
using BatteryPackingMES.Infrastructure.Services;
using BatteryPackingMES.Api.Extensions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// 配置Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/battery-mes-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();

// 添加 HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// 配置数据库和仓库
builder.Services.AddScoped<MESDbContext>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();

// 配置业务服务
builder.Services.AddBusinessServices();

// 配置监控增强服务
builder.Services.AddScoped<IMetricsService, MetricsService>();

// 配置数据验证服务
builder.Services.AddScoped<IDataValidationService, DataValidationService>();

// 配置审计日志服务
builder.Services.AddScoped<IAuditService, AuditService>();

// 配置API版本控制
builder.Services.AddCustomApiVersioning();

// 配置JWT认证
builder.Services.AddJwtAuthentication(builder.Configuration);

// 配置Redis服务
builder.Services.AddRedisServices(builder.Configuration);

// 配置报表服务
builder.Services.AddReportServices();

// 配置基于策略的授权
builder.Services.AddPolicyBasedAuthorization();

// 配置版本化的Swagger
builder.Services.AddVersionedSwagger();

// 配置CORS
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "*" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        if (corsOrigins.Contains("*"))
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        }
        else
        {
            policy.WithOrigins(corsOrigins)
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        }
    });
});

// 配置JSON序列化
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = null; // 保持原始命名
    options.SerializerOptions.WriteIndented = true;
});

// 配置AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

var app = builder.Build();

// 初始化数据库
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<MESDbContext>();
        await dbContext.InitializeDatabaseAsync();
        await dbContext.SeedDataAsync();
        Log.Information("数据库初始化完成");
    }
    catch (Exception ex)
    {
        Log.Error(ex, "数据库初始化失败");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || app.Environment.IsStaging())
{
    var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
    app.UseVersionedSwagger(apiVersionDescriptionProvider);
}

app.UseHttpsRedirection();

app.UseCors("DefaultPolicy");

app.UseAuthentication();
app.UseAuthorization();

// 性能监控中间件
app.UsePerformanceMonitoring();

// 全局异常处理中间件
app.UseMiddleware<GlobalExceptionMiddleware>();

app.MapControllers();

Log.Information("锂电池包装工序MES系统启动成功");
Log.Information("API文档地址: {Url}", app.Environment.IsDevelopment() ? "https://localhost:5001" : "生产环境地址");

app.Run();

/// <summary>
/// 全局异常处理中间件
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "处理请求时发生未处理的异常");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 500;

        var response = new
        {
            Success = false,
            Message = "系统内部错误，请稍后重试",
            ErrorCode = "INTERNAL_SERVER_ERROR",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}

/// <summary>
/// Program类 - 用于集成测试
/// </summary>
public partial class Program { }