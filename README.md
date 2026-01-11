iot-monitoring-system/
│
├── backend/                    # .NET 8后端
│   ├── IotMonitoringSystem.API/
│   │   ├── Controllers/
│   │   │   ├── DevicesController.cs
│   │   │   ├── DeviceDataController.cs
│   │   │   └── ThresholdsController.cs
│   │   ├── Hubs/
│   │   │   └── DeviceHub.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── IotMonitoringSystem.API.csproj
│   │
│   ├── IotMonitoringSystem.Core/
│   │   ├── Entities/
│   │   ├── DTOs/
│   │   └── Services/
│   │
│   └── IotMonitoringSystem.Infrastructure/
│       └── Data/
│           └── ApplicationDbContext.cs
│
├── frontend/                   # Vue 2前端
│   ├── public/
│   │   └── index.html
│   │
│   ├── src/
│   │   ├── views/
│   │   │   ├── Dashboard.vue
│   │   │   ├── HistoryData.vue
│   │   │   ├── Threshold.vue
│   │   │   └── DeviceInfo.vue
│   │   │
│   │   ├── components/
│   │   │   ├── charts/
│   │   │   │   ├── RealTimeChart.vue
│   │   │   │   └── HistoryChart.vue
│   │   │   ├── devices/
│   │   │   │   └── DeviceSelector.vue
│   │   │   └── alarms/
│   │   │       └── AlarmIndicator.vue
│   │   │
│   │   ├── services/
│   │   │   ├── api.js
│   │   │   ├── signalr.js
│   │   │   └── alarmService.js
│   │   │
│   │   ├── router/
│   │   │   └── index.js
│   │   │
│   │   └── App.vue
│   │
│   ├── package.json
│   └── vue.config.js
│
├── database/                   # 数据库脚本
│   ├── 01_create_database.sql
│   ├── 02_create_tables.sql
│   └── 03_sample_data.sql
│
├── docs/                      # 文档
│   ├── API文档.md
│   ├── 部署指南.md
│   └── 用户手册.md
│
└── README.md