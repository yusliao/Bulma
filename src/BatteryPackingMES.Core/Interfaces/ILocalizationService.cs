using System.Globalization;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 本地化服务接口
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// 获取本地化字符串
    /// </summary>
    string GetString(string key, CultureInfo? culture = null);

    /// <summary>
    /// 获取格式化的本地化字符串
    /// </summary>
    string GetString(string key, object[] args, CultureInfo? culture = null);

    /// <summary>
    /// 获取所有支持的语言
    /// </summary>
    IEnumerable<SupportedLanguage> GetSupportedLanguages();

    /// <summary>
    /// 设置当前语言
    /// </summary>
    void SetCurrentLanguage(string languageCode);

    /// <summary>
    /// 获取当前语言
    /// </summary>
    CultureInfo GetCurrentLanguage();

    /// <summary>
    /// 检查是否支持指定语言
    /// </summary>
    bool IsLanguageSupported(string languageCode);

    /// <summary>
    /// 获取本地化的枚举显示名称
    /// </summary>
    string GetEnumDisplayName<T>(T enumValue, CultureInfo? culture = null) where T : Enum;

    /// <summary>
    /// 获取本地化的日期格式
    /// </summary>
    string GetLocalizedDate(DateTime dateTime, CultureInfo? culture = null);

    /// <summary>
    /// 获取本地化的数字格式
    /// </summary>
    string GetLocalizedNumber(decimal number, CultureInfo? culture = null);

    /// <summary>
    /// 获取本地化的货币格式
    /// </summary>
    string GetLocalizedCurrency(decimal amount, CultureInfo? culture = null);
}

/// <summary>
/// 支持的语言信息
/// </summary>
public class SupportedLanguage
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string NativeName { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
    public bool IsRightToLeft { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>
/// 多语言资源接口
/// </summary>
public interface IMultiLanguageResourceService
{
    /// <summary>
    /// 获取资源字符串
    /// </summary>
    Task<string?> GetResourceAsync(string key, string languageCode);

    /// <summary>
    /// 设置资源字符串
    /// </summary>
    Task SetResourceAsync(string key, string languageCode, string value);

    /// <summary>
    /// 获取所有资源
    /// </summary>
    Task<Dictionary<string, string>> GetAllResourcesAsync(string languageCode);

    /// <summary>
    /// 导入资源
    /// </summary>
    Task ImportResourcesAsync(string languageCode, Dictionary<string, string> resources);

    /// <summary>
    /// 导出资源
    /// </summary>
    Task<Dictionary<string, string>> ExportResourcesAsync(string languageCode);

    /// <summary>
    /// 删除资源
    /// </summary>
    Task DeleteResourceAsync(string key, string languageCode);

    /// <summary>
    /// 搜索资源
    /// </summary>
    Task<Dictionary<string, string>> SearchResourcesAsync(string languageCode, string searchTerm);
} 