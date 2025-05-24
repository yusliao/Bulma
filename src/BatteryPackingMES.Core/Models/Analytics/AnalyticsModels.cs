namespace BatteryPackingMES.Core.Models.Analytics;

/// <summary>
/// 生产效率分析结果
/// </summary>
public class ProductionEfficiencyAnalysis
{
    public DateTimeOffset AnalysisDate { get; set; }
    public DateTimeOffset PeriodFrom { get; set; }
    public DateTimeOffset PeriodTo { get; set; }
    public string? WorkshopCode { get; set; }
    
    /// <summary>
    /// 总体效率指标
    /// </summary>
    public OverallEfficiencyMetrics Overall { get; set; } = new();
    
    /// <summary>
    /// 设备综合效率 (OEE)
    /// </summary>
    public OEEMetrics OEE { get; set; } = new();
    
    /// <summary>
    /// 产线效率对比
    /// </summary>
    public List<ProductionLineEfficiency> ProductionLines { get; set; } = new();
    
    /// <summary>
    /// 趋势数据
    /// </summary>
    public List<EfficiencyTrendPoint> TrendData { get; set; } = new();
    
    /// <summary>
    /// 效率影响因素分析
    /// </summary>
    public EfficiencyFactorAnalysis FactorAnalysis { get; set; } = new();
}

/// <summary>
/// 总体效率指标
/// </summary>
public class OverallEfficiencyMetrics
{
    public decimal ProductionEfficiency { get; set; }
    public decimal QualityRate { get; set; }
    public decimal OnTimeDeliveryRate { get; set; }
    public decimal ResourceUtilizationRate { get; set; }
    public decimal CostEfficiency { get; set; }
    public string EfficiencyGrade { get; set; } = string.Empty;
    public List<string> ImprovementAreas { get; set; } = new();
}

/// <summary>
/// OEE指标
/// </summary>
public class OEEMetrics
{
    public decimal OverallOEE { get; set; }
    public decimal Availability { get; set; }
    public decimal Performance { get; set; }
    public decimal Quality { get; set; }
    public decimal PlannedProductionTime { get; set; }
    public decimal ActualProductionTime { get; set; }
    public decimal IdealCycleTime { get; set; }
    public decimal ActualCycleTime { get; set; }
    public int TotalProduced { get; set; }
    public int QualifiedProduced { get; set; }
}

/// <summary>
/// 质量趋势分析
/// </summary>
public class QualityTrendAnalysis
{
    public DateTimeOffset AnalysisDate { get; set; }
    public DateTimeOffset PeriodFrom { get; set; }
    public DateTimeOffset PeriodTo { get; set; }
    public string? ProductModel { get; set; }
    
    /// <summary>
    /// 质量指标汇总
    /// </summary>
    public QualityMetricsSummary Summary { get; set; } = new();
    
    /// <summary>
    /// 缺陷分析
    /// </summary>
    public DefectAnalysis DefectAnalysis { get; set; } = new();
    
    /// <summary>
    /// 质量趋势数据
    /// </summary>
    public List<QualityTrendPoint> TrendData { get; set; } = new();
    
    /// <summary>
    /// 质量控制图数据
    /// </summary>
    public QualityControlChart ControlChart { get; set; } = new();
    
    /// <summary>
    /// 质量预测
    /// </summary>
    public QualityForecast Forecast { get; set; } = new();
}

/// <summary>
/// 设备健康度分析
/// </summary>
public class EquipmentHealthAnalysis
{
    public DateTimeOffset AnalysisDate { get; set; }
    public string? EquipmentCode { get; set; }
    
    /// <summary>
    /// 健康度评分 (0-100)
    /// </summary>
    public decimal HealthScore { get; set; }
    
    /// <summary>
    /// 健康度等级
    /// </summary>
    public string HealthGrade { get; set; } = string.Empty;
    
    /// <summary>
    /// 设备状态指标
    /// </summary>
    public EquipmentStatusMetrics StatusMetrics { get; set; } = new();
    
    /// <summary>
    /// 故障预测
    /// </summary>
    public EquipmentFailurePrediction FailurePrediction { get; set; } = new();
    
    /// <summary>
    /// 维护建议
    /// </summary>
    public List<MaintenanceRecommendation> MaintenanceRecommendations { get; set; } = new();
    
    /// <summary>
    /// 历史健康度趋势
    /// </summary>
    public List<HealthTrendPoint> HealthTrend { get; set; } = new();
}

/// <summary>
/// 能耗分析
/// </summary>
public class EnergyConsumptionAnalysis
{
    public DateTimeOffset AnalysisDate { get; set; }
    public DateTimeOffset PeriodFrom { get; set; }
    public DateTimeOffset PeriodTo { get; set; }
    public string? WorkshopCode { get; set; }
    
    /// <summary>
    /// 能耗汇总
    /// </summary>
    public EnergyConsumptionSummary Summary { get; set; } = new();
    
    /// <summary>
    /// 分时段能耗
    /// </summary>
    public List<HourlyEnergyConsumption> HourlyConsumption { get; set; } = new();
    
    /// <summary>
    /// 设备能耗排名
    /// </summary>
    public List<EquipmentEnergyRanking> EquipmentRanking { get; set; } = new();
    
    /// <summary>
    /// 能耗效率指标
    /// </summary>
    public EnergyEfficiencyMetrics EfficiencyMetrics { get; set; } = new();
    
    /// <summary>
    /// 节能建议
    /// </summary>
    public List<EnergySavingRecommendation> SavingRecommendations { get; set; } = new();
}

/// <summary>
/// 产能预测
/// </summary>
public class CapacityPrediction
{
    public DateTimeOffset PredictionDate { get; set; }
    public string ProductModel { get; set; } = string.Empty;
    public int PlannedQuantity { get; set; }
    public DateTimeOffset TargetDate { get; set; }
    
    /// <summary>
    /// 预测产能
    /// </summary>
    public int PredictedCapacity { get; set; }
    
    /// <summary>
    /// 完成概率
    /// </summary>
    public decimal CompletionProbability { get; set; }
    
    /// <summary>
    /// 预计完成时间
    /// </summary>
    public DateTimeOffset EstimatedCompletionDate { get; set; }
    
    /// <summary>
    /// 资源需求预测
    /// </summary>
    public ResourceRequirementPrediction ResourceRequirement { get; set; } = new();
    
    /// <summary>
    /// 风险因素
    /// </summary>
    public List<RiskFactor> RiskFactors { get; set; } = new();
    
    /// <summary>
    /// 置信度
    /// </summary>
    public decimal ConfidenceLevel { get; set; }
}

/// <summary>
/// 异常检测结果
/// </summary>
public class AnomalyDetectionResult
{
    public DateTimeOffset DetectionDate { get; set; }
    public string DataType { get; set; } = string.Empty;
    public DateTimeOffset PeriodFrom { get; set; }
    public DateTimeOffset PeriodTo { get; set; }
    
    /// <summary>
    /// 检测到的异常
    /// </summary>
    public List<DetectedAnomaly> DetectedAnomalies { get; set; } = new();
    
    /// <summary>
    /// 异常统计
    /// </summary>
    public AnomalyStatistics Statistics { get; set; } = new();
    
    /// <summary>
    /// 异常模式分析
    /// </summary>
    public AnomalyPatternAnalysis PatternAnalysis { get; set; } = new();
}

/// <summary>
/// 智能制造指标
/// </summary>
public class SmartManufacturingMetrics
{
    public DateTimeOffset MetricsDate { get; set; }
    public DateTimeOffset PeriodFrom { get; set; }
    public DateTimeOffset PeriodTo { get; set; }
    
    /// <summary>
    /// 数字化程度
    /// </summary>
    public decimal DigitalizationLevel { get; set; }
    
    /// <summary>
    /// 自动化程度
    /// </summary>
    public decimal AutomationLevel { get; set; }
    
    /// <summary>
    /// 智能化程度
    /// </summary>
    public decimal IntelligenceLevel { get; set; }
    
    /// <summary>
    /// 连接性指标
    /// </summary>
    public ConnectivityMetrics Connectivity { get; set; } = new();
    
    /// <summary>
    /// 数据质量指标
    /// </summary>
    public DataQualityMetrics DataQuality { get; set; } = new();
    
    /// <summary>
    /// 预测性维护覆盖率
    /// </summary>
    public decimal PredictiveMaintenanceCoverage { get; set; }
    
    /// <summary>
    /// 实时决策支持覆盖率
    /// </summary>
    public decimal RealTimeDecisionSupportCoverage { get; set; }
}

/// <summary>
/// 优化建议
/// </summary>
public class OptimizationSuggestions
{
    public DateTimeOffset SuggestionDate { get; set; }
    public string WorkshopCode { get; set; } = string.Empty;
    public DateTimeOffset AnalysisDate { get; set; }
    
    /// <summary>
    /// 生产优化建议
    /// </summary>
    public List<ProductionOptimizationSuggestion> ProductionSuggestions { get; set; } = new();
    
    /// <summary>
    /// 质量优化建议
    /// </summary>
    public List<QualityOptimizationSuggestion> QualitySuggestions { get; set; } = new();
    
    /// <summary>
    /// 设备优化建议
    /// </summary>
    public List<EquipmentOptimizationSuggestion> EquipmentSuggestions { get; set; } = new();
    
    /// <summary>
    /// 能耗优化建议
    /// </summary>
    public List<EnergyOptimizationSuggestion> EnergySuggestions { get; set; } = new();
    
    /// <summary>
    /// 预期收益
    /// </summary>
    public OptimizationBenefit ExpectedBenefit { get; set; } = new();
}

/// <summary>
/// 实时分析仪表板
/// </summary>
public class RealTimeAnalyticsDashboard
{
    public DateTimeOffset LastUpdated { get; set; }
    
    /// <summary>
    /// 实时生产状态
    /// </summary>
    public RealTimeProductionStatus ProductionStatus { get; set; } = new();
    
    /// <summary>
    /// 实时质量指标
    /// </summary>
    public RealTimeQualityMetrics QualityMetrics { get; set; } = new();
    
    /// <summary>
    /// 实时设备状态
    /// </summary>
    public RealTimeEquipmentStatus EquipmentStatus { get; set; } = new();
    
    /// <summary>
    /// 实时告警
    /// </summary>
    public List<RealTimeAlert> ActiveAlerts { get; set; } = new();
    
    /// <summary>
    /// 关键性能指标
    /// </summary>
    public List<KPIMetric> KPIs { get; set; } = new();
}

/// <summary>
/// 质量预测
/// </summary>
public class QualityPrediction
{
    public DateTimeOffset PredictionDate { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// 预测合格率
    /// </summary>
    public decimal PredictedQualificationRate { get; set; }
    
    /// <summary>
    /// 质量风险等级
    /// </summary>
    public string QualityRiskLevel { get; set; } = string.Empty;
    
    /// <summary>
    /// 影响因素
    /// </summary>
    public List<QualityInfluenceFactor> InfluenceFactors { get; set; } = new();
    
    /// <summary>
    /// 改进建议
    /// </summary>
    public List<QualityImprovementSuggestion> ImprovementSuggestions { get; set; } = new();
    
    /// <summary>
    /// 预测置信度
    /// </summary>
    public decimal ConfidenceLevel { get; set; }
}

// 支持类型定义
public class ProductionLineEfficiency
{
    public string LineCode { get; set; } = string.Empty;
    public decimal Efficiency { get; set; }
    public int ProducedQuantity { get; set; }
    public decimal UtilizationRate { get; set; }
}

public class EfficiencyTrendPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal Efficiency { get; set; }
    public decimal QualityRate { get; set; }
}

public class EfficiencyFactorAnalysis
{
    public Dictionary<string, decimal> FactorWeights { get; set; } = new();
    public List<string> BottleneckProcesses { get; set; } = new();
    public List<string> ImprovementOpportunities { get; set; } = new();
}

public class QualityMetricsSummary
{
    public decimal OverallQualificationRate { get; set; }
    public decimal FirstPassYield { get; set; }
    public decimal ReworkRate { get; set; }
    public decimal ScrapRate { get; set; }
    public decimal CustomerComplaintRate { get; set; }
}

public class DefectAnalysis
{
    public List<DefectType> TopDefects { get; set; } = new();
    public List<DefectRoot> RootCauses { get; set; } = new();
    public Dictionary<string, decimal> DefectDistribution { get; set; } = new();
}

public class DefectType
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Percentage { get; set; }
    public string Severity { get; set; } = string.Empty;
}

public class DefectRoot
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ImpactPercentage { get; set; }
}

public class QualityTrendPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal QualificationRate { get; set; }
    public decimal DefectRate { get; set; }
    public int TotalProduced { get; set; }
}

public class QualityControlChart
{
    public List<ControlPoint> ControlPoints { get; set; } = new();
    public decimal UpperControlLimit { get; set; }
    public decimal LowerControlLimit { get; set; }
    public decimal CenterLine { get; set; }
    public List<string> OutOfControlPoints { get; set; } = new();
}

public class ControlPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal Value { get; set; }
    public bool IsOutOfControl { get; set; }
}

public class QualityForecast
{
    public List<QualityForecastPoint> ForecastPoints { get; set; } = new();
    public decimal ConfidenceInterval { get; set; }
    public string TrendDirection { get; set; } = string.Empty;
}

public class QualityForecastPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal PredictedQualificationRate { get; set; }
    public decimal UpperBound { get; set; }
    public decimal LowerBound { get; set; }
}

// 其他支持类型...
public class EquipmentStatusMetrics
{
    public decimal Availability { get; set; }
    public decimal MTBF { get; set; } // Mean Time Between Failures
    public decimal MTTR { get; set; } // Mean Time To Repair
    public int TotalFailures { get; set; }
    public decimal VibrationLevel { get; set; }
    public decimal Temperature { get; set; }
    public decimal EnergyConsumption { get; set; }
}

public class EquipmentFailurePrediction
{
    public decimal FailureProbability { get; set; }
    public DateTimeOffset PredictedFailureDate { get; set; }
    public string FailureType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public List<string> WarningSignals { get; set; } = new();
}

public class MaintenanceRecommendation
{
    public string RecommendationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public DateTimeOffset RecommendedDate { get; set; }
    public decimal EstimatedCost { get; set; }
    public decimal ExpectedBenefit { get; set; }
}

public class HealthTrendPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal HealthScore { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class EnergyConsumptionSummary
{
    public decimal TotalConsumption { get; set; }
    public decimal AverageConsumption { get; set; }
    public decimal PeakConsumption { get; set; }
    public decimal OffPeakConsumption { get; set; }
    public decimal ConsumptionPerUnit { get; set; }
    public decimal CostPerUnit { get; set; }
    public decimal TotalCost { get; set; }
}

public class HourlyEnergyConsumption
{
    public DateTime Hour { get; set; }
    public decimal Consumption { get; set; }
    public decimal Cost { get; set; }
    public decimal Efficiency { get; set; }
}

public class EquipmentEnergyRanking
{
    public string EquipmentCode { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public decimal Consumption { get; set; }
    public decimal Percentage { get; set; }
    public int Ranking { get; set; }
}

public class EnergyEfficiencyMetrics
{
    public decimal OverallEfficiency { get; set; }
    public decimal ProductionEnergyRatio { get; set; }
    public decimal WasteEnergyPercentage { get; set; }
    public decimal BenchmarkComparison { get; set; }
}

public class EnergySavingRecommendation
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PotentialSaving { get; set; }
    public decimal ImplementationCost { get; set; }
    public decimal ROI { get; set; }
    public string Priority { get; set; } = string.Empty;
}

public class ResourceRequirementPrediction
{
    public List<MaterialRequirement> Materials { get; set; } = new();
    public List<LaborRequirement> Labor { get; set; } = new();
    public List<EquipmentRequirement> Equipment { get; set; } = new();
}

public class MaterialRequirement
{
    public string MaterialCode { get; set; } = string.Empty;
    public string MaterialName { get; set; } = string.Empty;
    public decimal RequiredQuantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTimeOffset RequiredDate { get; set; }
}

public class LaborRequirement
{
    public string SkillType { get; set; } = string.Empty;
    public int RequiredPersons { get; set; }
    public decimal RequiredHours { get; set; }
    public DateTimeOffset RequiredFrom { get; set; }
    public DateTimeOffset RequiredTo { get; set; }
}

public class EquipmentRequirement
{
    public string EquipmentType { get; set; } = string.Empty;
    public int RequiredUnits { get; set; }
    public decimal RequiredHours { get; set; }
    public DateTimeOffset RequiredFrom { get; set; }
    public DateTimeOffset RequiredTo { get; set; }
}

public class RiskFactor
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Impact { get; set; }
    public decimal Probability { get; set; }
    public string MitigationStrategy { get; set; } = string.Empty;
}

public class DetectedAnomaly
{
    public DateTimeOffset Timestamp { get; set; }
    public string AnomalyType { get; set; } = string.Empty;
    public string DataSource { get; set; } = string.Empty;
    public decimal Severity { get; set; }
    public decimal AnomalyScore { get; set; }
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> AnomalyData { get; set; } = new();
}

public class AnomalyStatistics
{
    public int TotalAnomalies { get; set; }
    public int HighSeverityAnomalies { get; set; }
    public int MediumSeverityAnomalies { get; set; }
    public int LowSeverityAnomalies { get; set; }
    public decimal AnomalyRate { get; set; }
    public List<string> AnomalyTypes { get; set; } = new();
}

public class AnomalyPatternAnalysis
{
    public List<AnomalyPattern> IdentifiedPatterns { get; set; } = new();
    public List<AnomalyCorrelation> Correlations { get; set; } = new();
    public string TrendDescription { get; set; } = string.Empty;
}

public class AnomalyPattern
{
    public string PatternType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Frequency { get; set; }
    public List<string> TriggerConditions { get; set; } = new();
}

public class AnomalyCorrelation
{
    public string Factor1 { get; set; } = string.Empty;
    public string Factor2 { get; set; } = string.Empty;
    public decimal CorrelationCoefficient { get; set; }
    public string RelationshipType { get; set; } = string.Empty;
}

public class ConnectivityMetrics
{
    public decimal DeviceConnectivityRate { get; set; }
    public decimal DataIntegrationLevel { get; set; }
    public decimal SystemInteroperability { get; set; }
    public int ConnectedDevices { get; set; }
    public int TotalDevices { get; set; }
}

public class DataQualityMetrics
{
    public decimal Completeness { get; set; }
    public decimal Accuracy { get; set; }
    public decimal Consistency { get; set; }
    public decimal Timeliness { get; set; }
    public decimal Validity { get; set; }
    public decimal OverallDataQuality { get; set; }
}

public class ProductionOptimizationSuggestion
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedImprovement { get; set; }
    public decimal ImplementationCost { get; set; }
    public string ImplementationTimeframe { get; set; } = string.Empty;
}

public class QualityOptimizationSuggestion
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedQualityImprovement { get; set; }
    public decimal ImplementationCost { get; set; }
}

public class EquipmentOptimizationSuggestion
{
    public string EquipmentCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedEfficiencyGain { get; set; }
    public decimal ImplementationCost { get; set; }
}

public class EnergyOptimizationSuggestion
{
    public string Category { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public decimal ExpectedEnergySaving { get; set; }
    public decimal ImplementationCost { get; set; }
    public decimal PaybackPeriod { get; set; }
}

public class OptimizationBenefit
{
    public decimal TotalExpectedSaving { get; set; }
    public decimal EfficiencyGain { get; set; }
    public decimal QualityImprovement { get; set; }
    public decimal EnergySaving { get; set; }
    public decimal ROI { get; set; }
    public string PaybackPeriod { get; set; } = string.Empty;
}

public class RealTimeProductionStatus
{
    public int ActiveLines { get; set; }
    public int TotalLines { get; set; }
    public int TodayProduction { get; set; }
    public int PlannedProduction { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal EfficiencyRate { get; set; }
}

public class RealTimeQualityMetrics
{
    public decimal CurrentQualificationRate { get; set; }
    public decimal TodayQualificationRate { get; set; }
    public int DefectsToday { get; set; }
    public int InspectionsToday { get; set; }
    public string QualityTrend { get; set; } = string.Empty;
}

public class RealTimeEquipmentStatus
{
    public int RunningEquipment { get; set; }
    public int IdleEquipment { get; set; }
    public int MaintenanceEquipment { get; set; }
    public int FaultEquipment { get; set; }
    public decimal OverallAvailability { get; set; }
}

public class RealTimeAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public bool IsAcknowledged { get; set; }
}

public class KPIMetric
{
    public string Name { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal TargetValue { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TrendPercentage { get; set; }
}

public class QualityInfluenceFactor
{
    public string FactorName { get; set; } = string.Empty;
    public decimal InfluenceWeight { get; set; }
    public string InfluenceDirection { get; set; } = string.Empty;
    public decimal CurrentValue { get; set; }
    public decimal OptimalValue { get; set; }
}

public class QualityImprovementSuggestion
{
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal ExpectedImprovement { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string ImplementationGuidance { get; set; } = string.Empty;
}

// 机器学习相关模型
public class ModelTrainingResult
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public decimal Accuracy { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1Score { get; set; }
    public DateTimeOffset TrainingDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TrainingLog { get; set; } = string.Empty;
}

public class QualityTrainingData
{
    public string BatchNumber { get; set; } = string.Empty;
    public Dictionary<string, object> ProcessParameters { get; set; } = new();
    public bool QualityResult { get; set; }
    public List<string> DefectTypes { get; set; } = new();
    public DateTimeOffset ProductionDate { get; set; }
}

public class EquipmentTrainingData
{
    public string EquipmentCode { get; set; } = string.Empty;
    public Dictionary<string, object> SensorData { get; set; } = new();
    public bool FailureOccurred { get; set; }
    public string FailureType { get; set; } = string.Empty;
    public DateTimeOffset DataTimestamp { get; set; }
}

public class ParameterOptimizationResult
{
    public Dictionary<string, object> OptimizedParameters { get; set; } = new();
    public decimal ExpectedImprovement { get; set; }
    public decimal ConfidenceLevel { get; set; }
    public List<ParameterRecommendation> Recommendations { get; set; } = new();
}

public class ParameterRecommendation
{
    public string ParameterName { get; set; } = string.Empty;
    public object CurrentValue { get; set; } = new();
    public object RecommendedValue { get; set; } = new();
    public string Reason { get; set; } = string.Empty;
    public decimal ImpactScore { get; set; }
}

public class PatternRecognitionResult
{
    public List<IdentifiedPattern> Patterns { get; set; } = new();
    public decimal ConfidenceScore { get; set; }
    public string PatternType { get; set; } = string.Empty;
    public List<PatternCharacteristic> Characteristics { get; set; } = new();
}

public class IdentifiedPattern
{
    public string PatternId { get; set; } = string.Empty;
    public string PatternName { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; }
    public DateTimeOffset EndTime { get; set; }
    public decimal Confidence { get; set; }
    public Dictionary<string, object> PatternData { get; set; } = new();
}

public class PatternCharacteristic
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Strength { get; set; }
    public List<string> AssociatedFactors { get; set; } = new();
}

public class ModelPerformanceMetrics
{
    public string ModelId { get; set; } = string.Empty;
    public string ModelType { get; set; } = string.Empty;
    public decimal Accuracy { get; set; }
    public decimal Precision { get; set; }
    public decimal Recall { get; set; }
    public decimal F1Score { get; set; }
    public decimal AUC { get; set; }
    public DateTimeOffset LastEvaluated { get; set; }
    public int PredictionCount { get; set; }
    public decimal AveragePredictionTime { get; set; }
}

public class TimeSeriesData
{
    public DateTimeOffset Timestamp { get; set; }
    public Dictionary<string, object> Values { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

// 预测相关模型
public class ProductionPlanPrediction
{
    public string PlanId { get; set; } = string.Empty;
    public decimal CompletionProbability { get; set; }
    public DateTimeOffset PredictedCompletionDate { get; set; }
    public List<PlanRisk> IdentifiedRisks { get; set; } = new();
    public List<string> RecommendedActions { get; set; } = new();
}

public class PlanRisk
{
    public string RiskType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Probability { get; set; }
    public decimal Impact { get; set; }
    public string MitigationStrategy { get; set; } = string.Empty;
}

public class InventoryDemandPrediction
{
    public string MaterialCode { get; set; } = string.Empty;
    public List<DemandForecast> DemandForecast { get; set; } = new();
    public decimal RecommendedStockLevel { get; set; }
    public DateTimeOffset NextOrderDate { get; set; }
    public decimal OrderQuantity { get; set; }
}

public class DemandForecast
{
    public DateTimeOffset Date { get; set; }
    public decimal PredictedDemand { get; set; }
    public decimal ConfidenceInterval { get; set; }
    public List<string> InfluencingFactors { get; set; } = new();
}

public class MaintenancePrediction
{
    public string EquipmentCode { get; set; } = string.Empty;
    public List<MaintenanceSchedule> RecommendedSchedule { get; set; } = new();
    public decimal FailureRisk { get; set; }
    public decimal MaintenanceCost { get; set; }
    public decimal DowntimeCost { get; set; }
}

public class MaintenanceSchedule
{
    public string MaintenanceType { get; set; } = string.Empty;
    public DateTimeOffset RecommendedDate { get; set; }
    public string Priority { get; set; } = string.Empty;
    public decimal EstimatedDuration { get; set; }
    public decimal EstimatedCost { get; set; }
    public List<string> RequiredResources { get; set; } = new();
}

public class EnergyConsumptionPrediction
{
    public string WorkshopCode { get; set; } = string.Empty;
    public DateTimeOffset TargetDate { get; set; }
    public decimal PredictedConsumption { get; set; }
    public decimal PredictedCost { get; set; }
    public List<EnergyForecastPoint> HourlyForecast { get; set; } = new();
    public List<string> OptimizationOpportunities { get; set; } = new();
}

public class EnergyForecastPoint
{
    public DateTimeOffset Timestamp { get; set; }
    public decimal PredictedConsumption { get; set; }
    public decimal PredictedCost { get; set; }
    public decimal ConfidenceLevel { get; set; }
}

public class QualityRiskPrediction
{
    public string BatchNumber { get; set; } = string.Empty;
    public string ProcessStage { get; set; } = string.Empty;
    public decimal QualityRiskScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public List<QualityRiskFactor> RiskFactors { get; set; } = new();
    public List<string> PreventiveActions { get; set; } = new();
}

public class QualityRiskFactor
{
    public string FactorName { get; set; } = string.Empty;
    public decimal RiskContribution { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
} 