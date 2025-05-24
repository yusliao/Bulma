using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BatteryPackingMES.Api.Extensions;

/// <summary>
/// ModelStateDictionary 扩展方法
/// </summary>
public static class ModelStateExtensions
{
    /// <summary>
    /// 获取所有错误信息
    /// </summary>
    /// <param name="modelState">模型状态</param>
    /// <returns>错误信息列表</returns>
    public static List<string> GetErrors(this ModelStateDictionary modelState)
    {
        var errors = new List<string>();
        
        foreach (var state in modelState)
        {
            foreach (var error in state.Value.Errors)
            {
                errors.Add(error.ErrorMessage);
            }
        }
        
        return errors;
    }
} 