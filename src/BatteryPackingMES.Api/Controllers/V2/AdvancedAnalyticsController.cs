using BatteryPackingMES.Core.Interfaces;
using BatteryPackingMES.Core.Models.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BatteryPackingMES.Api.Controllers.V2;

/// <summary>
/// 高级分析控制器
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("2.0")]
[Authorize]
[Produces("application/json")]
public class AdvancedAnalyticsController : ControllerBase
{
    private readonly IAdvancedAnalyticsService _analyticsService;
    private readonly IMachineLearningService _mlService;
    private readonly IPredictiveAnalyticsService _predictiveService;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<AdvancedAnalyticsController> _logger;

    public AdvancedAnalyticsController(
        IAdvancedAnalyticsService analyticsService,
        IMachineLearningService mlService,
        IPredictiveAnalyticsService predictiveService,
        ILocalizationService localizationService,
        ILogger<AdvancedAnalyticsController> logger)
    {
        _analyticsService = analyticsService;
        _mlService = mlService;
        _predictiveService = predictiveService;
        _localizationService = localizationService;
        _logger = logger;
    }

    /// <summary>
    /// 获取生产效率分析
    /// </summary>
    /// <param name="from">开始时间</param>
    /// <param name="to">结束时间</param>
    /// <param name="workshopCode">车间代码（可选）</param>
    /// <returns>生产效率分析结果</returns>
    [HttpGet("production-efficiency")]
    [ProducesResponseType(typeof(ProductionEfficiencyAnalysis), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProductionEfficiencyAnalysis>> GetProductionEfficiencyAnalysis(
        [Required] DateTimeOffset from,
        [Required] DateTimeOffset to,
        string? workshopCode = null)
    {
        try
        {
            if (from >= to)
            {
                return BadRequest(_localizationService.GetString("Analytics.InvalidDateRange"));
            }

            var analysis = await _analyticsService.GetProductionEfficiencyAnalysisAsync(from, to, workshopCode);
            
            _logger.LogInformation("生产效率分析完成: 时间范围 {From} - {To}, 车间: {Workshop}", 
                from, to, workshopCode ?? "全部");

            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取生产效率分析失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 获取质量趋势分析
    /// </summary>
    /// <param name="from">开始时间</param>
    /// <param name="to">结束时间</param>
    /// <param name="productModel">产品型号（可选）</param>
    /// <returns>质量趋势分析结果</returns>
    [HttpGet("quality-trend")]
    [ProducesResponseType(typeof(QualityTrendAnalysis), StatusCodes.Status200OK)]
    public async Task<ActionResult<QualityTrendAnalysis>> GetQualityTrendAnalysis(
        [Required] DateTimeOffset from,
        [Required] DateTimeOffset to,
        string? productModel = null)
    {
        try
        {
            var analysis = await _analyticsService.GetQualityTrendAnalysisAsync(from, to, productModel);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取质量趋势分析失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 获取设备健康度分析
    /// </summary>
    /// <param name="equipmentCode">设备代码（可选）</param>
    /// <returns>设备健康度分析结果</returns>
    [HttpGet("equipment-health")]
    [ProducesResponseType(typeof(EquipmentHealthAnalysis), StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentHealthAnalysis>> GetEquipmentHealthAnalysis(
        string? equipmentCode = null)
    {
        try
        {
            var analysis = await _analyticsService.GetEquipmentHealthAnalysisAsync(equipmentCode);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取设备健康度分析失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 获取能耗分析
    /// </summary>
    /// <param name="from">开始时间</param>
    /// <param name="to">结束时间</param>
    /// <param name="workshopCode">车间代码（可选）</param>
    /// <returns>能耗分析结果</returns>
    [HttpGet("energy-consumption")]
    [ProducesResponseType(typeof(EnergyConsumptionAnalysis), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnergyConsumptionAnalysis>> GetEnergyConsumptionAnalysis(
        [Required] DateTimeOffset from,
        [Required] DateTimeOffset to,
        string? workshopCode = null)
    {
        try
        {
            var analysis = await _analyticsService.GetEnergyConsumptionAnalysisAsync(from, to, workshopCode);
            return Ok(analysis);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取能耗分析失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 预测生产产能
    /// </summary>
    /// <param name="productModel">产品型号</param>
    /// <param name="plannedQuantity">计划数量</param>
    /// <param name="targetDate">目标日期</param>
    /// <returns>产能预测结果</returns>
    [HttpPost("predict-capacity")]
    [ProducesResponseType(typeof(CapacityPrediction), StatusCodes.Status200OK)]
    public async Task<ActionResult<CapacityPrediction>> PredictCapacity(
        [Required] string productModel,
        [Required] int plannedQuantity,
        [Required] DateTimeOffset targetDate)
    {
        try
        {
            if (plannedQuantity <= 0)
            {
                return BadRequest(_localizationService.GetString("Analytics.InvalidQuantity"));
            }

            if (targetDate <= DateTimeOffset.UtcNow)
            {
                return BadRequest(_localizationService.GetString("Analytics.InvalidTargetDate"));
            }

            var prediction = await _analyticsService.PredictCapacityAsync(productModel, plannedQuantity, targetDate);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "产能预测失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 异常检测
    /// </summary>
    /// <param name="dataType">数据类型</param>
    /// <param name="from">开始时间</param>
    /// <param name="to">结束时间</param>
    /// <returns>异常检测结果</returns>
    [HttpPost("detect-anomalies")]
    [ProducesResponseType(typeof(AnomalyDetectionResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AnomalyDetectionResult>> DetectAnomalies(
        [Required] string dataType,
        [Required] DateTimeOffset from,
        [Required] DateTimeOffset to)
    {
        try
        {
            var result = await _analyticsService.DetectAnomaliesAsync(dataType, from, to);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "异常检测失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 获取智能制造指标
    /// </summary>
    /// <param name="from">开始时间</param>
    /// <param name="to">结束时间</param>
    /// <returns>智能制造指标</returns>
    [HttpGet("smart-manufacturing-metrics")]
    [ProducesResponseType(typeof(SmartManufacturingMetrics), StatusCodes.Status200OK)]
    public async Task<ActionResult<SmartManufacturingMetrics>> GetSmartManufacturingMetrics(
        [Required] DateTimeOffset from,
        [Required] DateTimeOffset to)
    {
        try
        {
            var metrics = await _analyticsService.GetSmartManufacturingMetricsAsync(from, to);
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取智能制造指标失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 生成优化建议
    /// </summary>
    /// <param name="workshopCode">车间代码</param>
    /// <param name="analysisDate">分析日期</param>
    /// <returns>优化建议</returns>
    [HttpPost("optimization-suggestions")]
    [ProducesResponseType(typeof(OptimizationSuggestions), StatusCodes.Status200OK)]
    public async Task<ActionResult<OptimizationSuggestions>> GenerateOptimizationSuggestions(
        [Required] string workshopCode,
        DateTimeOffset? analysisDate = null)
    {
        try
        {
            var analysisDateTime = analysisDate ?? DateTimeOffset.UtcNow.Date;
            var suggestions = await _analyticsService.GenerateOptimizationSuggestionsAsync(workshopCode, analysisDateTime);
            return Ok(suggestions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成优化建议失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 获取实时分析仪表板
    /// </summary>
    /// <returns>实时分析仪表板数据</returns>
    [HttpGet("real-time-dashboard")]
    [ProducesResponseType(typeof(RealTimeAnalyticsDashboard), StatusCodes.Status200OK)]
    public async Task<ActionResult<RealTimeAnalyticsDashboard>> GetRealTimeAnalyticsDashboard()
    {
        try
        {
            var dashboard = await _analyticsService.GetRealTimeAnalyticsDashboardAsync();
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取实时分析仪表板失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 预测产品质量
    /// </summary>
    /// <param name="batchNumber">批次号</param>
    /// <param name="processParameters">工艺参数</param>
    /// <returns>质量预测结果</returns>
    [HttpPost("predict-quality")]
    [ProducesResponseType(typeof(QualityPrediction), StatusCodes.Status200OK)]
    public async Task<ActionResult<QualityPrediction>> PredictQuality(
        [Required] string batchNumber,
        [Required] Dictionary<string, object> processParameters)
    {
        try
        {
            var prediction = await _analyticsService.PredictQualityAsync(batchNumber, processParameters);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "质量预测失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 训练质量预测模型
    /// </summary>
    /// <param name="trainingData">训练数据</param>
    /// <returns>模型训练结果</returns>
    [HttpPost("ml/train-quality-model")]
    [ProducesResponseType(typeof(ModelTrainingResult), StatusCodes.Status200OK)]
    [Authorize(Roles = "Admin,DataScientist")]
    public async Task<ActionResult<ModelTrainingResult>> TrainQualityPredictionModel(
        [Required] IEnumerable<QualityTrainingData> trainingData)
    {
        try
        {
            var result = await _mlService.TrainQualityPredictionModelAsync(trainingData);
            
            _logger.LogInformation("质量预测模型训练完成: 模型ID {ModelId}, 准确率 {Accuracy}", 
                result.ModelId, result.Accuracy);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "质量预测模型训练失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 预测设备故障
    /// </summary>
    /// <param name="equipmentCode">设备代码</param>
    /// <param name="sensorData">传感器数据</param>
    /// <returns>设备故障预测结果</returns>
    [HttpPost("ml/predict-equipment-failure")]
    [ProducesResponseType(typeof(EquipmentFailurePrediction), StatusCodes.Status200OK)]
    public async Task<ActionResult<EquipmentFailurePrediction>> PredictEquipmentFailure(
        [Required] string equipmentCode,
        [Required] Dictionary<string, object> sensorData)
    {
        try
        {
            var prediction = await _mlService.PredictEquipmentFailureAsync(equipmentCode, sensorData);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备故障预测失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 优化生产参数
    /// </summary>
    /// <param name="processCode">工艺代码</param>
    /// <param name="productModel">产品型号</param>
    /// <param name="currentParameters">当前参数</param>
    /// <returns>参数优化结果</returns>
    [HttpPost("ml/optimize-parameters")]
    [ProducesResponseType(typeof(ParameterOptimizationResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<ParameterOptimizationResult>> OptimizeProductionParameters(
        [Required] string processCode,
        [Required] string productModel,
        [Required] Dictionary<string, object> currentParameters)
    {
        try
        {
            var result = await _mlService.OptimizeProductionParametersAsync(processCode, productModel, currentParameters);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生产参数优化失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 获取模型性能指标
    /// </summary>
    /// <param name="modelId">模型ID</param>
    /// <returns>模型性能指标</returns>
    [HttpGet("ml/model-performance/{modelId}")]
    [ProducesResponseType(typeof(ModelPerformanceMetrics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModelPerformanceMetrics>> GetModelPerformanceMetrics(
        [Required] string modelId)
    {
        try
        {
            var metrics = await _mlService.GetModelPerformanceMetricsAsync(modelId);
            return Ok(metrics);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(_localizationService.GetString("Analytics.ModelNotFound"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取模型性能指标失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 预测生产计划完成情况
    /// </summary>
    /// <param name="planId">计划ID</param>
    /// <returns>生产计划预测结果</returns>
    [HttpPost("predictive/production-plan/{planId}")]
    [ProducesResponseType(typeof(ProductionPlanPrediction), StatusCodes.Status200OK)]
    public async Task<ActionResult<ProductionPlanPrediction>> PredictProductionPlanCompletion(
        [Required] string planId)
    {
        try
        {
            var prediction = await _predictiveService.PredictProductionPlanCompletionAsync(planId);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生产计划预测失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 预测库存需求
    /// </summary>
    /// <param name="materialCode">物料代码</param>
    /// <param name="forecastDays">预测天数</param>
    /// <returns>库存需求预测结果</returns>
    [HttpPost("predictive/inventory-demand")]
    [ProducesResponseType(typeof(InventoryDemandPrediction), StatusCodes.Status200OK)]
    public async Task<ActionResult<InventoryDemandPrediction>> PredictInventoryDemand(
        [Required] string materialCode,
        [Range(1, 365)] int forecastDays = 30)
    {
        try
        {
            var prediction = await _predictiveService.PredictInventoryDemandAsync(materialCode, forecastDays);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "库存需求预测失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 预测设备维护需求
    /// </summary>
    /// <param name="equipmentCode">设备代码</param>
    /// <returns>维护需求预测结果</returns>
    [HttpPost("predictive/maintenance/{equipmentCode}")]
    [ProducesResponseType(typeof(MaintenancePrediction), StatusCodes.Status200OK)]
    public async Task<ActionResult<MaintenancePrediction>> PredictMaintenanceNeeds(
        [Required] string equipmentCode)
    {
        try
        {
            var prediction = await _predictiveService.PredictMaintenanceNeedsAsync(equipmentCode);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "设备维护预测失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 预测能耗
    /// </summary>
    /// <param name="workshopCode">车间代码</param>
    /// <param name="targetDate">目标日期</param>
    /// <returns>能耗预测结果</returns>
    [HttpPost("predictive/energy-consumption")]
    [ProducesResponseType(typeof(EnergyConsumptionPrediction), StatusCodes.Status200OK)]
    public async Task<ActionResult<EnergyConsumptionPrediction>> PredictEnergyConsumption(
        [Required] string workshopCode,
        [Required] DateTimeOffset targetDate)
    {
        try
        {
            var prediction = await _predictiveService.PredictEnergyConsumptionAsync(workshopCode, targetDate);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "能耗预测失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }

    /// <summary>
    /// 预测质量风险
    /// </summary>
    /// <param name="batchNumber">批次号</param>
    /// <param name="processStage">工艺阶段</param>
    /// <returns>质量风险预测结果</returns>
    [HttpPost("predictive/quality-risk")]
    [ProducesResponseType(typeof(QualityRiskPrediction), StatusCodes.Status200OK)]
    public async Task<ActionResult<QualityRiskPrediction>> PredictQualityRisk(
        [Required] string batchNumber,
        [Required] string processStage)
    {
        try
        {
            var prediction = await _predictiveService.PredictQualityRiskAsync(batchNumber, processStage);
            return Ok(prediction);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "质量风险预测失败");
            return StatusCode(500, _localizationService.GetString("Common.InternalServerError"));
        }
    }
} 