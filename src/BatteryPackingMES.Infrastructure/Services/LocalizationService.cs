using BatteryPackingMES.Core.Entities;
using BatteryPackingMES.Core.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;
using System.Collections.Concurrent;

namespace BatteryPackingMES.Infrastructure.Services;

/// <summary>
/// 本地化服务实现
/// </summary>
public class LocalizationService : ILocalizationService
{
    private readonly IMultiLanguageResourceService _resourceService;
    private readonly IMemoryCache _memoryCache;
    private readonly IDatabase _redisDatabase;
    private readonly ILogger<LocalizationService> _logger;
    private readonly ConcurrentDictionary<string, SupportedLanguage> _supportedLanguages;
    private CultureInfo _currentCulture;

    public LocalizationService(
        IMultiLanguageResourceService resourceService,
        IMemoryCache memoryCache,
        IConnectionMultiplexer redis,
        ILogger<LocalizationService> logger)
    {
        _resourceService = resourceService;
        _memoryCache = memoryCache;
        _redisDatabase = redis.GetDatabase();
        _logger = logger;
        _supportedLanguages = new ConcurrentDictionary<string, SupportedLanguage>();
        _currentCulture = new CultureInfo("zh-CN"); // 默认中文

        InitializeSupportedLanguages();
    }

    public string GetString(string key, CultureInfo? culture = null)
    {
        culture ??= _currentCulture;
        var languageCode = culture.Name;

        // 优先从内存缓存获取
        var cacheKey = $"localization:{languageCode}:{key}";
        if (_memoryCache.TryGetValue(cacheKey, out string? cachedValue) && !string.IsNullOrEmpty(cachedValue))
        {
            return cachedValue;
        }

        // 从Redis缓存获取
        try
        {
            var redisValue = _redisDatabase.StringGet(cacheKey);
            if (redisValue.HasValue)
            {
                var value = redisValue.ToString();
                _memoryCache.Set(cacheKey, value, TimeSpan.FromMinutes(30));
                return value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "从Redis获取本地化资源失败: {Key}, 语言: {Language}", key, languageCode);
        }

        // 从数据库获取
        var result = GetStringFromDatabase(key, languageCode);
        
        // 缓存结果
        if (!string.IsNullOrEmpty(result))
        {
            _memoryCache.Set(cacheKey, result, TimeSpan.FromMinutes(30));
            try
            {
                _redisDatabase.StringSet(cacheKey, result, TimeSpan.FromHours(2));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "设置Redis本地化缓存失败: {Key}", cacheKey);
            }
        }

        return result ?? key; // 如果找不到翻译，返回原始key
    }

    public string GetString(string key, object[] args, CultureInfo? culture = null)
    {
        var template = GetString(key, culture);
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "格式化本地化字符串失败: {Key}, 模板: {Template}", key, template);
            return template;
        }
    }

    public IEnumerable<SupportedLanguage> GetSupportedLanguages()
    {
        return _supportedLanguages.Values.OrderBy(x => x.Code);
    }

    public void SetCurrentLanguage(string languageCode)
    {
        if (IsLanguageSupported(languageCode))
        {
            _currentCulture = new CultureInfo(languageCode);
            Thread.CurrentThread.CurrentCulture = _currentCulture;
            Thread.CurrentThread.CurrentUICulture = _currentCulture;
            
            _logger.LogInformation("当前语言已设置为: {Language}", languageCode);
        }
        else
        {
            _logger.LogWarning("不支持的语言代码: {Language}", languageCode);
        }
    }

    public CultureInfo GetCurrentLanguage()
    {
        return _currentCulture;
    }

    public bool IsLanguageSupported(string languageCode)
    {
        return _supportedLanguages.ContainsKey(languageCode);
    }

    public string GetEnumDisplayName<T>(T enumValue, CultureInfo? culture = null) where T : Enum
    {
        var enumType = typeof(T);
        var memberName = enumValue.ToString();
        var key = $"Enum.{enumType.Name}.{memberName}";
        
        var localizedName = GetString(key, culture);
        return localizedName == key ? memberName : localizedName;
    }

    public string GetLocalizedDate(DateTime dateTime, CultureInfo? culture = null)
    {
        culture ??= _currentCulture;
        return dateTime.ToString("D", culture);
    }

    public string GetLocalizedNumber(decimal number, CultureInfo? culture = null)
    {
        culture ??= _currentCulture;
        return number.ToString("N", culture);
    }

    public string GetLocalizedCurrency(decimal amount, CultureInfo? culture = null)
    {
        culture ??= _currentCulture;
        return amount.ToString("C", culture);
    }

    private string? GetStringFromDatabase(string key, string languageCode)
    {
        try
        {
            return _resourceService.GetResourceAsync(key, languageCode).Result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "从数据库获取本地化资源失败: {Key}, 语言: {Language}", key, languageCode);
            
            // 尝试获取默认语言的资源
            if (languageCode != "zh-CN")
            {
                return GetStringFromDatabase(key, "zh-CN");
            }
            
            return null;
        }
    }

    private void InitializeSupportedLanguages()
    {
        var languages = new[]
        {
            new SupportedLanguage
            {
                Code = "zh-CN",
                Name = "Chinese (Simplified)",
                NativeName = "简体中文",
                Flag = "🇨🇳",
                IsRightToLeft = false,
                IsDefault = true
            },
            new SupportedLanguage
            {
                Code = "en-US",
                Name = "English (United States)",
                NativeName = "English",
                Flag = "🇺🇸",
                IsRightToLeft = false,
                IsDefault = false
            },
            new SupportedLanguage
            {
                Code = "ja-JP",
                Name = "Japanese",
                NativeName = "日本語",
                Flag = "🇯🇵",
                IsRightToLeft = false,
                IsDefault = false
            },
            new SupportedLanguage
            {
                Code = "ko-KR",
                Name = "Korean",
                NativeName = "한국어",
                Flag = "🇰🇷",
                IsRightToLeft = false,
                IsDefault = false
            },
            new SupportedLanguage
            {
                Code = "de-DE",
                Name = "German",
                NativeName = "Deutsch",
                Flag = "🇩🇪",
                IsRightToLeft = false,
                IsDefault = false
            },
            new SupportedLanguage
            {
                Code = "fr-FR",
                Name = "French",
                NativeName = "Français",
                Flag = "🇫🇷",
                IsRightToLeft = false,
                IsDefault = false
            }
        };

        foreach (var language in languages)
        {
            _supportedLanguages.TryAdd(language.Code, language);
        }
    }
}

/// <summary>
/// 多语言资源服务实现
/// </summary>
public class MultiLanguageResourceService : IMultiLanguageResourceService
{
    private readonly IRepository<LocalizationResource> _resourceRepository;
    private readonly IRepository<SupportedLanguageConfig> _languageRepository;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MultiLanguageResourceService> _logger;

    public MultiLanguageResourceService(
        IRepository<LocalizationResource> resourceRepository,
        IRepository<SupportedLanguageConfig> languageRepository,
        IMemoryCache memoryCache,
        ILogger<MultiLanguageResourceService> logger)
    {
        _resourceRepository = resourceRepository;
        _languageRepository = languageRepository;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<string?> GetResourceAsync(string key, string languageCode)
    {
        var cacheKey = $"resource:{languageCode}:{key}";
        
        if (_memoryCache.TryGetValue(cacheKey, out string? cachedValue))
        {
            return cachedValue;
        }

        try
        {
            var resource = await _resourceRepository.GetFirstOrDefaultAsync(
                r => r.Key == key && r.LanguageCode == languageCode && r.IsApproved);

            var value = resource?.Value;
            if (value != null)
            {
                _memoryCache.Set(cacheKey, value, TimeSpan.FromMinutes(30));
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取本地化资源失败: {Key}, 语言: {Language}", key, languageCode);
            return null;
        }
    }

    public async Task SetResourceAsync(string key, string languageCode, string value)
    {
        try
        {
            var existingResource = await _resourceRepository.GetFirstOrDefaultAsync(
                r => r.Key == key && r.LanguageCode == languageCode);

            if (existingResource != null)
            {
                existingResource.Value = value;
                existingResource.UpdatedTime = DateTime.Now;
                await _resourceRepository.UpdateAsync(existingResource);
            }
            else
            {
                var newResource = new LocalizationResource
                {
                    Key = key,
                    LanguageCode = languageCode,
                    Value = value,
                    IsApproved = false
                };
                await _resourceRepository.AddAsync(newResource);
            }

            // 清除缓存
            var cacheKey = $"resource:{languageCode}:{key}";
            _memoryCache.Remove(cacheKey);
            _memoryCache.Remove($"resources:{languageCode}");

            _logger.LogInformation("本地化资源已更新: {Key}, 语言: {Language}", key, languageCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设置本地化资源失败: {Key}, 语言: {Language}", key, languageCode);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> GetAllResourcesAsync(string languageCode)
    {
        var cacheKey = $"resources:{languageCode}";
        
        if (_memoryCache.TryGetValue(cacheKey, out Dictionary<string, string>? cachedResources))
        {
            return cachedResources!;
        }

        try
        {
            var resources = await _resourceRepository.GetAllAsync(
                r => r.LanguageCode == languageCode && r.IsApproved);

            var result = resources.ToDictionary(r => r.Key, r => r.Value);
            _memoryCache.Set(cacheKey, result, TimeSpan.FromHours(1));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取所有本地化资源失败: 语言: {Language}", languageCode);
            return new Dictionary<string, string>();
        }
    }

    public async Task ImportResourcesAsync(string languageCode, Dictionary<string, string> resources)
    {
        try
        {
            foreach (var kvp in resources)
            {
                await SetResourceAsync(kvp.Key, languageCode, kvp.Value);
            }

            _logger.LogInformation("导入本地化资源完成: 语言: {Language}, 数量: {Count}", 
                languageCode, resources.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导入本地化资源失败: 语言: {Language}", languageCode);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> ExportResourcesAsync(string languageCode)
    {
        return await GetAllResourcesAsync(languageCode);
    }

    public async Task DeleteResourceAsync(string key, string languageCode)
    {
        try
        {
            var resource = await _resourceRepository.GetFirstOrDefaultAsync(
                r => r.Key == key && r.LanguageCode == languageCode);

            if (resource != null)
            {
                await _resourceRepository.DeleteAsync(resource);
                
                // 清除缓存
                var cacheKey = $"resource:{languageCode}:{key}";
                _memoryCache.Remove(cacheKey);
                _memoryCache.Remove($"resources:{languageCode}");

                _logger.LogInformation("本地化资源已删除: {Key}, 语言: {Language}", key, languageCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除本地化资源失败: {Key}, 语言: {Language}", key, languageCode);
            throw;
        }
    }

    public async Task<Dictionary<string, string>> SearchResourcesAsync(string languageCode, string searchTerm)
    {
        try
        {
            var resources = await _resourceRepository.GetAllAsync(
                r => r.LanguageCode == languageCode && 
                     r.IsApproved &&
                     (r.Key.Contains(searchTerm) || r.Value.Contains(searchTerm)));

            return resources.ToDictionary(r => r.Key, r => r.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索本地化资源失败: 语言: {Language}, 搜索词: {SearchTerm}", 
                languageCode, searchTerm);
            return new Dictionary<string, string>();
        }
    }
} 