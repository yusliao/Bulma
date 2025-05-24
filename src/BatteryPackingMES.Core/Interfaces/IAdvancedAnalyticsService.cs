using BatteryPackingMES.Core.Models.Analytics;

namespace BatteryPackingMES.Core.Interfaces;

/// <summary>
/// 高级分析服务接口
/// </summary>
public interface IAdvancedAnalyticsService
{
    /// <summary>
    /// 获取生产效率分析
    /// </summary>
    Task<ProductionEfficiencyAnalysis> GetProductionEfficiencyAnalysisAsync(
        DateTimeOffset from, 
        DateTimeOffset to, 
        string? workshopCode = null);

    /// <summary>
    /// 获取质量趋势分析
    /// </summary>
    Task<QualityTrendAnalysis> GetQualityTrendAnalysisAsync(
        DateTimeOffset from, 
        DateTimeOffset to, 
        string? productModel = null);

    /// <summary>
    /// 获取设备健康度分析
    /// </summary>
    Task<EquipmentHealthAnalysis> GetEquipmentHealthAnalysisAsync(
        string? equipmentCode = null);

    /// <summary>
    /// 获取能耗分析
    /// </summary>
    Task<EnergyConsumptionAnalysis> GetEnergyConsumptionAnalysisAsync(
        DateTimeOffset from, 
        DateTimeOffset to, 
        string? workshopCode = null);

    /// <summary>
    /// 预测生产产能
    /// </summary>
    Task<CapacityPrediction> PredictCapacityAsync(
        string productModel, 
        int plannedQuantity, 
        DateTimeOffset targetDate);

    /// <summary>
    /// 异常检测
    /// </summary>
    Task<AnomalyDetectionResult> DetectAnomaliesAsync(
        string dataType, 
        DateTimeOffset from, 
        DateTimeOffset to);

    /// <summary>
    /// 获取智能制造指标
    /// </summary>
    Task<SmartManufacturingMetrics> GetSmartManufacturingMetricsAsync(
        DateTimeOffset from, 
        DateTimeOffset to);

    /// <summary>
    /// 生成优化建议
    /// </summary>
    Task<OptimizationSuggestions> GenerateOptimizationSuggestionsAsync(
        string workshopCode, 
        DateTimeOffset analysisDate);

    /// <summary>
    /// 获取实时分析仪表板数据
    /// </summary>
    Task<RealTimeAnalyticsDashboard> GetRealTimeAnalyticsDashboardAsync();

    /// <summary>
    /// 获取产品质量预测
    /// </summary>
    Task<QualityPrediction> PredictQualityAsync(
        string batchNumber, 
        Dictionary<string, object> processParameters);
}

/// <summary>
/// 机器学习服务接口
/// </summary>
public interface IMachineLearningService
{
    /// <summary>
    /// 训练质量预测模型
    /// </summary>
    Task<ModelTrainingResult> TrainQualityPredictionModelAsync(
        IEnumerable<QualityTrainingData> trainingData);

    /// <summary>
    /// 训练设备故障预测模型
    /// </summary>
    Task<ModelTrainingResult> TrainEquipmentFailurePredictionModelAsync(
        IEnumerable<EquipmentTrainingData> trainingData);

    /// <summary>
    /// 预测设备故障
    /// </summary>
    Task<EquipmentFailurePrediction> PredictEquipmentFailureAsync(
        string equipmentCode, 
        Dictionary<string, object> sensorData);

    /// <summary>
    /// 优化生产参数
    /// </summary>
    Task<ParameterOptimizationResult> OptimizeProductionParametersAsync(
        string processCode, 
        string productModel, 
        Dictionary<string, object> currentParameters);

    /// <summary>
    /// 异常模式识别
    /// </summary>
    Task<PatternRecognitionResult> RecognizeAnomalyPatternsAsync(
        IEnumerable<TimeSeriesData> timeSeries);

    /// <summary>
    /// 获取模型性能指标
    /// </summary>
    Task<ModelPerformanceMetrics> GetModelPerformanceMetricsAsync(
        string modelId);
}

/// <summary>
/// 预测分析服务接口
/// </summary>
public interface IPredictiveAnalyticsService
{
    /// <summary>
    /// 预测生产计划完成情况
    /// </summary>
    Task<ProductionPlanPrediction> PredictProductionPlanCompletionAsync(
        string planId);

    /// <summary>
    /// 预测库存需求
    /// </summary>
    Task<InventoryDemandPrediction> PredictInventoryDemandAsync(
        string materialCode, 
        int forecastDays);

    /// <summary>
    /// 预测设备维护需求
    /// </summary>
    Task<MaintenancePrediction> PredictMaintenanceNeedsAsync(
        string equipmentCode);

    /// <summary>
    /// 能耗预测
    /// </summary>
    Task<EnergyConsumptionPrediction> PredictEnergyConsumptionAsync(
        string workshopCode, 
        DateTimeOffset targetDate);

    /// <summary>
    /// 质量风险预测
    /// </summary>
    Task<QualityRiskPrediction> PredictQualityRiskAsync(
        string batchNumber, 
        string processStage);
} 