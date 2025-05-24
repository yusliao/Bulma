# 工艺路线管理功能使用指南

## 概述

本文档介绍了BatteryPackingMES系统中新增的工艺路线管理功能，包括工艺路线设计、可视化编辑、参数管理等核心功能。

## 功能模块

### 1. 工艺路线管理 (ProcessRouteController)

#### 核心功能
- 工艺路线CRUD操作
- 路线复制与版本管理
- 步骤管理
- 状态控制
- 统计分析

#### 主要API接口

##### 1.1 分页查询工艺路线
```http
GET /api/v2.0/process-routes/paged?pageIndex=1&pageSize=20&productType=电芯&isEnabled=true&keyword=标准

Response:
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 1,
        "routeCode": "ROUTE_CELL_001",
        "routeName": "电芯包装标准工艺",
        "productType": "电芯",
        "description": "适用于18650电芯包装",
        "isEnabled": true,
        "versionNumber": "1.0",
        "stepCount": 3,
        "createdTime": "2024-01-15T10:30:00Z",
        "updatedTime": "2024-01-15T15:20:00Z"
      }
    ],
    "total": 1,
    "pageIndex": 1,
    "pageSize": 20
  }
}
```

##### 1.2 获取工艺路线详情
```http
GET /api/v2.0/process-routes/1

Response:
{
  "success": true,
  "data": {
    "id": 1,
    "routeCode": "ROUTE_CELL_001",
    "routeName": "电芯包装标准工艺",
    "productType": "电芯",
    "description": "适用于18650电芯包装",
    "isEnabled": true,
    "versionNumber": "1.0",
    "routeConfig": "{\"nodes\":[...],\"edges\":[...]}",
    "steps": [
      {
        "id": 1,
        "processId": 10,
        "processCode": "INSPECT_001",
        "processName": "外观检验",
        "processType": "CellPacking",
        "stepOrder": 1,
        "isRequired": true,
        "standardTime": 30.5
      }
    ]
  }
}
```

##### 1.3 创建工艺路线
```http
POST /api/v2.0/process-routes
Content-Type: application/json

{
  "routeCode": "ROUTE_MODULE_001",
  "routeName": "模组包装标准工艺",
  "productType": "模组",
  "description": "适用于标准模组包装流程",
  "isEnabled": true,
  "versionNumber": "1.0",
  "routeConfig": "{\"nodes\":[...],\"edges\":[...]}",
  "steps": [
    {
      "processId": 10,
      "stepOrder": 1,
      "isRequired": true,
      "stepConfig": "{\"temperature\": {\"min\": 20, \"max\": 25}}"
    }
  ]
}
```

##### 1.4 复制工艺路线
```http
POST /api/v2.0/process-routes/1/copy
Content-Type: application/json

{
  "newRouteCode": "ROUTE_CELL_002",
  "newRouteName": "电芯包装改进工艺",
  "description": "基于标准工艺的改进版本"
}
```

##### 1.5 更新工艺路线步骤
```http
PUT /api/v2.0/process-routes/1/steps
Content-Type: application/json

{
  "steps": [
    {
      "processId": 10,
      "stepOrder": 1,
      "isRequired": true,
      "stepConfig": "{\"temperature\": {\"min\": 18, \"max\": 22}}"
    },
    {
      "processId": 11,
      "stepOrder": 2,
      "isRequired": true,
      "stepConfig": "{\"pressure\": {\"min\": 0.8, \"max\": 1.2}}"
    }
  ]
}
```

### 2. 可视化工艺路线设计器 (ProcessRouteDesignerController)

#### 核心功能
- 拖拽式流程图设计
- 实时配置验证
- 模板应用
- 配置导入导出

#### 主要API接口

##### 2.1 获取设计器配置
```http
GET /api/v2.0/process-route-designer/1/designer-config

Response:
{
  "success": true,
  "data": {
    "routeId": 1,
    "routeCode": "ROUTE_CELL_001",
    "routeName": "电芯包装标准工艺",
    "productType": "电芯",
    "flowChartConfig": {
      "nodes": [
        {
          "id": "start",
          "type": "start",
          "label": "开始",
          "position": { "x": 100, "y": 100 }
        },
        {
          "id": "process_1",
          "type": "process",
          "label": "外观检验",
          "position": { "x": 250, "y": 100 },
          "processId": 10
        }
      ],
      "edges": [
        {
          "id": "e1",
          "source": "start",
          "target": "process_1"
        }
      ],
      "viewPort": { "x": 0, "y": 0, "zoom": 1 }
    },
    "availableProcesses": [
      {
        "id": "10",
        "processId": 10,
        "processCode": "INSPECT_001",
        "processName": "外观检验",
        "processType": "CellPacking",
        "standardTime": 30.5,
        "icon": "battery",
        "color": "#FF6B6B"
      }
    ]
  }
}
```

##### 2.2 保存设计器配置
```http
PUT /api/v2.0/process-route-designer/1/designer-config
Content-Type: application/json

{
  "flowChartConfig": {
    "nodes": [
      {
        "id": "start",
        "type": "start",
        "label": "开始",
        "position": { "x": 100, "y": 100 }
      },
      {
        "id": "process_1",
        "type": "process",
        "label": "外观检验",
        "position": { "x": 250, "y": 100 },
        "processId": 10
      },
      {
        "id": "end",
        "type": "end",
        "label": "结束",
        "position": { "x": 400, "y": 100 }
      }
    ],
    "edges": [
      {
        "id": "e1",
        "source": "start",
        "target": "process_1"
      },
      {
        "id": "e2",
        "source": "process_1",
        "target": "end"
      }
    ],
    "viewPort": { "x": 0, "y": 0, "zoom": 1 }
  }
}
```

##### 2.3 验证流程图配置
```http
POST /api/v2.0/process-route-designer/validate
Content-Type: application/json

{
  "flowChartConfig": {
    "nodes": [...],
    "edges": [...],
    "viewPort": { "x": 0, "y": 0, "zoom": 1 }
  }
}

Response:
{
  "success": true,
  "data": {
    "isValid": true,
    "errors": [],
    "warnings": [
      "建议添加质量检测节点"
    ]
  }
}
```

##### 2.4 获取预定义模板
```http
GET /api/v2.0/process-route-designer/templates?productType=电芯

Response:
{
  "success": true,
  "data": [
    {
      "id": "template_cell_packing",
      "templateName": "电芯包装标准工艺",
      "productType": "电芯",
      "description": "适用于标准电芯包装流程",
      "flowChartConfig": {
        "nodes": [...],
        "edges": [...]
      }
    }
  ]
}
```

##### 2.5 应用模板
```http
POST /api/v2.0/process-route-designer/1/apply-template
Content-Type: application/json

{
  "templateId": "template_cell_packing"
}
```

### 3. 工艺参数管理 (ProcessParameterController)

#### 核心功能
- 参数批量记录
- 实时数据监控
- 统计分析
- 预警管理
- 数据导出

#### 主要API接口

##### 3.1 批量记录工艺参数
```http
POST /api/v2.0/process-parameters/batch-record
Content-Type: application/json

{
  "parameters": [
    {
      "processId": 10,
      "parameterName": "温度",
      "parameterValue": 23.5,
      "parameterUnit": "℃",
      "standardValue": 23.0,
      "upperLimit": 25.0,
      "lowerLimit": 20.0,
      "recordTime": "2024-01-15T10:30:00Z",
      "equipmentId": 1001,
      "batchNumber": "BATCH20240115001",
      "productBarcode": "BT001202401150001"
    }
  ]
}

Response:
{
  "success": true,
  "data": {
    "results": [
      {
        "parameterName": "温度",
        "success": true,
        "parameterId": 12345
      }
    ],
    "successCount": 1,
    "failureCount": 0,
    "totalCount": 1
  }
}
```

##### 3.2 获取参数统计分析
```http
GET /api/v2.0/process-parameters/statistics?processId=10&parameterName=温度&startTime=2024-01-01&endTime=2024-01-15

Response:
{
  "success": true,
  "data": {
    "processId": 10,
    "parameterName": "温度",
    "totalCount": 1000,
    "qualifiedCount": 985,
    "unqualifiedCount": 15,
    "qualificationRate": 98.5,
    "minValue": 20.1,
    "maxValue": 24.8,
    "averageValue": 22.8,
    "standardDeviation": 1.2,
    "parameterUnit": "℃",
    "standardValue": 23.0,
    "upperLimit": 25.0,
    "lowerLimit": 20.0,
    "trendData": [
      {
        "date": "2024-01-15",
        "averageValue": 22.8,
        "minValue": 21.5,
        "maxValue": 24.2,
        "count": 50,
        "qualifiedCount": 48
      }
    ]
  }
}
```

##### 3.3 获取实时参数数据
```http
GET /api/v2.0/process-parameters/realtime?processId=10&parameterNames=温度,压力&minutes=30

Response:
{
  "success": true,
  "data": [
    {
      "parameterName": "温度",
      "latestValue": 23.2,
      "latestRecordTime": "2024-01-15T10:30:00Z",
      "parameterUnit": "℃",
      "isQualified": true,
      "changeRate": 0.5,
      "dataPoints": [
        {
          "value": 23.0,
          "timestamp": "2024-01-15T10:00:00Z",
          "isQualified": true
        },
        {
          "value": 23.2,
          "timestamp": "2024-01-15T10:30:00Z",
          "isQualified": true
        }
      ]
    }
  ]
}
```

##### 3.4 获取参数预警信息
```http
GET /api/v2.0/process-parameters/alerts?processId=10&hours=24

Response:
{
  "success": true,
  "data": [
    {
      "processId": 10,
      "parameterName": "温度",
      "alertCount": 3,
      "latestAlertTime": "2024-01-15T10:30:00Z",
      "latestValue": 26.5,
      "standardValue": 23.0,
      "upperLimit": 25.0,
      "lowerLimit": 20.0,
      "parameterUnit": "℃",
      "alertLevel": "High"
    }
  ]
}
```

##### 3.5 设置参数标准值
```http
POST /api/v2.0/process-parameters/set-standards
Content-Type: application/json

{
  "processId": 10,
  "parameterName": "温度",
  "standardValue": 23.0,
  "upperLimit": 25.0,
  "lowerLimit": 20.0
}
```

## 使用场景

### 场景1：创建新的工艺路线

1. **创建基础路线**
   ```http
   POST /api/v2.0/process-routes
   ```

2. **使用设计器配置流程图**
   ```http
   GET /api/v2.0/process-route-designer/{id}/designer-config
   PUT /api/v2.0/process-route-designer/{id}/designer-config
   ```

3. **配置工艺步骤**
   ```http
   PUT /api/v2.0/process-routes/{id}/steps
   ```

4. **启用路线**
   ```http
   PUT /api/v2.0/process-routes/{id}/toggle-status
   ```

### 场景2：监控工艺参数

1. **设备实时上报参数**
   ```http
   POST /api/v2.0/process-parameters/batch-record
   ```

2. **监控实时数据**
   ```http
   GET /api/v2.0/process-parameters/realtime
   ```

3. **检查预警信息**
   ```http
   GET /api/v2.0/process-parameters/alerts
   ```

4. **分析参数趋势**
   ```http
   GET /api/v2.0/process-parameters/statistics
   ```

### 场景3：使用模板快速创建

1. **获取可用模板**
   ```http
   GET /api/v2.0/process-route-designer/templates
   ```

2. **创建空路线**
   ```http
   POST /api/v2.0/process-routes
   ```

3. **应用模板**
   ```http
   POST /api/v2.0/process-route-designer/{id}/apply-template
   ```

4. **自定义调整**
   ```http
   PUT /api/v2.0/process-route-designer/{id}/designer-config
   ```

## 数据模型

### 工艺路线流程图配置格式

```json
{
  "nodes": [
    {
      "id": "node_unique_id",
      "type": "start|end|process|decision",
      "label": "节点显示名称",
      "position": { "x": 100, "y": 100 },
      "processId": 10,  // 仅process类型节点需要
      "data": {},  // 自定义数据
      "style": {
        "backgroundColor": "#ffffff",
        "borderColor": "#000000",
        "textColor": "#000000",
        "borderWidth": 1.0,
        "borderRadius": 5.0
      }
    }
  ],
  "edges": [
    {
      "id": "edge_unique_id",
      "source": "source_node_id",
      "target": "target_node_id",
      "label": "连接线标签",
      "data": {},  // 自定义数据
      "style": {
        "stroke": "#000000",
        "strokeWidth": 1.0,
        "strokeDasharray": "5,5"
      }
    }
  ],
  "viewPort": {
    "x": 0,
    "y": 0,
    "zoom": 1.0
  }
}
```

## 注意事项

1. **权限控制**：所有API都需要JWT认证，确保用户具有相应权限
2. **数据验证**：流程图配置必须包含开始和结束节点
3. **版本管理**：工艺路线支持版本控制，建议使用语义化版本号
4. **审计日志**：所有操作都会记录审计日志，便于追溯
5. **性能优化**：大量参数数据建议分批处理，避免单次请求过大
6. **实时监控**：参数实时数据建议设置合理的时间窗口，避免数据量过大

## 扩展功能

### 自定义节点类型
可以通过扩展flowChart配置支持自定义节点类型，如条件判断、并行处理等。

### 参数公式计算
支持基于多个参数值进行公式计算，生成派生参数。

### 工艺路线版本对比
提供版本对比功能，可视化展示不同版本间的差异。

### 智能模板推荐
基于产品类型和历史数据，智能推荐最适合的工艺路线模板。 