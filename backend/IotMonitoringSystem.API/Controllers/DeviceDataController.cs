using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using IotMonitoringSystem.Core.DTOs;
using IotMonitoringSystem.Infrastructure.Data;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using IotMonitoringSystem.Core.Entities;

namespace IotMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IMemoryCache _cache;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(ApplicationDbContext context, IMemoryCache cache, ILogger<DevicesController> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有设备列表
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceDto>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetDevices([FromQuery] bool? isActive = null)
        {
            try
            {
                var cacheKey = $"devices_{isActive}";
                if (!_cache.TryGetValue(cacheKey, out List<DeviceDto>? devices))
                {
                    var query = _context.Devices.AsQueryable();
                    
                    if (isActive.HasValue)
                    {
                        query = query.Where(d => d.IsActive == isActive.Value);
                    }
                    
                    devices = await query
                        .Select(d => new DeviceDto
                        {
                            Id = d.Id,
                            DeviceCode = d.DeviceCode,
                            DeviceName = d.DeviceName,
                            Location = d.Location,
                            Manufacturer = d.Manufacturer,
                            Model = d.Model,
                            SerialNumber = d.SerialNumber,
                            InstallationDate = d.InstallationDate,
                            IsActive = d.IsActive,
                            CreatedAt = d.CreatedAt,
                            UpdatedAt = d.UpdatedAt,
                            TotalDataCount = d.DeviceData.Count,
                            TodayDataCount = d.DeviceData.Count(dd => dd.Timestamp.Date == DateTime.Today),
                            ActiveAlarmCount = d.Alarms.Count(a => !a.IsAcknowledged && a.Timestamp.Date == DateTime.Today)
                        })
                        .OrderBy(d => d.DeviceName)
                        .ToListAsync();
                    
                    // 缓存5分钟
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
                    
                    _cache.Set(cacheKey, devices, cacheOptions);
                }
                
                return Ok(ApiResponse<List<DeviceDto>>.Success(devices!));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备列表失败");
                return StatusCode(500, ApiResponse.Error("获取设备列表失败"));
            }
        }

        /// <summary>
        /// 获取单个设备详情
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<DeviceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDevice(int id)
        {
            try
            {
                var device = await _context.Devices
                    .Where(d => d.Id == id)
                    .Select(d => new DeviceDto
                    {
                        Id = d.Id,
                        DeviceCode = d.DeviceCode,
                        DeviceName = d.DeviceName,
                        Location = d.Location,
                        Manufacturer = d.Manufacturer,
                        Model = d.Model,
                        SerialNumber = d.SerialNumber,
                        InstallationDate = d.InstallationDate,
                        IsActive = d.IsActive,
                        CreatedAt = d.CreatedAt,
                        UpdatedAt = d.UpdatedAt,
                        TotalDataCount = d.DeviceData.Count,
                        TodayDataCount = d.DeviceData.Count(dd => dd.Timestamp.Date == DateTime.Today),
                        ActiveAlarmCount = d.Alarms.Count(a => !a.IsAcknowledged)
                    })
                    .FirstOrDefaultAsync();
                
                if (device == null)
                {
                    return NotFound(ApiResponse.Error("设备不存在"));
                }
                
                return Ok(ApiResponse<DeviceDto>.Success(device));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备详情失败，ID: {DeviceId}", id);
                return StatusCode(500, ApiResponse.Error("获取设备详情失败"));
            }
        }

        /// <summary>
        /// 创建设备
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<DeviceDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDevice([FromBody] CreateDeviceDto dto)
        {
            try
            {
                // 检查设备编码是否重复
                var exists = await _context.Devices
                    .AnyAsync(d => d.DeviceCode == dto.DeviceCode);
                
                if (exists)
                {
                    return BadRequest(ApiResponse.Error("设备编码已存在"));
                }
                
                var device = new Device
                {
                    DeviceCode = dto.DeviceCode,
                    DeviceName = dto.DeviceName,
                    Location = dto.Location,
                    Manufacturer = dto.Manufacturer,
                    Model = dto.Model,
                    SerialNumber = dto.SerialNumber,
                    InstallationDate = dto.InstallationDate,
                    IsActive = dto.IsActive
                };
                
                _context.Devices.Add(device);
                await _context.SaveChangesAsync();
                
                // 清除缓存
                _cache.Remove("devices_");
                _cache.Remove("devices_true");
                _cache.Remove("devices_false");
                
                var result = new DeviceDto
                {
                    Id = device.Id,
                    DeviceCode = device.DeviceCode,
                    DeviceName = device.DeviceName,
                    Location = device.Location,
                    Manufacturer = device.Manufacturer,
                    Model = device.Model,
                    SerialNumber = device.SerialNumber,
                    InstallationDate = device.InstallationDate,
                    IsActive = device.IsActive,
                    CreatedAt = device.CreatedAt,
                    UpdatedAt = device.UpdatedAt
                };
                
                return CreatedAtAction(nameof(GetDevice), new { id = device.Id }, 
                    ApiResponse<DeviceDto>.Success(result, "设备创建成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建设备失败");
                return StatusCode(500, ApiResponse.Error("创建设备失败"));
            }
        }

        /// <summary>
        /// 更新设备
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<DeviceDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDevice(int id, [FromBody] UpdateDeviceDto dto)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                {
                    return NotFound(ApiResponse.Error("设备不存在"));
                }
                
                if (!string.IsNullOrEmpty(dto.DeviceName))
                {
                    device.DeviceName = dto.DeviceName;
                }
                
                if (dto.Location != null)
                {
                    device.Location = dto.Location;
                }
                
                if (dto.IsActive.HasValue)
                {
                    device.IsActive = dto.IsActive.Value;
                }
                
                device.UpdatedAt = DateTime.UtcNow;
                
                await _context.SaveChangesAsync();
                
                // 清除缓存
                _cache.Remove("devices_");
                _cache.Remove($"device_{id}");
                
                var result = new DeviceDto
                {
                    Id = device.Id,
                    DeviceCode = device.DeviceCode,
                    DeviceName = device.DeviceName,
                    Location = device.Location,
                    Manufacturer = device.Manufacturer,
                    Model = device.Model,
                    SerialNumber = device.SerialNumber,
                    InstallationDate = device.InstallationDate,
                    IsActive = device.IsActive,
                    CreatedAt = device.CreatedAt,
                    UpdatedAt = device.UpdatedAt
                };
                
                return Ok(ApiResponse<DeviceDto>.Success(result, "设备更新成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新设备失败，ID: {DeviceId}", id);
                return StatusCode(500, ApiResponse.Error("更新设备失败"));
            }
        }

        /// <summary>
        /// 删除设备
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDevice(int id)
        {
            try
            {
                var device = await _context.Devices.FindAsync(id);
                if (device == null)
                {
                    return NotFound(ApiResponse.Error("设备不存在"));
                }
                
                _context.Devices.Remove(device);
                await _context.SaveChangesAsync();
                
                // 清除缓存
                _cache.Remove("devices_");
                _cache.Remove($"device_{id}");
                
                return Ok(ApiResponse.Success("设备删除成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除设备失败，ID: {DeviceId}", id);
                return StatusCode(500, ApiResponse.Error("删除设备失败"));
            }
        }

        /// <summary>
        /// 获取设备状态统计
        /// </summary>
        [HttpGet("status-summary")]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceStatusSummaryDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeviceStatusSummary()
        {
            try
            {
                var summary = await _context.Devices
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
                
                return Ok(ApiResponse<List<DeviceStatusSummaryDto>>.Success(summary));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备状态统计失败");
                return StatusCode(500, ApiResponse.Error("获取设备状态统计失败"));
            }
        }
    }
    
    /// <summary>
    /// API响应封装类
    /// </summary>
    public class ApiResponse<T>
    {
        public bool SuccessType { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public static ApiResponse<T> Success(T data, string? message = null)
        {
            return new ApiResponse<T>
            {
                SuccessType = true,
                Message = message ?? "请求成功",
                Data = data
            };
        }
        
        public static ApiResponse<T> Error(string message)
        {
            return new ApiResponse<T>
            {
                SuccessType = false,
                Message = message
            };
        }
    }
    
    public class ApiResponse
    {
        public bool SuccessType { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public static ApiResponse Success(string? message = null)
        {
            return new ApiResponse
            {
                SuccessType = true,
                Message = message ?? "请求成功"
            };
        }
        
        public static ApiResponse Error(string message)
        {
            return new ApiResponse
            {
                SuccessType = false,
                Message = message
            };
        }
    }
}