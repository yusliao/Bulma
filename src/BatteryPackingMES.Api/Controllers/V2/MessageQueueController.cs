using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Models.MessageTypes;
using BatteryPackingMES.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 消息队列控制器
/// </summary>
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
// [Authorize] // 暂时注释掉用于测试
public class MessageQueueController : ControllerBase
{
    private readonly IMessageQueueService _messageQueueService;
    private readonly ILogger<MessageQueueController> _logger;

    public MessageQueueController(
        IMessageQueueService messageQueueService,
        ILogger<MessageQueueController> logger)
    {
        _messageQueueService = messageQueueService;
        _logger = logger;
    }

    /// <summary>
    /// 发布生产线状态变更消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <returns></returns>
    [HttpPost("production-line-status")]
    public async Task<IActionResult> PublishProductionLineStatusAsync([FromBody] ProductionLineStatusMessage message)
    {
        try
        {
            await _messageQueueService.PublishAsync("production.line.status", message);
            return Ok(new { success = true, message = "消息发布成功", messageId = message.MessageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布生产线状态消息失败");
            return StatusCode(500, new { success = false, message = "消息发布失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 发布质量检测消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <returns></returns>
    [HttpPost("quality-check")]
    public async Task<IActionResult> PublishQualityCheckAsync([FromBody] QualityCheckMessage message)
    {
        try
        {
            await _messageQueueService.PublishAsync("quality.check", message);
            return Ok(new { success = true, message = "质量检测消息发布成功", messageId = message.MessageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布质量检测消息失败");
            return StatusCode(500, new { success = false, message = "消息发布失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 发布设备告警消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <returns></returns>
    [HttpPost("equipment-alarm")]
    public async Task<IActionResult> PublishEquipmentAlarmAsync([FromBody] EquipmentAlarmMessage message)
    {
        try
        {
            await _messageQueueService.PublishAsync("equipment.alarm", message);
            return Ok(new { success = true, message = "设备告警消息发布成功", messageId = message.MessageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布设备告警消息失败");
            return StatusCode(500, new { success = false, message = "消息发布失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 发布生产订单消息
    /// </summary>
    /// <param name="message">消息内容</param>
    /// <returns></returns>
    [HttpPost("production-order")]
    public async Task<IActionResult> PublishProductionOrderAsync([FromBody] ProductionOrderMessage message)
    {
        try
        {
            await _messageQueueService.PublishAsync("production.order", message);
            return Ok(new { success = true, message = "生产订单消息发布成功", messageId = message.MessageId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "发布生产订单消息失败");
            return StatusCode(500, new { success = false, message = "消息发布失败", error = ex.Message });
        }
    }

    /// <summary>
    /// 获取历史消息
    /// </summary>
    /// <param name="channel">频道名称</param>
    /// <param name="count">数量</param>
    /// <returns></returns>
    [HttpGet("history/{channel}")]
    public async Task<IActionResult> GetHistoryMessagesAsync(string channel, [FromQuery] int count = 100)
    {
        try
        {
            var redisService = _messageQueueService as RedisMessageQueueService;
            if (redisService == null)
            {
                return BadRequest(new { success = false, message = "当前消息队列服务不支持历史消息查询" });
            }

            var messages = await redisService.GetHistoryMessagesAsync<ProductionMessage>(channel, count);
            return Ok(new { success = true, data = messages, count = messages.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取历史消息失败，频道: {Channel}", channel);
            return StatusCode(500, new { success = false, message = "获取历史消息失败", error = ex.Message });
        }
    }
} 