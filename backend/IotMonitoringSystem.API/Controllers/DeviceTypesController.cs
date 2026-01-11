using IotMonitoringSystem.Core.DTOs;
using IotMonitoringSystem.Core.Entities;
using IotMonitoringSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace IotMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceTypesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeviceTypesController> _logger;

        public DeviceTypesController(
            ApplicationDbContext context,
            ILogger<DeviceTypesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有设备类型
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceTypeDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeviceTypes([FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.DeviceTypes
                    .Include(dt => dt.Devices)
                    .Include(dt => dt.DeviceTypeFactors)
                    .ThenInclude(dtf => dtf.DeviceFactor)
                    .AsQueryable();

                if (isActive.HasValue)
                {
                    query = query.Where(dt => dt.IsActive == isActive.Value);
                }

                var deviceTypes = await query
                    .OrderBy(dt => dt.TypeName)
                    .Select(dt => new DeviceTypeDto
                    {
                        Id = dt.Id,
                        TypeCode = dt.TypeCode,
                        TypeName = dt.TypeName,
                        Description = dt.Description,
                        Icon = dt.Icon,
                        IsActive = dt.IsActive,
                        CreatedAt = dt.CreatedAt,
                        UpdatedAt = dt.UpdatedAt,
                        DeviceCount = dt.Devices.Count,
                        FactorCount = dt.DeviceTypeFactors.Count,
                        Factors = dt.DeviceTypeFactors
                            .Where(dtf => dtf.IsVisible)
                            .OrderBy(dtf => dtf.SortOrder)
                            .Select(dtf => new DeviceFactorSimpleDto
                            {
                                Id = dtf.DeviceFactor!.Id,
                                FactorCode = dtf.DeviceFactor.FactorCode,
                                FactorName = dtf.DeviceFactor.FactorName,
                                Unit = dtf.DeviceFactor.Unit,
                                IsVisible = dtf.IsVisible,
                                IsRequired = dtf.IsRequired,
                                SortOrder = dtf.SortOrder
                            })
                            .ToList()
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<DeviceTypeDto>>.Success(deviceTypes));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备类型列表失败");
                return StatusCode(500, ApiResponse<List<DeviceTypeDto>>.Error("获取设备类型列表失败"));
            }
        }

        /// <summary>
        /// 获取单个设备类型
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<DeviceTypeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDeviceType(int id)
        {
            try
            {
                var deviceType = await _context.DeviceTypes
                    .Include(dt => dt.Devices)
                    .Include(dt => dt.DeviceTypeFactors)
                    .ThenInclude(dtf => dtf.DeviceFactor)
                    .Where(dt => dt.Id == id)
                    .Select(dt => new DeviceTypeDto
                    {
                        Id = dt.Id,
                        TypeCode = dt.TypeCode,
                        TypeName = dt.TypeName,
                        Description = dt.Description,
                        Icon = dt.Icon,
                        IsActive = dt.IsActive,
                        CreatedAt = dt.CreatedAt,
                        UpdatedAt = dt.UpdatedAt,
                        DeviceCount = dt.Devices.Count,
                        FactorCount = dt.DeviceTypeFactors.Count,
                        Factors = dt.DeviceTypeFactors
                            .Where(dtf => dtf.IsVisible)
                            .OrderBy(dtf => dtf.SortOrder)
                            .Select(dtf => new DeviceFactorSimpleDto
                            {
                                Id = dtf.DeviceFactor!.Id,
                                FactorCode = dtf.DeviceFactor.FactorCode,
                                FactorName = dtf.DeviceFactor.FactorName,
                                Unit = dtf.DeviceFactor.Unit,
                                IsVisible = dtf.IsVisible,
                                IsRequired = dtf.IsRequired,
                                SortOrder = dtf.SortOrder
                            })
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (deviceType == null)
                {
                    return NotFound(ApiResponse<DeviceTypeDto>.Error("设备类型不存在"));
                }

                return Ok(ApiResponse<DeviceTypeDto>.Success(deviceType));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备类型失败，ID: {DeviceTypeId}", id);
                return StatusCode(500, ApiResponse<DeviceTypeDto>.Error("获取设备类型失败"));
            }
        }

        /// <summary>
        /// 创建设备类型
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<DeviceTypeDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDeviceType([FromBody] CreateDeviceTypeDto dto)
        {
            try
            {
                // 验证类型编码是否重复
                var exists = await _context.DeviceTypes
                    .AnyAsync(dt => dt.TypeCode == dto.TypeCode);

                if (exists)
                {
                    return BadRequest(ApiResponse<DeviceTypeDto>.Error("设备类型编码已存在"));
                }

                var deviceType = new DeviceType
                {
                    TypeCode = dto.TypeCode,
                    TypeName = dto.TypeName,
                    Description = dto.Description,
                    Icon = dto.Icon,
                    IsActive = dto.IsActive
                };

                _context.DeviceTypes.Add(deviceType);
                await _context.SaveChangesAsync();

                // 关联监测因子
                if (dto.FactorIds != null && dto.FactorIds.Any())
                {
                    await AssociateFactors(deviceType.Id, dto.FactorIds);
                }

                var result = await GetDeviceTypeDto(deviceType.Id);

                _logger.LogInformation("创建设备类型成功，编码: {TypeCode}", dto.TypeCode);

                return CreatedAtAction(nameof(GetDeviceType), new { id = deviceType.Id },
                    ApiResponse<DeviceTypeDto>.Success(result, "设备类型创建成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建设备类型失败");
                return StatusCode(500, ApiResponse<DeviceTypeDto>.Error("创建设备类型失败"));
            }
        }

        /// <summary>
        /// 更新设备类型
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<DeviceTypeDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDeviceType(int id, [FromBody] UpdateDeviceTypeDto dto)
        {
            try
            {
                var deviceType = await _context.DeviceTypes.FindAsync(id);
                if (deviceType == null)
                {
                    return NotFound(ApiResponse<DeviceTypeDto>.Error("设备类型不存在"));
                }

                // 更新字段
                if (!string.IsNullOrEmpty(dto.TypeName))
                {
                    deviceType.TypeName = dto.TypeName;
                }

                if (dto.Description != null)
                {
                    deviceType.Description = dto.Description;
                }

                if (dto.Icon != null)
                {
                    deviceType.Icon = dto.Icon;
                }

                if (dto.IsActive.HasValue)
                {
                    deviceType.IsActive = dto.IsActive.Value;
                }

                deviceType.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // 更新监测因子关联
                if (dto.FactorIds != null)
                {
                    await UpdateFactorAssociations(id, dto.FactorIds);
                }

                var result = await GetDeviceTypeDto(id);

                _logger.LogInformation("更新设备类型成功，ID: {DeviceTypeId}", id);

                return Ok(ApiResponse<DeviceTypeDto>.Success(result, "设备类型更新成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新设备类型失败，ID: {DeviceTypeId}", id);
                return StatusCode(500, ApiResponse<DeviceTypeDto>.Error("更新设备类型失败"));
            }
        }

        /// <summary>
        /// 删除设备类型
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDeviceType(int id)
        {
            try
            {
                var deviceType = await _context.DeviceTypes
                    .Include(dt => dt.Devices)
                    .Include(dt => dt.DeviceTypeFactors)
                    .FirstOrDefaultAsync(dt => dt.Id == id);

                if (deviceType == null)
                {
                    return NotFound(ApiResponse.Error("设备类型不存在"));
                }

                // 检查是否有关联的设备
                if (deviceType.Devices.Any())
                {
                    return BadRequest(ApiResponse.Error("该设备类型有关联的设备，无法删除"));
                }

                // 删除关联关系
                _context.DeviceTypeFactors.RemoveRange(deviceType.DeviceTypeFactors);

                // 删除设备类型
                _context.DeviceTypes.Remove(deviceType);

                await _context.SaveChangesAsync();

                _logger.LogInformation("删除设备类型成功，ID: {DeviceTypeId}", id);

                return Ok(ApiResponse.Success("设备类型删除成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除设备类型失败，ID: {DeviceTypeId}", id);
                return StatusCode(500, ApiResponse.Error("删除设备类型失败"));
            }
        }

        /// <summary>
        /// 获取设备类型的监测因子映射
        /// </summary>
        [HttpGet("{id}/factor-mapping")]
        [ProducesResponseType(typeof(ApiResponse<DeviceFactorMappingDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeviceFactorMapping(int id)
        {
            try
            {
                var deviceType = await _context.DeviceTypes
                    .Include(dt => dt.DeviceTypeFactors)
                    .ThenInclude(dtf => dtf.DeviceFactor)
                    .FirstOrDefaultAsync(dt => dt.Id == id);

                if (deviceType == null)
                {
                    return NotFound(ApiResponse<DeviceFactorMappingDto>.Error("设备类型不存在"));
                }

                var mapping = new DeviceFactorMappingDto
                {
                    DeviceTypeId = deviceType.Id,
                    DeviceTypeName = deviceType.TypeName,
                    Factors = deviceType.DeviceTypeFactors
                        .OrderBy(dtf => dtf.SortOrder)
                        .Select(dtf => new DeviceFactorMappingItemDto
                        {
                            FactorId = dtf.DeviceFactor!.Id,
                            FactorName = dtf.DeviceFactor.FactorName,
                            Unit = dtf.DeviceFactor.Unit,
                            IsVisible = dtf.IsVisible,
                            IsRequired = dtf.IsRequired,
                            SortOrder = dtf.SortOrder
                        })
                        .ToList()
                };

                return Ok(ApiResponse<DeviceFactorMappingDto>.Success(mapping));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备类型监测因子映射失败，ID: {DeviceTypeId}", id);
                return StatusCode(500, ApiResponse<DeviceFactorMappingDto>.Error("获取映射信息失败"));
            }
        }

        /// <summary>
        /// 更新设备类型的监测因子映射
        /// </summary>
        [HttpPut("{id}/factor-mapping")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateDeviceFactorMapping(int id, [FromBody] UpdateFactorMappingDto dto)
        {
            try
            {
                var deviceType = await _context.DeviceTypes
                    .Include(dt => dt.DeviceTypeFactors)
                    .FirstOrDefaultAsync(dt => dt.Id == id);

                if (deviceType == null)
                {
                    return NotFound(ApiResponse.Error("设备类型不存在"));
                }

                // 清除现有关联
                _context.DeviceTypeFactors.RemoveRange(deviceType.DeviceTypeFactors);

                // 添加新关联
                var newAssociations = dto.Factors.Select(f => new DeviceTypeFactor
                {
                    DeviceTypeId = id,
                    FactorId = f.FactorId,
                    IsVisible = f.IsVisible,
                    IsRequired = f.IsRequired,
                    SortOrder = f.SortOrder
                }).ToList();

                await _context.DeviceTypeFactors.AddRangeAsync(newAssociations);
                await _context.SaveChangesAsync();

                _logger.LogInformation("更新设备类型监测因子映射成功，ID: {DeviceTypeId}", id);

                return Ok(ApiResponse.Success("监测因子映射更新成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新设备类型监测因子映射失败，ID: {DeviceTypeId}", id);
                return StatusCode(500, ApiResponse.Error("更新映射信息失败"));
            }
        }

        /// <summary>
        /// 获取可用的监测因子（未关联到该设备类型的）
        /// </summary>
        [HttpGet("{id}/available-factors")]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceFactorDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAvailableFactors(int id)
        {
            try
            {
                var deviceType = await _context.DeviceTypes.FindAsync(id);
                if (deviceType == null)
                {
                    return NotFound(ApiResponse<List<DeviceFactorDto>>.Error("设备类型不存在"));
                }

                var associatedFactorIds = await _context.DeviceTypeFactors
                    .Where(dtf => dtf.DeviceTypeId == id)
                    .Select(dtf => dtf.FactorId)
                    .ToListAsync();

                var availableFactors = await _context.DeviceFactors
                    .Where(f => !associatedFactorIds.Contains(f.Id) && f.IsActive)
                    .OrderBy(f => f.SortOrder)
                    .ThenBy(f => f.FactorName)
                    .Select(f => new DeviceFactorDto
                    {
                        Id = f.Id,
                        FactorCode = f.FactorCode,
                        FactorName = f.FactorName,
                        DisplayName = f.DisplayName,
                        Category = f.Category,
                        CategoryName = GetCategoryName(f.Category),
                        Unit = f.Unit,
                        DefaultUpperLimit = f.DefaultUpperLimit,
                        DefaultLowerLimit = f.DefaultLowerLimit,
                        Description = f.Description,
                        DataType = f.DataType,
                        Precision = f.Precision,
                        Scale = f.Scale,
                        IsRequired = f.IsRequired,
                        IsActive = f.IsActive,
                        SortOrder = f.SortOrder,
                        Icon = f.Icon,
                        Color = f.Color
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<DeviceFactorDto>>.Success(availableFactors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取可用监测因子失败，设备类型ID: {DeviceTypeId}", id);
                return StatusCode(500, ApiResponse<List<DeviceFactorDto>>.Error("获取可用监测因子失败"));
            }
        }

        // 辅助方法
        private async Task<DeviceTypeDto> GetDeviceTypeDto(int deviceTypeId)
        {
            return await _context.DeviceTypes
                .Include(dt => dt.Devices)
                .Include(dt => dt.DeviceTypeFactors)
                .ThenInclude(dtf => dtf.DeviceFactor)
                .Where(dt => dt.Id == deviceTypeId)
                .Select(dt => new DeviceTypeDto
                {
                    Id = dt.Id,
                    TypeCode = dt.TypeCode,
                    TypeName = dt.TypeName,
                    Description = dt.Description,
                    Icon = dt.Icon,
                    IsActive = dt.IsActive,
                    CreatedAt = dt.CreatedAt,
                    UpdatedAt = dt.UpdatedAt,
                    DeviceCount = dt.Devices.Count,
                    FactorCount = dt.DeviceTypeFactors.Count,
                    Factors = dt.DeviceTypeFactors
                        .Where(dtf => dtf.IsVisible)
                        .OrderBy(dtf => dtf.SortOrder)
                        .Select(dtf => new DeviceFactorSimpleDto
                        {
                            Id = dtf.DeviceFactor!.Id,
                            FactorCode = dtf.DeviceFactor.FactorCode,
                            FactorName = dtf.DeviceFactor.FactorName,
                            Unit = dtf.DeviceFactor.Unit,
                            IsVisible = dtf.IsVisible,
                            IsRequired = dtf.IsRequired,
                            SortOrder = dtf.SortOrder
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        private async Task AssociateFactors(int deviceTypeId, List<int> factorIds)
        {
            var associations = factorIds.Select(factorId => new DeviceTypeFactor
            {
                DeviceTypeId = deviceTypeId,
                FactorId = factorId,
                IsVisible = true,
                IsRequired = false,
                SortOrder = 0
            }).ToList();

            await _context.DeviceTypeFactors.AddRangeAsync(associations);
            await _context.SaveChangesAsync();
        }

        private async Task UpdateFactorAssociations(int deviceTypeId, List<int> factorIds)
        {
            // 清除现有关联
            var existingAssociations = await _context.DeviceTypeFactors
                .Where(dtf => dtf.DeviceTypeId == deviceTypeId)
                .ToListAsync();

            _context.DeviceTypeFactors.RemoveRange(existingAssociations);

            // 添加新关联
            var newAssociations = factorIds.Select(factorId => new DeviceTypeFactor
            {
                DeviceTypeId = deviceTypeId,
                FactorId = factorId,
                IsVisible = true,
                IsRequired = false,
                SortOrder = 0
            }).ToList();

            await _context.DeviceTypeFactors.AddRangeAsync(newAssociations);
            await _context.SaveChangesAsync();
        }

        private string GetCategoryName(FactorCategory category)
        {
            return category switch
            {
                FactorCategory.General => "通用",
                FactorCategory.Environmental => "环境",
                FactorCategory.Electrical => "电气",
                FactorCategory.Mechanical => "机械",
                FactorCategory.Safety => "安全",
                FactorCategory.Performance => "性能",
                FactorCategory.Quality => "质量",
                FactorCategory.Custom => "自定义",
                _ => "未知"
            };
        }
    }

    public class CreateDeviceTypeDto
    {
        [Required(ErrorMessage = "设备类型编码不能为空")]
        [StringLength(50, ErrorMessage = "设备类型编码长度不能超过50个字符")]
        [RegularExpression(@"^[A-Z][A-Z0-9_]*$", ErrorMessage = "设备类型编码必须以大写字母开头，只能包含大写字母、数字和下划线")]
        public string TypeCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "设备类型名称不能为空")]
        [StringLength(100, ErrorMessage = "设备类型名称长度不能超过100个字符")]
        public string TypeName { get; set; } = string.Empty;

        [StringLength(200, ErrorMessage = "描述长度不能超过200个字符")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "图标长度不能超过100个字符")]
        public string? Icon { get; set; }

        public bool IsActive { get; set; } = true;

        // 关联的监测因子
        public List<int>? FactorIds { get; set; }
    }

    public class UpdateDeviceTypeDto
    {
        [StringLength(100, ErrorMessage = "设备类型名称长度不能超过100个字符")]
        public string? TypeName { get; set; }

        [StringLength(200, ErrorMessage = "描述长度不能超过200个字符")]
        public string? Description { get; set; }

        [StringLength(100, ErrorMessage = "图标长度不能超过100个字符")]
        public string? Icon { get; set; }

        public bool? IsActive { get; set; }

        // 关联的监测因子
        public List<int>? FactorIds { get; set; }
    }

    public class UpdateFactorMappingDto
    {
        public List<FactorMappingItemDto> Factors { get; set; } = new();
    }

    public class FactorMappingItemDto
    {
        public int FactorId { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsRequired { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }
}