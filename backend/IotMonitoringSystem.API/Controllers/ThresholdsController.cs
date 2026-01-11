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
    public class ThresholdsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<DeviceHub> _hubContext;
        private readonly ILogger<ThresholdsController> _logger;

        public ThresholdsController(
            ApplicationDbContext context, 
            IHubContext<DeviceHub> hubContext,
            ILogger<ThresholdsController> logger)
        {
            _context = context;
            _hubContext = hubContext;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有阈值设置
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<ThresholdDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetThresholds([FromQuery] int? deviceId = null)
        {
            try
            {
                var query = _context.Thresholds
                    .Include(t => t.Device)
                    .AsQueryable();
                
                if (deviceId.HasValue)
                {
                    query = query.Where(t => t.DeviceId == deviceId.Value);
                }
                
                var thresholds = await query
                    .Select(t => new ThresholdDto
                    {
                        Id = t.Id,
                        DeviceId = t.DeviceId,
                        FactorType = t.FactorType,
                        FactorName = t.FactorName ?? GetFactorName(t.FactorType),
                        UpperLimit = t.UpperLimit,
                        LowerLimit = t.LowerLimit,
                        IsRealTimeAlert = t.IsRealTimeAlert,
                        AlertMessage = t.AlertMessage,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        DeviceName = t.Device!.DeviceName
                    })
                    .OrderBy(t => t.DeviceId)
                    .ThenBy(t => t.FactorType)
                    .ToListAsync();
                
                return Ok(ApiResponse<List<ThresholdDto>>.Success(thresholds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取阈值设置失败");
                return StatusCode(500, ApiResponse<List<ThresholdDto>>.Error("获取阈值设置失败"));
            }
        }

        /// <summary>
        /// 获取设备的阈值设置
        /// </summary>
        [HttpGet("device/{deviceId}")]
        [ProducesResponseType(typeof(ApiResponse<List<ThresholdDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeviceThresholds(int deviceId)
        {
            try
            {
                var thresholds = await _context.Thresholds
                    .Where(t => t.DeviceId == deviceId)
                    .Select(t => new ThresholdDto
                    {
                        Id = t.Id,
                        DeviceId = t.DeviceId,
                        FactorType = t.FactorType,
                        FactorName = t.FactorName ?? GetFactorName(t.FactorType),
                        UpperLimit = t.UpperLimit,
                        LowerLimit = t.LowerLimit,
                        IsRealTimeAlert = t.IsRealTimeAlert,
                        AlertMessage = t.AlertMessage,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt
                    })
                    .OrderBy(t => t.FactorType)
                    .ToListAsync();
                
                return Ok(ApiResponse<List<ThresholdDto>>.Success(thresholds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备阈值设置失败，设备ID: {DeviceId}", deviceId);
                return StatusCode(500, ApiResponse<List<ThresholdDto>>.Error("获取阈值设置失败"));
            }
        }

        /// <summary>
        /// 获取单个阈值设置
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ThresholdDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetThreshold(int id)
        {
            try
            {
                var threshold = await _context.Thresholds
                    .Include(t => t.Device)
                    .Where(t => t.Id == id)
                    .Select(t => new ThresholdDto
                    {
                        Id = t.Id,
                        DeviceId = t.DeviceId,
                        FactorType = t.FactorType,
                        FactorName = t.FactorName ?? GetFactorName(t.FactorType),
                        UpperLimit = t.UpperLimit,
                        LowerLimit = t.LowerLimit,
                        IsRealTimeAlert = t.IsRealTimeAlert,
                        AlertMessage = t.AlertMessage,
                        CreatedAt = t.CreatedAt,
                        UpdatedAt = t.UpdatedAt,
                        DeviceName = t.Device!.DeviceName
                    })
                    .FirstOrDefaultAsync();
                
                if (threshold == null)
                {
                    return NotFound(ApiResponse<ThresholdDto>.Error("阈值设置不存在"));
                }
                
                return Ok(ApiResponse<ThresholdDto>.Success(threshold));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取阈值设置失败，ID: {ThresholdId}", id);
                return StatusCode(500, ApiResponse<ThresholdDto>.Error("获取阈值设置失败"));
            }
        }

        /// <summary>
        /// 创建阈值设置
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<ThresholdDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateThreshold([FromBody] CreateThresholdDto dto)
        {
            try
            {
                // 验证设备是否存在
                var device = await _context.Devices.FindAsync(dto.DeviceId);
                if (device == null)
                {
                    return BadRequest(ApiResponse<ThresholdDto>.Error("设备不存在"));
                }
                
                // 验证阈值是否合理
                if (dto.UpperLimit <= dto.LowerLimit)
                {
                    return BadRequest(ApiResponse<ThresholdDto>.Error("上限值必须大于下限值"));
                }
                
                // 检查是否已存在相同设备和因子的阈值
                var exists = await _context.Thresholds
                    .AnyAsync(t => t.DeviceId == dto.DeviceId && t.FactorType == dto.FactorType);
                
                if (exists)
                {
                    return BadRequest(ApiResponse<ThresholdDto>.Error("该设备已存在相同监测因子的阈值设置"));
                }
                
                var threshold = new Threshold
                {
                    DeviceId = dto.DeviceId,
                    FactorType = dto.FactorType,
                    FactorName = GetFactorName(dto.FactorType),
                    UpperLimit = dto.UpperLimit,
                    LowerLimit = dto.LowerLimit,
                    IsRealTimeAlert = dto.IsRealTimeAlert,
                    AlertMessage = dto.AlertMessage
                };
                
                _context.Thresholds.Add(threshold);
                await _context.SaveChangesAsync();
                
                var result = new ThresholdDto
                {
                    Id = threshold.Id,
                    DeviceId = threshold.DeviceId,
                    FactorType = threshold.FactorType,
                    FactorName = threshold.FactorName ?? GetFactorName(threshold.FactorType),
                    UpperLimit = threshold.UpperLimit,
                    LowerLimit = threshold.LowerLimit,
                    IsRealTimeAlert = threshold.IsRealTimeAlert,
                    AlertMessage = threshold.AlertMessage,
                    CreatedAt = threshold.CreatedAt,
                    UpdatedAt = threshold.UpdatedAt,
                    DeviceName = device.DeviceName
                };
                
                // 发送阈值更新通知
                await _hubContext.Clients.Group($"device-{device.Id}")
                    .SendAsync("ThresholdUpdated", result);
                
                _logger.LogInformation("创建阈值设置成功，设备ID: {DeviceId}, 因子类型: {FactorType}", 
                    dto.DeviceId, dto.FactorType);
                
                return CreatedAtAction(nameof(GetThreshold), new { id = threshold.Id }, 
                    ApiResponse<ThresholdDto>.Success(result, "阈值设置创建成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建阈值设置失败");
                return StatusCode(500, ApiResponse<ThresholdDto>.Error("创建阈值设置失败"));
            }
        }

        /// <summary>
        /// 更新阈值设置
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<ThresholdDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateThreshold(int id, [FromBody] UpdateThresholdDto dto)
        {
            try
            {
                var threshold = await _context.Thresholds
                    .Include(t => t.Device)
                    .FirstOrDefaultAsync(t => t.Id == id);
                
                if (threshold == null)
                {
                    return NotFound(ApiResponse<ThresholdDto>.Error("阈值设置不存在"));
                }
                
                // 验证阈值是否合理
                if (dto.UpperLimit.HasValue && dto.LowerLimit.HasValue)
                {
                    if (dto.UpperLimit.Value <= dto.LowerLimit.Value)
                    {
                        return BadRequest(ApiResponse<ThresholdDto>.Error("上限值必须大于下限值"));
                    }
                }
                else if (dto.UpperLimit.HasValue && dto.UpperLimit.Value <= threshold.LowerLimit)
                {
                    return BadRequest(ApiResponse<ThresholdDto>.Error("上限值必须大于下限值"));
                }
                else if (dto.LowerLimit.HasValue && threshold.UpperLimit <= dto.LowerLimit.Value)
                {
                    return BadRequest(ApiResponse<ThresholdDto>.Error("上限值必须大于下限值"));
                }
                
                // 更新字段
                if (dto.UpperLimit.HasValue)
                {
                    threshold.UpperLimit = dto.UpperLimit.Value;
                }
                
                if (dto.LowerLimit.HasValue)
                {
                    threshold.LowerLimit = dto.LowerLimit.Value;
                }
                
                if (dto.IsRealTimeAlert.HasValue)
                {
                    threshold.IsRealTimeAlert = dto.IsRealTimeAlert.Value;
                }
                
                if (dto.AlertMessage != null)
                {
                    threshold.AlertMessage = dto.AlertMessage;
                }
                
                threshold.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                var result = new ThresholdDto
                {
                    Id = threshold.Id,
                    DeviceId = threshold.DeviceId,
                    FactorType = threshold.FactorType,
                    FactorName = threshold.FactorName ?? GetFactorName(threshold.FactorType),
                    UpperLimit = threshold.UpperLimit,
                    LowerLimit = threshold.LowerLimit,
                    IsRealTimeAlert = threshold.IsRealTimeAlert,
                    AlertMessage = threshold.AlertMessage,
                    CreatedAt = threshold.CreatedAt,
                    UpdatedAt = threshold.UpdatedAt,
                    DeviceName = threshold.Device!.DeviceName
                };
                
                // 发送阈值更新通知
                await _hubContext.Clients.Group($"device-{threshold.DeviceId}")
                    .SendAsync("ThresholdUpdated", result);
                
                _logger.LogInformation("更新阈值设置成功，ID: {ThresholdId}", id);
                
                return Ok(ApiResponse<ThresholdDto>.Success(result, "阈值设置更新成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新阈值设置失败，ID: {ThresholdId}", id);
                return StatusCode(500, ApiResponse<ThresholdDto>.Error("更新阈值设置失败"));
            }
        }

        /// <summary>
        /// 批量更新阈值设置
        /// </summary>
        [HttpPut("batch")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchUpdateThresholds([FromBody] List<UpdateThresholdDto> dtos)
        {
            try
            {
                var results = new List<ThresholdDto>();
                
                foreach (var dto in dtos)
                {
                    if (!dto.Id.HasValue)
                    {
                        continue;
                    }
                    
                    var threshold = await _context.Thresholds
                        .Include(t => t.Device)
                        .FirstOrDefaultAsync(t => t.Id == dto.Id.Value);
                    
                    if (threshold == null)
                    {
                        continue;
                    }
                    
                    // 验证阈值
                    if (dto.UpperLimit.HasValue && dto.LowerLimit.HasValue)
                    {
                        if (dto.UpperLimit.Value <= dto.LowerLimit.Value)
                        {
                            continue;
                        }
                    }
                    
                    // 更新字段
                    if (dto.UpperLimit.HasValue)
                    {
                        threshold.UpperLimit = dto.UpperLimit.Value;
                    }
                    
                    if (dto.LowerLimit.HasValue)
                    {
                        threshold.LowerLimit = dto.LowerLimit.Value;
                    }
                    
                    if (dto.IsRealTimeAlert.HasValue)
                    {
                        threshold.IsRealTimeAlert = dto.IsRealTimeAlert.Value;
                    }
                    
                    if (dto.AlertMessage != null)
                    {
                        threshold.AlertMessage = dto.AlertMessage;
                    }
                    
                    threshold.UpdatedAt = DateTime.UtcNow;
                    
                    results.Add(new ThresholdDto
                    {
                        Id = threshold.Id,
                        DeviceId = threshold.DeviceId,
                        FactorType = threshold.FactorType,
                        FactorName = threshold.FactorName ?? GetFactorName(threshold.FactorType),
                        UpperLimit = threshold.UpperLimit,
                        LowerLimit = threshold.LowerLimit,
                        IsRealTimeAlert = threshold.IsRealTimeAlert,
                        AlertMessage = threshold.AlertMessage,
                        CreatedAt = threshold.CreatedAt,
                        UpdatedAt = threshold.UpdatedAt,
                        DeviceName = threshold.Device!.DeviceName
                    });
                }
                
                await _context.SaveChangesAsync();
                
                // 发送批量更新通知
                foreach (var result in results.GroupBy(r => r.DeviceId))
                {
                    await _hubContext.Clients.Group($"device-{result.Key}")
                        .SendAsync("ThresholdsBatchUpdated", result.ToList());
                }
                
                _logger.LogInformation("批量更新阈值设置成功，共更新 {Count} 条", results.Count);
                
                return Ok(ApiResponse.Success($"批量更新成功，共更新 {results.Count} 条阈值设置"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新阈值设置失败");
                return StatusCode(500, ApiResponse.Error("批量更新失败"));
            }
        }

        /// <summary>
        /// 删除阈值设置
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteThreshold(int id)
        {
            try
            {
                var threshold = await _context.Thresholds
                    .Include(t => t.Device)
                    .FirstOrDefaultAsync(t => t.Id == id);
                
                if (threshold == null)
                {
                    return NotFound(ApiResponse.Error("阈值设置不存在"));
                }
                
                var deviceId = threshold.DeviceId;
                
                _context.Thresholds.Remove(threshold);
                await _context.SaveChangesAsync();
                
                // 发送阈值删除通知
                await _hubContext.Clients.Group($"device-{deviceId}")
                    .SendAsync("ThresholdDeleted", id);
                
                _logger.LogInformation("删除阈值设置成功，ID: {ThresholdId}", id);
                
                return Ok(ApiResponse.Success("阈值设置删除成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除阈值设置失败，ID: {ThresholdId}", id);
                return StatusCode(500, ApiResponse.Error("删除阈值设置失败"));
            }
        }

        /// <summary>
        /// 验证阈值设置
        /// </summary>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(ApiResponse<ThresholdValidationResult>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ValidateThreshold([FromBody] ValidateThresholdDto dto)
        {
            try
            {
                var result = new ThresholdValidationResult
                {
                    IsValid = true,
                    Warnings = new List<string>()
                };
                
                // 基本验证
                if (dto.UpperLimit <= dto.LowerLimit)
                {
                    result.IsValid = false;
                    result.Errors.Add("上限值必须大于下限值");
                }
                
                // 验证设备是否存在
                var device = await _context.Devices.FindAsync(dto.DeviceId);
                if (device == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("设备不存在");
                }
                
                // 验证是否已存在相同设备和因子的阈值（仅在创建时检查）
                if (dto.ThresholdId == null)
                {
                    var exists = await _context.Thresholds
                        .AnyAsync(t => t.DeviceId == dto.DeviceId && t.FactorType == dto.FactorType);
                    
                    if (exists)
                    {
                        result.IsValid = false;
                        result.Errors.Add("该设备已存在相同监测因子的阈值设置");
                    }
                }
                
                // 获取历史数据验证阈值合理性
                var historicalData = await _context.DeviceData
                    .Where(d => d.DeviceId == dto.DeviceId)
                    .OrderByDescending(d => d.Timestamp)
                    .Take(1000)
                    .ToListAsync();
                
                if (historicalData.Any())
                {
                    decimal? minValue = null;
                    decimal? maxValue = null;
                    
                    switch (dto.FactorType)
                    {
                        case FactorType.Temperature:
                            minValue = historicalData.Where(d => d.Temperature.HasValue).Min(d => d.Temperature);
                            maxValue = historicalData.Where(d => d.Temperature.HasValue).Max(d => d.Temperature);
                            break;
                        case FactorType.Humidity:
                            minValue = historicalData.Where(d => d.Humidity.HasValue).Min(d => d.Humidity);
                            maxValue = historicalData.Where(d => d.Humidity.HasValue).Max(d => d.Humidity);
                            break;
                        case FactorType.Current:
                            minValue = historicalData.Where(d => d.Current.HasValue).Min(d => d.Current);
                            maxValue = historicalData.Where(d => d.Current.HasValue).Max(d => d.Current);
                            break;
                        case FactorType.Voltage:
                            minValue = historicalData.Where(d => d.Voltage.HasValue).Min(d => d.Voltage);
                            maxValue = historicalData.Where(d => d.Voltage.HasValue).Max(d => d.Voltage);
                            break;
                    }
                    
                    if (minValue.HasValue && maxValue.HasValue)
                    {
                        var range = maxValue.Value - minValue.Value;
                        
                        if (dto.LowerLimit > minValue.Value - range * 0.5m)
                        {
                            result.Warnings.Add("下限值可能设置过高，建议降低");
                        }
                        
                        if (dto.UpperLimit < maxValue.Value + range * 0.5m)
                        {
                            result.Warnings.Add("上限值可能设置过低，建议提高");
                        }
                        
                        if (dto.UpperLimit - dto.LowerLimit < range * 0.1m)
                        {
                            result.Warnings.Add("阈值范围可能过小，建议扩大范围");
                        }
                    }
                }
                
                return Ok(ApiResponse<ThresholdValidationResult>.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证阈值设置失败");
                return StatusCode(500, ApiResponse<ThresholdValidationResult>.Error("验证失败"));
            }
        }

        private string GetFactorName(FactorType factorType)
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
    
    public class UpdateThresholdDto
    {
        public int? Id { get; set; }
        public decimal? UpperLimit { get; set; }
        public decimal? LowerLimit { get; set; }
        public bool? IsRealTimeAlert { get; set; }
        public string? AlertMessage { get; set; }
    }
    
    public class ValidateThresholdDto
    {
        public int? ThresholdId { get; set; }
        public int DeviceId { get; set; }
        public FactorType FactorType { get; set; }
        public decimal UpperLimit { get; set; }
        public decimal LowerLimit { get; set; }
    }
    
    public class ThresholdValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}