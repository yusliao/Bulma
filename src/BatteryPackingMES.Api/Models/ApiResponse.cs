namespace BatteryPackingMES.Api.Models;

/// <summary>
/// API响应基类
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 响应数据
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// 默认构造函数
    /// </summary>
    public ApiResponse()
    {
    }

    /// <summary>
    /// 构造函数（成功响应）
    /// </summary>
    /// <param name="data">数据</param>
    /// <param name="message">消息</param>
    public ApiResponse(T data, string message = "操作成功")
    {
        Success = true;
        Data = data;
        Message = message;
    }

    /// <summary>
    /// 构造函数（失败响应）
    /// </summary>
    /// <param name="message">错误消息</param>
    public ApiResponse(string message)
    {
        Success = false;
        Message = message;
    }

    /// <summary>
    /// 成功响应
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 失败响应
    /// </summary>
    public static ApiResponse<T> Fail(string message, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// 成功响应（带数据）
    /// </summary>
    public static ApiResponse<T> CreateWithData<T>(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }
}

/// <summary>
/// 无数据的API响应
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// 响应消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 错误代码
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>
    /// 时间戳
    /// </summary>
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    /// <summary>
    /// 成功响应
    /// </summary>
    public static ApiResponse Ok(string message = "操作成功")
    {
        return new ApiResponse
        {
            Success = true,
            Message = message
        };
    }

    /// <summary>
    /// 成功响应（带数据）
    /// </summary>
    public static ApiResponse<T> OK<T>(T data, string message = "操作成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 失败响应
    /// </summary>
    public static ApiResponse Fail(string message, string? errorCode = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// 失败响应（带错误列表）
    /// </summary>
    public static ApiResponse Fail(string message, List<string> errors)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message + ": " + string.Join(", ", errors)
        };
    }
}

/// <summary>
/// 分页结果
/// </summary>
/// <typeparam name="T">数据类型</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// 数据项
    /// </summary>
    public List<T> Items { get; set; } = new List<T>();

    /// <summary>
    /// 总数
    /// </summary>
    public int Total { get; set; }

    /// <summary>
    /// 页码
    /// </summary>
    public int PageIndex { get; set; }

    /// <summary>
    /// 页大小
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;

    /// <summary>
    /// 是否有上一页
    /// </summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>
    /// 是否有下一页
    /// </summary>
    public bool HasNextPage => PageIndex < TotalPages;
} 