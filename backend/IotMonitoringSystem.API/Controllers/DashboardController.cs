using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IotMonitoringSystem.Core.DTOs;
using IotMonitoringSystem.Infrastructure.Data;
using IotMonitoringSystem.Core.Entities;

namespace IotMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(ApplicationDbContext context, ILogger<DashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 获取仪表板摘要信息
        /// </summary>
        [HttpGet("summary")]
        [ProducesResponseType(typeof(ApiResponse<DashboardSummaryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboardSummary()
        {
            try
            {
                var today = DateTime.Today;
                
                // 设备统计
                var totalDevices = await _context.Devices.CountAsync();
                var activeDevices = await _context.Devices.CountAsync(d => d.IsActive);
                
                // 今日报警统计
                var totalAlarmsToday = await _context.Alarms
                    .CountAsync(a => a.Timestamp >= today);
                
                var unacknowledgedAlarms = await _context.Alarms
                    .CountAsync(a => !a.IsAcknowledged);
                
                // 今日数据点统计
                var totalDataPointsToday = await _context.DeviceData
                    .CountAsync(d => d.Timestamp >= today);
                
                // 设备状态摘要
                var deviceStatusSummary = await _context.Devices
                    .Where(d => d.IsActive)
                    .Select(d => new DeviceStatusSummaryDto
                    {
                        DeviceName = d.DeviceName,
                        Status = d.DeviceData
                            .OrderByDescending(dd => dd.Timestamp)
                            .FirstOrDefault()!.Status.ToString(),
                        LastTemperature = d.DeviceData
                            .OrderByDescending(dd => dd.Timestamp)
                            .FirstOrDefault()!.Temperature ?? 0,
                        LastHumidity = d.DeviceData
                            .OrderByDescending(dd => dd.Timestamp)
                            .FirstOrDefault()!.Humidity ?? 0,
                        HasAlarm = d.Alarms.Any(a => !a.IsAcknowledged)
                    })
                    .ToListAsync();
                
                // 报警趋势（最近7天）
                var alarmTrend = new List<AlarmTrendDto>();
                for (int i = 6; i >= 0; i--)
                {
                    var date = today.AddDays(-i);
                    var nextDay = date.AddDays(1);
                    
                    var count = await _context.Alarms
                        .CountAsync(a => a.Timestamp >= date && a.Timestamp < nextDay);
                    
                    alarmTrend.Add(new AlarmTrendDto
                    {
                        Time = date,
                        AlarmCount = count
                    });
                }
                
                var summary = new DashboardSummaryDto
                {
                    TotalDevices = totalDevices,
                    ActiveDevices = activeDevices,
                    TotalAlarmsToday = totalAlarmsToday,
                    UnacknowledgedAlarms = unacknowledgedAlarms,
                    TotalDataPointsToday = totalDataPointsToday,
                    DeviceStatusSummary = deviceStatusSummary,
                    AlarmTrend = alarmTrend
                };
                
                return Ok(ApiResponse<DashboardSummaryDto>.Success(summary));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取仪表板摘要失败");
                return StatusCode(500, ApiResponse<DashboardSummaryDto>.Error("获取仪表板信息失败"));
            }
        }

        /// <summary>
        /// 获取实时监控数据
        /// </summary>
        [HttpGet("monitoring")]
        [ProducesResponseType(typeof(ApiResponse<List<MonitoringDataDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMonitoringData()
        {
            try
            {
                var monitoringData = await _context.Devices
                    .Where(d => d.IsActive)
                    .Select(d => new MonitoringDataDto
                    {
                        DeviceId = d.Id,
                        DeviceName = d.DeviceName,
                        LastDataTimestamp = d.DeviceData
                            .OrderByDescending(dd => dd.Timestamp)
                            .FirstOrDefault()!.Timestamp,
                        Temperature = d.DeviceData
                            .OrderByDescending(dd => dd.Timestamp)
                            .FirstOrDefault()!.Temperature,
                        Humidity = d.DeviceData
                            .OrderByDescending(dd => dd.Timestamp)
                            .FirstOrDefault()!.Humidity,
                        Current = d.DeviceData
                            .OrderByDescending(dd => dd.Timestamp)
                            .FirstOrDefault()!.Current,
                        Voltage = d.DeviceData
                            .OrderByDescending(dd => dd.Timestamp)
                            .FirstOrDefault()!.Voltage,
                        Status = d.DeviceData
                            .OrderByDescending(dd => dd.Timestamp)
                            .FirstOrDefault()!.Status,
                        HasActiveAlarm = d.Alarms.Any(a => !a.IsAcknowledged),
                        ActiveAlarmCount = d.Alarms.Count(a => !a.IsAcknowledged)
                    })
                    .OrderBy(d => d.DeviceName)
                    .ToListAsync();
                
                // 检查设备是否离线（超过5分钟无数据）
                var offlineThreshold = DateTime.UtcNow.AddMinutes(-5);
                foreach (var data in monitoringData)
                {
                    if (data.LastDataTimestamp < offlineThreshold)
                    {
                        data.Status = DeviceStatus.Offline;
                        data.IsOffline = true;
                        data.OfflineDuration = DateTime.UtcNow - data.LastDataTimestamp;
                    }
                }
                
                return Ok(ApiResponse<List<MonitoringDataDto>>.Success(monitoringData));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实时监控数据失败");
                return StatusCode(500, ApiResponse<List<MonitoringDataDto>>.Error("获取监控数据失败"));
            }
        }

        /// <summary>
        /// 获取系统性能指标
        /// </summary>
        [HttpGet("performance")]
        [ProducesResponseType(typeof(ApiResponse<PerformanceMetricsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPerformanceMetrics()
        {
            try
            {
                var metrics = new PerformanceMetricsDto
                {
                    Timestamp = DateTime.UtcNow,
                    DatabaseMetrics = await GetDatabaseMetrics(),
                    SystemMetrics = GetSystemMetrics(),
                    ApiMetrics = GetApiMetrics()
                };
                
                return Ok(ApiResponse<PerformanceMetricsDto>.Success(metrics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取性能指标失败");
                return StatusCode(500, ApiResponse<PerformanceMetricsDto>.Error("获取性能指标失败"));
            }
        }

        private async Task<DatabaseMetricsDto> GetDatabaseMetrics()
        {
            var metrics = new DatabaseMetricsDto();
            
            // 获取表记录数
            metrics.TotalDevices = await _context.Devices.CountAsync();
            metrics.TotalDeviceData = await _context.DeviceData.CountAsync();
            metrics.TotalAlarms = await _context.Alarms.CountAsync();
            metrics.TotalThresholds = await _context.Thresholds.CountAsync();
            
            // 获取今日数据增长率
            var today = DateTime.Today;
            var yesterday = today.AddDays(-1);
            
            var todayCount = await _context.DeviceData
                .CountAsync(d => d.Timestamp >= today);
            
            var yesterdayCount = await _context.DeviceData
                .CountAsync(d => d.Timestamp >= yesterday && d.Timestamp < today);
            
            if (yesterdayCount > 0)
            {
                metrics.DataGrowthRate = (todayCount - yesterdayCount) / (double)yesterdayCount * 100;
            }
            
            return metrics;
        }
        
        private SystemMetricsDto GetSystemMetrics()
        {
            var metrics = new SystemMetricsDto
            {
                ServerTime = DateTime.UtcNow,
                Uptime = GetUptime(),
                MemoryUsage = GetMemoryUsage(),
                CpuUsage = GetCpuUsage(),
                ActiveConnections = GetActiveConnections()
            };
            
            return metrics;
        }
        
        private ApiMetricsDto GetApiMetrics()
        {
            // 这里可以集成Application Insights或其他监控工具
            return new ApiMetricsDto
            {
                TotalRequestsToday = 0, // 实际实现中应从监控系统获取
                AverageResponseTime = 0,
                ErrorRate = 0,
                ActiveUsers = 0
            };
        }
        
        private TimeSpan GetUptime()
        {
            return TimeSpan.FromSeconds(Environment.TickCount / 1000.0);
        }
        
        private double GetMemoryUsage()
        {
            var process = System.Diagnostics.Process.GetCurrentProcess();
            return process.WorkingSet64 / (1024.0 * 1024.0); // MB
        }
        
        private double GetCpuUsage()
        {
            // 实际实现中需要使用更复杂的方法获取CPU使用率
            return 0;
        }
        
        private int GetActiveConnections()
        {
            // 实际实现中可能需要从数据库或应用服务器获取
            return 0;
        }
    }
    
    public class MonitoringDataDto
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public DateTime LastDataTimestamp { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public decimal? Current { get; set; }
        public decimal? Voltage { get; set; }
        public DeviceStatus Status { get; set; }
        public bool HasActiveAlarm { get; set; }
        public int ActiveAlarmCount { get; set; }
        public bool IsOffline { get; set; }
        public TimeSpan? OfflineDuration { get; set; }
    }
    
    public class PerformanceMetricsDto
    {
        public DateTime Timestamp { get; set; }
        public DatabaseMetricsDto DatabaseMetrics { get; set; } = new();
        public SystemMetricsDto SystemMetrics { get; set; } = new();
        public ApiMetricsDto ApiMetrics { get; set; } = new();
    }
    
    public class DatabaseMetricsDto
    {
        public int TotalDevices { get; set; }
        public int TotalDeviceData { get; set; }
        public int TotalAlarms { get; set; }
        public int TotalThresholds { get; set; }
        public double DataGrowthRate { get; set; } // 百分比
    }
    
    public class SystemMetricsDto
    {
        public DateTime ServerTime { get; set; }
        public TimeSpan Uptime { get; set; }
        public double MemoryUsage { get; set; } // MB
        public double CpuUsage { get; set; } // 百分比
        public int ActiveConnections { get; set; }
    }
    
    public class ApiMetricsDto
    {
        public long TotalRequestsToday { get; set; }
        public double AverageResponseTime { get; set; } // 毫秒
        public double ErrorRate { get; set; } // 百分比
        public int ActiveUsers { get; set; }
    }
}