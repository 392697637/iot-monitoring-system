using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IotMonitoringSystem.Core.DTOs;
using IotMonitoringSystem.Infrastructure.Data;
using IotMonitoringSystem.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using IotMonitoringSystem.Core.Entities;

namespace IotMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AlarmsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly ILogger<AlarmsController> _logger;

        public AlarmsController(
            ApplicationDbContext context, 
            IHubContext<DeviceHub> hubContext,
            ILogger<AlarmsController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// 获取报警列表
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<AlarmDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAlarms(
            [FromQuery] int? deviceId = null,
            [FromQuery] bool? isAcknowledged = null,
            [FromQuery] DateTime? startTime = null,
            [FromQuery] DateTime? endTime = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var query = _context.Alarms
                    .Include(a => a.Device)
                    .Include(a => a.Threshold)
                    .AsQueryable();
                
                // 应用筛选条件
                if (deviceId.HasValue)
                {
                    query = query.Where(a => a.DeviceId == deviceId.Value);
                }
                
                if (isAcknowledged.HasValue)
                {
                    query = query.Where(a => a.IsAcknowledged == isAcknowledged.Value);
                }
                
                if (startTime.HasValue)
                {
                    query = query.Where(a => a.Timestamp >= startTime.Value);
                }
                
                if (endTime.HasValue)
                {
                    query = query.Where(a => a.Timestamp <= endTime.Value);
                }
                
                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
                
                var alarms = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(a => new AlarmDto
                    {
                        Id = a.Id,
                        DeviceId = a.DeviceId,
                        DeviceName = a.Device!.DeviceName,
                        FactorType = a.FactorType,
                        FactorName = GetFactorName(a.FactorType),
                        Value = a.Value,
                        LimitType = a.LimitType,
                        Message = a.Message,
                        Timestamp = a.Timestamp,
                        IsAcknowledged = a.IsAcknowledged,
                        AcknowledgedAt = a.AcknowledgedAt,
                        Duration = DateTime.UtcNow - a.Timestamp
                    })
                    .ToListAsync();
                
                var result = new PagedResult<AlarmDto>
                {
                    Data = alarms,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                };
                
                return Ok(ApiResponse<PagedResult<AlarmDto>>.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取报警列表失败");
                return StatusCode(500, ApiResponse<PagedResult<AlarmDto>>.Error("获取报警列表失败"));
            }
        }

        /// <summary>
        /// 获取未确认的报警
        /// </summary>
        [HttpGet("unacknowledged")]
        [ProducesResponseType(typeof(ApiResponse<List<AlarmDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnacknowledgedAlarms([FromQuery] int? deviceId = null)
        {
            try
            {
                var query = _context.Alarms
                    .Include(a => a.Device)
                    .Where(a => !a.IsAcknowledged);
                
                if (deviceId.HasValue)
                {
                    query = query.Where(a => a.DeviceId == deviceId.Value);
                }
                
                var alarms = await query
                    .OrderByDescending(a => a.Timestamp)
                    .Take(100)
                    .Select(a => new AlarmDto
                    {
                        Id = a.Id,
                        DeviceId = a.DeviceId,
                        DeviceName = a.Device!.DeviceName,
                        FactorType = a.FactorType,
                        FactorName = GetFactorName(a.FactorType),
                        Value = a.Value,
                        LimitType = a.LimitType,
                        Message = a.Message,
                        Timestamp = a.Timestamp,
                        IsAcknowledged = a.IsAcknowledged,
                        Duration = DateTime.UtcNow - a.Timestamp
                    })
                    .ToListAsync();
                
                return Ok(ApiResponse<List<AlarmDto>>.Success(alarms));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取未确认报警失败");
                return StatusCode(500, ApiResponse<List<AlarmDto>>.Error("获取未确认报警失败"));
            }
        }

        /// <summary>
        /// 确认报警
        /// </summary>
        [HttpPost("{id}/acknowledge")]
        [ProducesResponseType(typeof(ApiResponse<AlarmDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AcknowledgeAlarm(long id)
        {
            try
            {
                var alarm = await _context.Alarms
                    .Include(a => a.Device)
                    .FirstOrDefaultAsync(a => a.Id == id);
                
                if (alarm == null)
                {
                    return NotFound(ApiResponse<AlarmDto>.Error("报警不存在"));
                }
                
                if (alarm.IsAcknowledged)
                {
                    return BadRequest(ApiResponse<AlarmDto>.Error("报警已确认"));
                }
                
                alarm.IsAcknowledged = true;
                alarm.AcknowledgedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                var result = new AlarmDto
                {
                    Id = alarm.Id,
                    DeviceId = alarm.DeviceId,
                    DeviceName = alarm.Device!.DeviceName,
                    FactorType = alarm.FactorType,
                    FactorName = GetFactorName(alarm.FactorType),
                    Value = alarm.Value,
                    LimitType = alarm.LimitType,
                    Message = alarm.Message,
                    Timestamp = alarm.Timestamp,
                    IsAcknowledged = alarm.IsAcknowledged,
                    AcknowledgedAt = alarm.AcknowledgedAt,
                    Duration = DateTime.UtcNow - alarm.Timestamp
                };
                
                // 发送报警确认通知
                await _hubContext.Clients.Group($"device-{alarm.DeviceId}")
                    .SendAsync("AlarmAcknowledged", result);
                
                _logger.LogInformation("确认报警成功，ID: {AlarmId}", id);
                
                return Ok(ApiResponse<AlarmDto>.Success(result, "报警确认成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "确认报警失败，ID: {AlarmId}", id);
                return StatusCode(500, ApiResponse<AlarmDto>.Error("确认报警失败"));
            }
        }

        /// <summary>
        /// 批量确认报警
        /// </summary>
        [HttpPost("batch-acknowledge")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchAcknowledgeAlarms([FromBody] List<long> alarmIds)
        {
            try
            {
                var alarms = await _context.Alarms
                    .Where(a => alarmIds.Contains(a.Id) && !a.IsAcknowledged)
                    .ToListAsync();
                
                var now = DateTime.UtcNow;
                var acknowledgedAlarms = new List<Alarm>();
                
                foreach (var alarm in alarms)
                {
                    alarm.IsAcknowledged = true;
                    alarm.AcknowledgedAt = now;
                    acknowledgedAlarms.Add(alarm);
                }
                
                if (acknowledgedAlarms.Any())
                {
                    await _context.SaveChangesAsync();
                    
                    // 发送批量确认通知
                    foreach (var group in acknowledgedAlarms.GroupBy(a => a.DeviceId))
                    {
                        await _hubContext.Clients.Group($"device-{group.Key}")
                            .SendAsync("AlarmsBatchAcknowledged", group.Select(a => a.Id).ToList());
                    }
                    
                    _logger.LogInformation("批量确认报警成功，共确认 {Count} 条", acknowledgedAlarms.Count);
                    
                    return Ok(ApiResponse.Success($"批量确认成功，共确认 {acknowledgedAlarms.Count} 条报警"));
                }
                
                return Ok(ApiResponse.Success("没有需要确认的报警"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量确认报警失败");
                return StatusCode(500, ApiResponse.Error("批量确认失败"));
            }
        }

        /// <summary>
        /// 获取报警统计
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponse<AlarmStatisticsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAlarmStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            try
            {
                var start = startDate ?? DateTime.Today.AddDays(-30);
                var end = endDate ?? DateTime.Today.AddDays(1);
                
                var query = _context.Alarms
                    .Where(a => a.Timestamp >= start && a.Timestamp < end);
                
                var totalAlarms = await query.CountAsync();
                var unacknowledgedAlarms = await query.CountAsync(a => !a.IsAcknowledged);
                var acknowledgedAlarms = totalAlarms - unacknowledgedAlarms;
                
                // 按设备统计
                var byDevice = await query
                    .GroupBy(a => a.DeviceId)
                    .Select(g => new
                    {
                        DeviceId = g.Key,
                        Count = g.Count(),
                        DeviceName = g.First().Device!.DeviceName
                    })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync();
                
                // 按因子类型统计
                var byFactor = await query
                    .Where(a => a.FactorType.HasValue)
                    .GroupBy(a => a.FactorType)
                    .Select(g => new
                    {
                        FactorType = g.Key,
                        Count = g.Count()
                    })
                    .OrderByDescending(x => x.Count)
                    .ToListAsync();
                
                // 按时间统计（每日）
                var byDate = await query
                    .GroupBy(a => a.Timestamp.Date)
                    .Select(g => new
                    {
                        Date = g.Key,
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToListAsync();
                
                var statistics = new AlarmStatisticsDto
                {
                    Period = new DateRange { Start = start, End = end },
                    TotalAlarms = totalAlarms,
                    UnacknowledgedAlarms = unacknowledgedAlarms,
                    AcknowledgedAlarms = acknowledgedAlarms,
                    ByDevice = byDevice.ToDictionary(
                        x => x.DeviceName,
                        x => x.Count),
                    ByFactor = byFactor.ToDictionary(
                        x => GetFactorName(x.FactorType),
                        x => x.Count),
                    ByDate = byDate.ToDictionary(
                        x => x.Date.ToString("yyyy-MM-dd"),
                        x => x.Count)
                };
                
                return Ok(ApiResponse<AlarmStatisticsDto>.Success(statistics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取报警统计失败");
                return StatusCode(500, ApiResponse<AlarmStatisticsDto>.Error("获取统计信息失败"));
            }
        }

        /// <summary>
        /// 清除过期报警
        /// </summary>
        [HttpDelete("cleanup")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CleanupOldAlarms([FromQuery] int days = 180)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var oldAlarms = await _context.Alarms
                    .Where(a => a.Timestamp < cutoffDate && a.IsAcknowledged)
                    .ToListAsync();
                
                if (!oldAlarms.Any())
                {
                    return Ok(ApiResponse.Success("没有需要清理的过期报警"));
                }
                
                _context.Alarms.RemoveRange(oldAlarms);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("清理过期报警成功，共清理 {Count} 条", oldAlarms.Count);
                
                return Ok(ApiResponse.Success($"成功清理 {oldAlarms.Count} 条过期报警"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理过期报警失败");
                return StatusCode(500, ApiResponse.Error("清理失败"));
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
    }
    
    public class AlarmStatisticsDto
    {
        public DateRange Period { get; set; } = new();
        public int TotalAlarms { get; set; }
        public int UnacknowledgedAlarms { get; set; }
        public int AcknowledgedAlarms { get; set; }
        public Dictionary<string, int> ByDevice { get; set; } = new();
        public Dictionary<string, int> ByFactor { get; set; } = new();
        public Dictionary<string, int> ByDate { get; set; } = new();
    }
    
    public class DateRange
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
    }
}