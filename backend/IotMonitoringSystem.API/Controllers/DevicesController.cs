using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IotMonitoringSystem.Core.DTOs;
using IotMonitoringSystem.Infrastructure.Data;
using IotMonitoringSystem.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using System.Linq.Dynamic.Core;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using IotMonitoringSystem.Core.Entities;

namespace IotMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly ILogger<DeviceDataController> _logger;

        public DeviceDataController(
            ApplicationDbContext context, 
            IHubContext<DeviceHub> hubContext,
            ILogger<DeviceDataController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// 获取设备实时数据
        /// </summary>
        [HttpGet("realtime/{deviceId}")]
        [ProducesResponseType(typeof(ApiResponse<List<RealTimeDataDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRealTimeData(int deviceId, [FromQuery] int minutes = 30)
        {
            try
            {
                var endTime = DateTime.UtcNow;
                var startTime = endTime.AddMinutes(-minutes);
                
                // 获取设备阈值
                var thresholds = await _context.Thresholds
                    .Where(t => t.DeviceId == deviceId)
                    .ToListAsync();
                
                var data = await _context.DeviceData
                    .Where(d => d.DeviceId == deviceId 
                             && d.Timestamp >= startTime 
                             && d.Timestamp <= endTime)
                    .OrderBy(d => d.Timestamp)
                    .Select(d => new RealTimeDataDto
                    {
                        Timestamp = d.Timestamp,
                        Temperature = d.Temperature,
                        Humidity = d.Humidity,
                        Current = d.Current,
                        Voltage = d.Voltage,
                        Status = d.Status.ToString(),
                        HasAlarm = false, // 将在下面计算
                        AlarmMessage = null
                    })
                    .ToListAsync();
                
                // 检查每个数据点是否有报警
                foreach (var item in data)
                {
                    CheckDataPointAlarms(item, thresholds);
                }
                
                _logger.LogInformation("获取设备 {DeviceId} 的实时数据，共 {Count} 条", deviceId, data.Count);
                
                return Ok(ApiResponse<List<RealTimeDataDto>>.Success(data));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实时数据失败，设备ID: {DeviceId}", deviceId);
                return StatusCode(500, ApiResponse<List<RealTimeDataDto>>.Error("获取实时数据失败"));
            }
        }

        /// <summary>
        /// 获取最新数据点
        /// </summary>
        [HttpGet("latest/{deviceId}")]
        [ProducesResponseType(typeof(ApiResponse<RealTimeDataDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLatestData(int deviceId)
        {
            try
            {
                var latestData = await _context.DeviceData
                    .Where(d => d.DeviceId == deviceId)
                    .OrderByDescending(d => d.Timestamp)
                    .FirstOrDefaultAsync();
                
                if (latestData == null)
                {
                    return Ok(ApiResponse<RealTimeDataDto>.Success(new RealTimeDataDto()));
                }
                
                // 获取阈值并检查报警
                var thresholds = await _context.Thresholds
                    .Where(t => t.DeviceId == deviceId)
                    .ToListAsync();
                
                var result = new RealTimeDataDto
                {
                    Timestamp = latestData.Timestamp,
                    Temperature = latestData.Temperature,
                    Humidity = latestData.Humidity,
                    Current = latestData.Current,
                    Voltage = latestData.Voltage,
                    Status = latestData.Status.ToString()
                };
                
                CheckDataPointAlarms(result, thresholds);
                
                return Ok(ApiResponse<RealTimeDataDto>.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取最新数据失败，设备ID: {DeviceId}", deviceId);
                return StatusCode(500, ApiResponse<RealTimeDataDto>.Error("获取最新数据失败"));
            }
        }

        /// <summary>
        /// 获取历史数据
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<DeviceDataDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetHistoryData([FromQuery] HistoricalDataQueryDto query)
        {
            try
            {
                // 验证时间范围
                if (query.StartTime >= query.EndTime)
                {
                    return BadRequest(ApiResponse<PagedResult<DeviceDataDto>>.Error("开始时间必须早于结束时间"));
                }
                
                if ((query.EndTime - query.StartTime).TotalDays > 31)
                {
                    return BadRequest(ApiResponse<PagedResult<DeviceDataDto>>.Error("查询时间范围不能超过31天"));
                }
                
                var baseQuery = _context.DeviceData
                    .Include(d => d.Device)
                    .Where(d => d.DeviceId == query.DeviceId 
                             && d.Timestamp >= query.StartTime 
                             && d.Timestamp <= query.EndTime);
                
                // 动态排序
                if (!string.IsNullOrEmpty(query.SortBy))
                {
                    var sortDirection = query.SortDescending ? "DESC" : "ASC";
                    baseQuery = baseQuery.OrderBy($"{query.SortBy} {sortDirection}");
                }
                else
                {
                    baseQuery = baseQuery.OrderByDescending(d => d.Timestamp);
                }
                
                var totalCount = await baseQuery.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)query.PageSize);
                
                var data = await baseQuery
                    .Skip((query.PageNumber - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Select(d => new DeviceDataDto
                    {
                        Id = d.Id,
                        DeviceId = d.DeviceId,
                        Temperature = d.Temperature,
                        Humidity = d.Humidity,
                        Current = d.Current,
                        Voltage = d.Voltage,
                        Status = d.Status.ToString(),
                        Timestamp = d.Timestamp,
                        DeviceName = d.Device!.DeviceName
                    })
                    .ToListAsync();
                
                var result = new PagedResult<DeviceDataDto>
                {
                    Data = data,
                    PageNumber = query.PageNumber,
                    PageSize = query.PageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPreviousPage = query.PageNumber > 1,
                    HasNextPage = query.PageNumber < totalPages
                };
                
                // 添加分页头信息
                Response.Headers.Append("X-Pagination", JsonSerializer.Serialize(new
                {
                    result.TotalCount,
                    result.PageSize,
                    result.PageNumber,
                    result.TotalPages
                }));
                
                _logger.LogInformation("查询历史数据，设备ID: {DeviceId}, 时间范围: {StartTime} - {EndTime}", 
                    query.DeviceId, query.StartTime, query.EndTime);
                
                return Ok(ApiResponse<PagedResult<DeviceDataDto>>.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询历史数据失败");
                return StatusCode(500, ApiResponse<PagedResult<DeviceDataDto>>.Error("查询历史数据失败"));
            }
        }

        /// <summary>
        /// 接收设备上报数据
        /// </summary>
        [HttpPost("report")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReportDeviceData([FromBody] DeviceDataReportDto report)
        {
            try
            {
                // 验证设备是否存在
                var device = await _context.Devices
                    .FirstOrDefaultAsync(d => d.Id == report.DeviceId && d.IsActive);
                
                if (device == null)
                {
                    return BadRequest(ApiResponse.Error("设备不存在或已停用"));
                }
                
                // 创建设备数据记录
                var deviceData = new DeviceData
                {
                    DeviceId = report.DeviceId,
                    Temperature = report.Temperature,
                    Humidity = report.Humidity,
                    Current = report.Current,
                    Voltage = report.Voltage,
                    Status = report.Status,
                    Timestamp = report.Timestamp ?? DateTime.UtcNow
                };
                
                _context.DeviceData.Add(deviceData);
                await _context.SaveChangesAsync();
                
                // 检查报警
                var alarms = await CheckAlarms(deviceData);
                
                // 发送实时数据更新
                var realTimeData = new RealTimeDataDto
                {
                    Timestamp = deviceData.Timestamp,
                    Temperature = deviceData.Temperature,
                    Humidity = deviceData.Humidity,
                    Current = deviceData.Current,
                    Voltage = deviceData.Voltage,
                    Status = deviceData.Status.ToString(),
                    HasAlarm = alarms.Any(),
                    AlarmMessage = alarms.FirstOrDefault()?.Message
                };
                
                await _hubContext.Clients.Group($"device-{deviceData.DeviceId}")
                    .SendAsync("DeviceDataUpdate", realTimeData);
                
                // 如果有报警，发送报警通知
                if (alarms.Any())
                {
                    foreach (var alarm in alarms)
                    {
                        await _hubContext.Clients.Group($"device-{deviceData.DeviceId}")
                            .SendAsync("NewAlarm", new AlarmDto
                            {
                                Id = alarm.Id,
                                DeviceId = alarm.DeviceId,
                                DeviceName = device.DeviceName,
                                FactorType = alarm.FactorType,
                                FactorName = GetFactorName(alarm.FactorType),
                                Value = alarm.Value,
                                LimitType = alarm.LimitType,
                                Message = alarm.Message,
                                Timestamp = alarm.Timestamp,
                                IsAcknowledged = alarm.IsAcknowledged,
                                Duration = DateTime.UtcNow - alarm.Timestamp
                            });
                    }
                }
                
                _logger.LogInformation("设备数据上报成功，设备ID: {DeviceId}, 数据ID: {DataId}", 
                    report.DeviceId, deviceData.Id);
                
                return Ok(ApiResponse.Success("数据上报成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设备数据上报失败");
                return StatusCode(500, ApiResponse.Error("数据上报失败"));
            }
        }

        /// <summary>
        /// 导出历史数据为CSV
        /// </summary>
        [HttpGet("export/{deviceId}")]
        [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
        public async Task<IActionResult> ExportHistoryData(int deviceId, 
            [FromQuery] DateTime startTime, 
            [FromQuery] DateTime endTime)
        {
            try
            {
                var data = await _context.DeviceData
                    .Include(d => d.Device)
                    .Where(d => d.DeviceId == deviceId 
                             && d.Timestamp >= startTime 
                             && d.Timestamp <= endTime)
                    .OrderBy(d => d.Timestamp)
                    .ToListAsync();
                
                if (!data.Any())
                {
                    return BadRequest(ApiResponse.Error("没有可导出的数据"));
                }
                
                var csvLines = new List<string>
                {
                    "时间戳,温度(°C),湿度(%),电流(A),电压(V),状态"
                };
                
                foreach (var item in data)
                {
                    csvLines.Add($"{item.Timestamp:yyyy-MM-dd HH:mm:ss}," +
                                $"{item.Temperature?.ToString("F2") ?? ""}," +
                                $"{item.Humidity?.ToString("F2") ?? ""}," +
                                $"{item.Current?.ToString("F2") ?? ""}," +
                                $"{item.Voltage?.ToString("F2") ?? ""}," +
                                $"{item.Status}");
                }
                
                var csvContent = string.Join(Environment.NewLine, csvLines);
                var bytes = System.Text.Encoding.UTF8.GetBytes(csvContent);
                
                var fileName = $"设备数据_{deviceId}_{DateTime.Now:yyyyMMddHHmmss}.csv";
                
                return File(bytes, "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出历史数据失败");
                return StatusCode(500, ApiResponse.Error("导出失败"));
            }
        }

        /// <summary>
        /// 获取数据统计信息
        /// </summary>
        [HttpGet("statistics/{deviceId}")]
        [ProducesResponseType(typeof(ApiResponse<DataStatisticsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStatistics(int deviceId, [FromQuery] DateTime? date = null)
        {
            try
            {
                var targetDate = date ?? DateTime.Today;
                var startTime = targetDate.Date;
                var endTime = startTime.AddDays(1);
                
                var data = await _context.DeviceData
                    .Where(d => d.DeviceId == deviceId 
                             && d.Timestamp >= startTime 
                             && d.Timestamp < endTime)
                    .ToListAsync();
                
                if (!data.Any())
                {
                    return Ok(ApiResponse<DataStatisticsDto>.Success(new DataStatisticsDto
                    {
                        Date = targetDate,
                        TotalCount = 0
                    }));
                }
                
                var statistics = new DataStatisticsDto
                {
                    Date = targetDate,
                    TotalCount = data.Count,
                    TemperatureStats = CalculateStats(data.Where(d => d.Temperature.HasValue).Select(d => d.Temperature!.Value)),
                    HumidityStats = CalculateStats(data.Where(d => d.Humidity.HasValue).Select(d => d.Humidity!.Value)),
                    CurrentStats = CalculateStats(data.Where(d => d.Current.HasValue).Select(d => d.Current!.Value)),
                    VoltageStats = CalculateStats(data.Where(d => d.Voltage.HasValue).Select(d => d.Voltage!.Value)),
                    StatusDistribution = data.GroupBy(d => d.Status)
                        .ToDictionary(g => g.Key.ToString(), g => g.Count())
                };
                
                return Ok(ApiResponse<DataStatisticsDto>.Success(statistics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取数据统计失败");
                return StatusCode(500, ApiResponse<DataStatisticsDto>.Error("获取统计信息失败"));
            }
        }

        private async Task<List<Alarm>> CheckAlarms(DeviceData data)
        {
            var alarms = new List<Alarm>();
            var thresholds = await _context.Thresholds
                .Where(t => t.DeviceId == data.DeviceId && t.IsRealTimeAlert)
                .ToListAsync();
            
            foreach (var threshold in thresholds)
            {
                decimal? value = threshold.FactorType switch
                {
                    FactorType.Temperature => data.Temperature,
                    FactorType.Humidity => data.Humidity,
                    FactorType.Current => data.Current,
                    FactorType.Voltage => data.Voltage,
                    _ => null
                };
                
                if (value.HasValue)
                {
                    bool isUpperAlarm = value > threshold.UpperLimit;
                    bool isLowerAlarm = value < threshold.LowerLimit;
                    
                    if (isUpperAlarm || isLowerAlarm)
                    {
                        var alarm = new Alarm
                        {
                            DeviceId = data.DeviceId,
                            ThresholdId = threshold.Id,
                            FactorType = threshold.FactorType,
                            Value = value.Value,
                            LimitType = isUpperAlarm ? "Upper" : "Lower",
                            Message = threshold.AlertMessage ?? 
                                     $"设备 {data.DeviceId} {GetFactorName(threshold.FactorType)} " +
                                     $"{(isUpperAlarm ? "超过上限" : "低于下限")}: {value.Value:F2}",
                            Timestamp = DateTime.UtcNow
                        };
                        
                        alarms.Add(alarm);
                        _context.Alarms.Add(alarm);
                    }
                }
            }
            
            if (alarms.Any())
            {
                await _context.SaveChangesAsync();
            }
            
            return alarms;
        }
        
        private void CheckDataPointAlarms(RealTimeDataDto data, List<Threshold> thresholds)
        {
            foreach (var threshold in thresholds.Where(t => t.IsRealTimeAlert))
            {
                decimal? value = threshold.FactorType switch
                {
                    FactorType.Temperature => data.Temperature,
                    FactorType.Humidity => data.Humidity,
                    FactorType.Current => data.Current,
                    FactorType.Voltage => data.Voltage,
                    _ => null
                };
                
                if (value.HasValue && (value > threshold.UpperLimit || value < threshold.LowerLimit))
                {
                    data.HasAlarm = true;
                    data.AlarmMessage = $"{GetFactorName(threshold.FactorType)}异常: {value:F2}";
                    break;
                }
            }
        }
        
        private string GetFactorName(FactorType? factorType)
        {
            return factorType switch
            {
                FactorType.Temperature => "温度",
                FactorType.Humidity => "湿度",
                FactorType.Current => "电流",
                FactorType.Voltage => "电压",
                _ => "未知"
            };
        }
        
        private FactorStatisticsDto CalculateStats(IEnumerable<decimal> values)
        {
            var valueList = values.ToList();
            if (!valueList.Any())
            {
                return new FactorStatisticsDto();
            }
            
            return new FactorStatisticsDto
            {
                Average = valueList.Average(),
                Maximum = valueList.Max(),
                Minimum = valueList.Min(),
                StdDeviation = CalculateStdDev(valueList)
            };
        }
        
        private double CalculateStdDev(List<decimal> values)
        {
            var avg = (double)values.Average();
            var sum = values.Sum(v => Math.Pow((double)v - avg, 2));
            return Math.Sqrt(sum / values.Count);
        }
    }
    
    public class DeviceDataReportDto
    {
        [Required]
        public int DeviceId { get; set; }
        
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public decimal? Current { get; set; }
        public decimal? Voltage { get; set; }
        
        [Required]
        public DeviceStatus Status { get; set; }
        
        public DateTime? Timestamp { get; set; }
    }
    
    public class PagedResult<T>
    {
        public List<T> Data { get; set; } = new();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }
    }
    
    public class DataStatisticsDto
    {
        public DateTime Date { get; set; }
        public int TotalCount { get; set; }
        public FactorStatisticsDto? TemperatureStats { get; set; }
        public FactorStatisticsDto? HumidityStats { get; set; }
        public FactorStatisticsDto? CurrentStats { get; set; }
        public FactorStatisticsDto? VoltageStats { get; set; }
        public Dictionary<string, int> StatusDistribution { get; set; } = new();
    }
    
    public class FactorStatisticsDto
    {
        public decimal Average { get; set; }
        public decimal Maximum { get; set; }
        public decimal Minimum { get; set; }
        public double StdDeviation { get; set; }
    }
}