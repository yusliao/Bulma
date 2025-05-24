using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using StackExchange.Redis;
using System.Text;

namespace BatteryPackingMES.Api.Extensions;

/// <summary>
/// 服务集合扩展
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// 添加API版本控制
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddCustomApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            // 默认API版本
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;

            // 版本号的提供方式
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(), // 从URL段读取版本 /api/v1.0/controller
                new HeaderApiVersionReader("X-Version"), // 从请求头读取版本
                new QueryStringApiVersionReader("version") // 从查询字符串读取版本
            );

            // 版本号格式
            options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);
        });

        services.AddVersionedApiExplorer(setup =>
        {
            setup.GroupNameFormat = "'v'VVV";
            setup.SubstituteApiVersionInUrl = true;
        });

        return services;
    }

    /// <summary>
    /// 添加JWT认证
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtKey = configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
        var jwtIssuer = configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
        var jwtAudience = configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,
                ValidateAudience = true,
                ValidAudience = jwtAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                    {
                        context.Response.Headers.Add("Token-Expired", "true");
                    }
                    return Task.CompletedTask;
                }
            };
        });

        return services;
    }

    /// <summary>
    /// 添加基于策略的授权
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddPolicyBasedAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // 定义权限策略
            options.AddPolicy("RequireAdminRole", policy =>
                policy.RequireClaim("role", "Admin"));

            options.AddPolicy("RequireOperatorRole", policy =>
                policy.RequireClaim("role", "Operator", "Admin"));

            options.AddPolicy("RequireViewerRole", policy =>
                policy.RequireClaim("role", "Viewer", "Operator", "Admin"));

            // 定义具体权限策略
            options.AddPolicy("CanManageUsers", policy =>
                policy.RequireClaim("permission", "users.manage"));

            options.AddPolicy("CanViewReports", policy =>
                policy.RequireClaim("permission", "reports.view"));

            options.AddPolicy("CanManageProcesses", policy =>
                policy.RequireClaim("permission", "processes.manage"));

            options.AddPolicy("CanManageProduction", policy =>
                policy.RequireClaim("permission", "production.manage"));
        });

        return services;
    }

    /// <summary>
    /// 添加版本化的Swagger
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddVersionedSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            // 安全定义
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // 包含XML注释
            var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }

            // 操作过滤器，用于版本控制
            options.OperationFilter<SwaggerDefaultValues>();
            
            // 文档过滤器，用于移除版本参数
            options.DocumentFilter<ReplaceVersionWithExactValueInPath>();
        });

        // 配置版本化的API Explorer
        services.ConfigureOptions<ConfigureSwaggerOptions>();

        return services;
    }

    /// <summary>
    /// 配置版本化的Swagger
    /// </summary>
    /// <param name="app">应用程序构建器</param>
    /// <param name="provider">API版本描述提供者</param>
    /// <returns>应用程序构建器</returns>
    public static IApplicationBuilder UseVersionedSwagger(this IApplicationBuilder app, IApiVersionDescriptionProvider provider)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            foreach (var description in provider.ApiVersionDescriptions)
            {
                options.SwaggerEndpoint(
                    $"/swagger/{description.GroupName}/swagger.json",
                    $"锂电池包装工序MES系统 API {description.GroupName.ToUpperInvariant()}");
            }

            options.RoutePrefix = string.Empty; // Swagger UI 在根路径
            options.DocumentTitle = "锂电池包装工序MES系统 API";
            options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
        });

        return app;
    }

    /// <summary>
    /// 配置Redis服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddRedisServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Redis") ?? configuration["Redis:ConnectionString"];
        
        if (!string.IsNullOrEmpty(connectionString))
        {
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var configurationOptions = ConfigurationOptions.Parse(connectionString);
                configurationOptions.CommandMap = CommandMap.Create(new HashSet<string>
                {
                    "INFO", "CONFIG", "CLUSTER", "PING", "ECHO", "CLIENT"
                }, available: false);
                
                return ConnectionMultiplexer.Connect(configurationOptions);
            });

            // 注册缓存服务
            services.AddSingleton<ICacheService, RedisCacheService>();

            // 注册消息队列服务
            services.AddSingleton<IMessageQueueService, RedisMessageQueueService>();
        }
        else
        {
            // 如果没有Redis配置，使用内存缓存
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        return services;
    }

    /// <summary>
    /// 配置报表服务
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <returns>服务集合</returns>
    public static IServiceCollection AddReportServices(this IServiceCollection services)
    {
        services.AddScoped<IReportService, ReportService>();
        return services;
    }

    // 配置业务服务
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<ICodeGenerationService, CodeGenerationService>();
        services.AddScoped<IMetricsService, MetricsService>();
        services.AddScoped<IDataValidationService, DataValidationService>();
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }
}

/// <summary>
/// Swagger默认值操作过滤器
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        foreach (var responseType in context.ApiDescription.SupportedResponseTypes)
        {
            var responseKey = responseType.IsDefaultResponse ? "default" : responseType.StatusCode.ToString();
            var response = operation.Responses[responseKey];

            foreach (var contentType in response.Content.Keys)
            {
                if (responseType.ApiResponseFormats.All(x => x.MediaType != contentType))
                {
                    response.Content.Remove(contentType);
                }
            }
        }

        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions.First(p => p.Name == parameter.Name);
            parameter.Description ??= description.ModelMetadata?.Description;

            if (parameter.Schema.Default == null && description.DefaultValue != null)
            {
                parameter.Schema.Default = new Microsoft.OpenApi.Any.OpenApiString(description.DefaultValue.ToString());
            }

            parameter.Required |= description.IsRequired;
        }
    }
}

/// <summary>
/// 替换版本占位符的文档过滤器
/// </summary>
public class ReplaceVersionWithExactValueInPath : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var paths = new OpenApiPaths();
        
        foreach (var path in swaggerDoc.Paths)
        {
            paths.Add(path.Key.Replace("v{version}", swaggerDoc.Info.Version), path.Value);
        }
        
        swaggerDoc.Paths = paths;
    }
}

/// <summary>
/// 配置Swagger选项
/// </summary>
public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    public ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }

    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, CreateInfoForApiVersion(description));
        }
    }

    private static OpenApiInfo CreateInfoForApiVersion(ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "锂电池包装工序MES系统 API",
            Version = description.ApiVersion.ToString(),
            Description = "锂电池包装工序制造执行系统(MES) RESTful API",
            Contact = new OpenApiContact
            {
                Name = "MES系统开发团队",
                Email = "dev@batterypackingmes.com"
            },
            License = new OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        };

        if (description.IsDeprecated)
        {
            info.Description += " - 此API版本已弃用";
        }

        return info;
    }
} 