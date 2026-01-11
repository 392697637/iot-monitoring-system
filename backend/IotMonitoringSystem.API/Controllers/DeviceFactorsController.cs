using IotMonitoringSystem.Core.DTOs;
using IotMonitoringSystem.Core.Entities;
using IotMonitoringSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq.Dynamic.Core;

namespace IotMonitoringSystem.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceFactorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeviceFactorsController> _logger;

        public DeviceFactorsController(
            ApplicationDbContext context,
            ILogger<DeviceFactorsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有监测因子
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<DeviceFactorDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDeviceFactors(
            [FromQuery] string? search = null,
            [FromQuery] FactorCategory? category = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = "SortOrder",
            [FromQuery] bool sortDescending = false)
        {
            try
            {
                var query = _context.DeviceFactors
                    .Include(f => f.DeviceTypeFactors)
                    .ThenInclude(dtf => dtf.DeviceType)
                    .AsQueryable();

                // 应用筛选条件
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(f =>
                        f.FactorCode.Contains(search) ||
                        f.FactorName.Contains(search) ||
                        f.DisplayName!.Contains(search) ||
                        f.Description!.Contains(search));
                }

                if (category.HasValue)
                {
                    query = query.Where(f => f.Category == category.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(f => f.IsActive == isActive.Value);
                }

                // 动态排序
                var sortDirection = sortDescending ? "DESC" : "ASC";
                if (!string.IsNullOrEmpty(sortBy))
                {
                    query = query.OrderBy($"{sortBy} {sortDirection}");
                }
                else
                {
                    query = query.OrderBy(f => f.SortOrder).ThenBy(f => f.FactorName);
                }

                var totalCount = await query.CountAsync();
                var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

                var factors = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
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
                        Color = f.Color,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt,
                        ThresholdCount = f.Thresholds.Count,
                        DeviceTypeCount = f.DeviceTypeFactors.Count,
                        DeviceTypeNames = f.DeviceTypeFactors
                            .Select(dtf => dtf.DeviceType!.TypeName)
                            .ToList()
                    })
                    .ToListAsync();

                var result = new PagedResult<DeviceFactorDto>
                {
                    Data = factors,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = totalPages,
                    HasPreviousPage = page > 1,
                    HasNextPage = page < totalPages
                };

                return Ok(ApiResponse<PagedResult<DeviceFactorDto>>.Success(result));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取监测因子列表失败");
                return StatusCode(500, ApiResponse<PagedResult<DeviceFactorDto>>.Error("获取监测因子列表失败"));
            }
        }

        /// <summary>
        /// 获取单个监测因子
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<DeviceFactorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDeviceFactor(int id)
        {
            try
            {
                var factor = await _context.DeviceFactors
                    .Include(f => f.DeviceTypeFactors)
                    .ThenInclude(dtf => dtf.DeviceType)
                    .Where(f => f.Id == id)
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
                        Color = f.Color,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt,
                        ThresholdCount = f.Thresholds.Count,
                        DeviceTypeCount = f.DeviceTypeFactors.Count,
                        DeviceTypeNames = f.DeviceTypeFactors
                            .Select(dtf => dtf.DeviceType!.TypeName)
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (factor == null)
                {
                    return NotFound(ApiResponse<DeviceFactorDto>.Error("监测因子不存在"));
                }

                return Ok(ApiResponse<DeviceFactorDto>.Success(factor));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取监测因子失败，ID: {FactorId}", id);
                return StatusCode(500, ApiResponse<DeviceFactorDto>.Error("获取监测因子失败"));
            }
        }

        /// <summary>
        /// 根据编码获取监测因子
        /// </summary>
        [HttpGet("code/{code}")]
        [ProducesResponseType(typeof(ApiResponse<DeviceFactorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDeviceFactorByCode(string code)
        {
            try
            {
                var factor = await _context.DeviceFactors
                    .Where(f => f.FactorCode == code)
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
                        Color = f.Color,
                        CreatedAt = f.CreatedAt,
                        UpdatedAt = f.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (factor == null)
                {
                    return NotFound(ApiResponse<DeviceFactorDto>.Error("监测因子不存在"));
                }

                return Ok(ApiResponse<DeviceFactorDto>.Success(factor));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取监测因子失败，编码: {FactorCode}", code);
                return StatusCode(500, ApiResponse<DeviceFactorDto>.Error("获取监测因子失败"));
            }
        }

        /// <summary>
        /// 创建设备监测因子
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<DeviceFactorDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateDeviceFactor([FromBody] CreateDeviceFactorDto dto)
        {
            try
            {
                // 验证因子编码是否重复
                var exists = await _context.DeviceFactors
                    .AnyAsync(f => f.FactorCode == dto.FactorCode);

                if (exists)
                {
                    return BadRequest(ApiResponse<DeviceFactorDto>.Error("因子编码已存在"));
                }

                // 验证默认阈值
                if (dto.DefaultUpperLimit.HasValue && dto.DefaultLowerLimit.HasValue)
                {
                    if (dto.DefaultUpperLimit <= dto.DefaultLowerLimit)
                    {
                        return BadRequest(ApiResponse<DeviceFactorDto>.Error("默认上限值必须大于下限值"));
                    }
                }

                var factor = new DeviceFactor
                {
                    FactorCode = dto.FactorCode,
                    FactorName = dto.FactorName,
                    DisplayName = dto.DisplayName,
                    Category = dto.Category,
                    Unit = dto.Unit,
                    DefaultUpperLimit = dto.DefaultUpperLimit,
                    DefaultLowerLimit = dto.DefaultLowerLimit,
                    Description = dto.Description,
                    DataType = dto.DataType,
                    Precision = dto.Precision,
                    Scale = dto.Scale,
                    IsRequired = dto.IsRequired,
                    IsActive = dto.IsActive,
                    SortOrder = dto.SortOrder,
                    Icon = dto.Icon,
                    Color = dto.Color
                };

                _context.DeviceFactors.Add(factor);
                await _context.SaveChangesAsync();

                // 关联设备类型
                if (dto.DeviceTypeIds != null && dto.DeviceTypeIds.Any())
                {
                    await AssociateDeviceTypes(factor.Id, dto.DeviceTypeIds);
                }

                // 重新加载获取完整数据
                var result = await GetFactorDto(factor.Id);

                _logger.LogInformation("创建设备监测因子成功，编码: {FactorCode}", dto.FactorCode);

                return CreatedAtAction(nameof(GetDeviceFactor), new { id = factor.Id },
                    ApiResponse<DeviceFactorDto>.Success(result, "监测因子创建成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建设备监测因子失败");
                return StatusCode(500, ApiResponse<DeviceFactorDto>.Error("创建监测因子失败"));
            }
        }

        /// <summary>
        /// 更新监测因子
        /// </summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponse<DeviceFactorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateDeviceFactor(int id, [FromBody] UpdateDeviceFactorDto dto)
        {
            try
            {
                var factor = await _context.DeviceFactors.FindAsync(id);
                if (factor == null)
                {
                    return NotFound(ApiResponse<DeviceFactorDto>.Error("监测因子不存在"));
                }

                // 验证默认阈值
                if (dto.DefaultUpperLimit.HasValue && dto.DefaultLowerLimit.HasValue)
                {
                    if (dto.DefaultUpperLimit <= dto.DefaultLowerLimit)
                    {
                        return BadRequest(ApiResponse<DeviceFactorDto>.Error("默认上限值必须大于下限值"));
                    }
                }

                // 更新字段
                if (!string.IsNullOrEmpty(dto.FactorName))
                {
                    factor.FactorName = dto.FactorName;
                }

                if (dto.DisplayName != null)
                {
                    factor.DisplayName = dto.DisplayName;
                }

                if (dto.Category.HasValue)
                {
                    factor.Category = dto.Category.Value;
                }

                if (dto.Unit != null)
                {
                    factor.Unit = dto.Unit;
                }

                if (dto.DefaultUpperLimit.HasValue)
                {
                    factor.DefaultUpperLimit = dto.DefaultUpperLimit;
                }

                if (dto.DefaultLowerLimit.HasValue)
                {
                    factor.DefaultLowerLimit = dto.DefaultLowerLimit;
                }

                if (dto.Description != null)
                {
                    factor.Description = dto.Description;
                }

                if (dto.DataType.HasValue)
                {
                    factor.DataType = dto.DataType.Value;
                }

                if (dto.Precision.HasValue)
                {
                    factor.Precision = dto.Precision;
                }

                if (dto.Scale.HasValue)
                {
                    factor.Scale = dto.Scale;
                }

                if (dto.IsRequired.HasValue)
                {
                    factor.IsRequired = dto.IsRequired.Value;
                }

                if (dto.IsActive.HasValue)
                {
                    factor.IsActive = dto.IsActive.Value;
                }

                if (dto.SortOrder.HasValue)
                {
                    factor.SortOrder = dto.SortOrder.Value;
                }

                if (dto.Icon != null)
                {
                    factor.Icon = dto.Icon;
                }

                if (dto.Color != null)
                {
                    factor.Color = dto.Color;
                }

                factor.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // 更新设备类型关联
                if (dto.DeviceTypeIds != null)
                {
                    await UpdateDeviceTypeAssociations(id, dto.DeviceTypeIds);
                }

                var result = await GetFactorDto(id);

                _logger.LogInformation("更新监测因子成功，ID: {FactorId}", id);

                return Ok(ApiResponse<DeviceFactorDto>.Success(result, "监测因子更新成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新监测因子失败，ID: {FactorId}", id);
                return StatusCode(500, ApiResponse<DeviceFactorDto>.Error("更新监测因子失败"));
            }
        }

        /// <summary>
        /// 删除监测因子
        /// </summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteDeviceFactor(int id)
        {
            try
            {
                var factor = await _context.DeviceFactors
                    .Include(f => f.Thresholds)
                    .Include(f => f.DeviceTypeFactors)
                    .FirstOrDefaultAsync(f => f.Id == id);

                if (factor == null)
                {
                    return NotFound(ApiResponse.Error("监测因子不存在"));
                }

                // 检查是否有关联的阈值
                if (factor.Thresholds.Any())
                {
                    return BadRequest(ApiResponse.Error("该监测因子有关联的阈值设置，无法删除"));
                }

                // 删除设备类型关联
                _context.DeviceTypeFactors.RemoveRange(factor.DeviceTypeFactors);

                // 删除监测因子
                _context.DeviceFactors.Remove(factor);

                await _context.SaveChangesAsync();

                _logger.LogInformation("删除监测因子成功，ID: {FactorId}", id);

                return Ok(ApiResponse.Success("监测因子删除成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除监测因子失败，ID: {FactorId}", id);
                return StatusCode(500, ApiResponse.Error("删除监测因子失败"));
            }
        }

        /// <summary>
        /// 获取监测因子分类
        /// </summary>
        [HttpGet("categories")]
        [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), StatusCodes.Status200OK)]
        public IActionResult GetFactorCategories()
        {
            var categories = Enum.GetValues(typeof(FactorCategory))
                .Cast<FactorCategory>()
                .Select(c => new CategoryDto
                {
                    Value = (int)c,
                    Name = GetCategoryName(c),
                    Description = GetCategoryDescription(c)
                })
                .ToList();

            return Ok(ApiResponse<List<CategoryDto>>.Success(categories));
        }

        /// <summary>
        /// 获取数据类型
        /// </summary>
        [HttpGet("data-types")]
        [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), StatusCodes.Status200OK)]
        public IActionResult GetDataTypes()
        {
            var dataTypes = Enum.GetValues(typeof(IotDataType))
                .Cast<IotDataType>()
                .Select(d => new CategoryDto
                {
                    Value = (int)d,
                    Name = GetDataTypeName(d),
                    Description = GetDataTypeDescription(d)
                })
                .ToList();

            return Ok(ApiResponse<List<CategoryDto>>.Success(dataTypes));
        }

        /// <summary>
        /// 获取设备类型关联的监测因子
        /// </summary>
        [HttpGet("device-type/{deviceTypeId}")]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceFactorDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFactorsByDeviceType(int deviceTypeId)
        {
            try
            {
                var factors = await _context.DeviceTypeFactors
                    .Where(dtf => dtf.DeviceTypeId == deviceTypeId && dtf.IsVisible)
                    .Include(dtf => dtf.DeviceFactor)
                    .OrderBy(dtf => dtf.SortOrder)
                    .Select(dtf => new DeviceFactorDto
                    {
                        Id = dtf.DeviceFactor!.Id,
                        FactorCode = dtf.DeviceFactor.FactorCode,
                        FactorName = dtf.DeviceFactor.FactorName,
                        DisplayName = dtf.DeviceFactor.DisplayName,
                        Category = dtf.DeviceFactor.Category,
                        CategoryName = GetCategoryName(dtf.DeviceFactor.Category),
                        Unit = dtf.DeviceFactor.Unit,
                        DefaultUpperLimit = dtf.DeviceFactor.DefaultUpperLimit,
                        DefaultLowerLimit = dtf.DeviceFactor.DefaultLowerLimit,
                        Description = dtf.DeviceFactor.Description,
                        DataType = dtf.DeviceFactor.DataType,
                        Precision = dtf.DeviceFactor.Precision,
                        Scale = dtf.DeviceFactor.Scale,
                        IsRequired = dtf.IsRequired,
                        IsActive = dtf.DeviceFactor.IsActive,
                        SortOrder = dtf.SortOrder,
                        Icon = dtf.DeviceFactor.Icon,
                        Color = dtf.DeviceFactor.Color,
                        CreatedAt = dtf.DeviceFactor.CreatedAt,
                        UpdatedAt = dtf.DeviceFactor.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<DeviceFactorDto>>.Success(factors));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取设备类型关联的监测因子失败，设备类型ID: {DeviceTypeId}", deviceTypeId);
                return StatusCode(500, ApiResponse<List<DeviceFactorDto>>.Error("获取监测因子失败"));
            }
        }

        /// <summary>
        /// 获取监测因子统计
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(ApiResponse<DeviceFactorStatisticsDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetFactorStatistics()
        {
            try
            {
                var totalFactors = await _context.DeviceFactors.CountAsync();
                var activeFactors = await _context.DeviceFactors.CountAsync(f => f.IsActive);

                // 分类分布
                var categoryDistribution = await _context.DeviceFactors
                    .GroupBy(f => f.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(
                        x => GetCategoryName(x.Category),
                        x => x.Count
                    );

                // 数据类型分布
                var dataTypeDistribution = await _context.DeviceFactors
                    .GroupBy(f => f.DataType)
                    .Select(g => new
                    {
                        DataType = g.Key,
                        Count = g.Count()
                    })
                    .ToDictionaryAsync(
                        x => GetDataTypeName(x.DataType),
                        x => x.Count
                    );

                // 最常用的监测因子
                var mostUsedFactors = await _context.DeviceFactors
                    .Select(f => new FactorUsageDto
                    {
                        FactorName = f.FactorName,
                        DeviceCount = f.DeviceTypeFactors.Count,
                        ThresholdCount = f.Thresholds.Count,
                        DataRecordCount = 0 // 这里需要根据实际数据表统计
                    })
                    .OrderByDescending(f => f.ThresholdCount)
                    .ThenByDescending(f => f.DeviceCount)
                    .Take(10)
                    .ToListAsync();

                var statistics = new DeviceFactorStatisticsDto
                {
                    TotalFactors = totalFactors,
                    ActiveFactors = activeFactors,
                    CategoryDistribution = categoryDistribution,
                    DataTypeDistribution = dataTypeDistribution,
                    MostUsedFactors = mostUsedFactors
                };

                return Ok(ApiResponse<DeviceFactorStatisticsDto>.Success(statistics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取监测因子统计失败");
                return StatusCode(500, ApiResponse<DeviceFactorStatisticsDto>.Error("获取统计信息失败"));
            }
        }

        /// <summary>
        /// 批量更新监测因子排序
        /// </summary>
        [HttpPut("batch/sort-order")]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> BatchUpdateSortOrder([FromBody] List<SortOrderDto> sortOrders)
        {
            try
            {
                var factorIds = sortOrders.Select(s => s.Id).ToList();
                var factors = await _context.DeviceFactors
                    .Where(f => factorIds.Contains(f.Id))
                    .ToListAsync();

                foreach (var factor in factors)
                {
                    var sortOrder = sortOrders.FirstOrDefault(s => s.Id == factor.Id);
                    if (sortOrder != null)
                    {
                        factor.SortOrder = sortOrder.SortOrder;
                        factor.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(ApiResponse.Success("排序更新成功"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量更新监测因子排序失败");
                return StatusCode(500, ApiResponse.Error("排序更新失败"));
            }
        }

        // 辅助方法
        private async Task<DeviceFactorDto> GetFactorDto(int factorId)
        {
            return await _context.DeviceFactors
                .Include(f => f.DeviceTypeFactors)
                .ThenInclude(dtf => dtf.DeviceType)
                .Where(f => f.Id == factorId)
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
                    Color = f.Color,
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt,
                    ThresholdCount = f.Thresholds.Count,
                    DeviceTypeCount = f.DeviceTypeFactors.Count,
                    DeviceTypeNames = f.DeviceTypeFactors
                        .Select(dtf => dtf.DeviceType!.TypeName)
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        private async Task AssociateDeviceTypes(int factorId, List<int> deviceTypeIds)
        {
            var existingAssociations = await _context.DeviceTypeFactors
                .Where(dtf => dtf.FactorId == factorId)
                .ToListAsync();

            // 删除不再关联的设备类型
            var toRemove = existingAssociations
                .Where(ea => !deviceTypeIds.Contains(ea.DeviceTypeId))
                .ToList();

            if (toRemove.Any())
            {
                _context.DeviceTypeFactors.RemoveRange(toRemove);
            }

            // 添加新关联的设备类型
            var existingDeviceTypeIds = existingAssociations.Select(ea => ea.DeviceTypeId).ToList();
            var toAdd = deviceTypeIds
                .Where(id => !existingDeviceTypeIds.Contains(id))
                .Select(deviceTypeId => new DeviceTypeFactor
                {
                    FactorId = factorId,
                    DeviceTypeId = deviceTypeId,
                    IsVisible = true,
                    IsRequired = false,
                    SortOrder = 0
                })
                .ToList();

            if (toAdd.Any())
            {
                await _context.DeviceTypeFactors.AddRangeAsync(toAdd);
            }

            await _context.SaveChangesAsync();
        }

        private async Task UpdateDeviceTypeAssociations(int factorId, List<int> deviceTypeIds)
        {
            // 清除现有关联
            var existingAssociations = await _context.DeviceTypeFactors
                .Where(dtf => dtf.FactorId == factorId)
                .ToListAsync();

            _context.DeviceTypeFactors.RemoveRange(existingAssociations);

            // 添加新关联
            var newAssociations = deviceTypeIds.Select(deviceTypeId => new DeviceTypeFactor
            {
                FactorId = factorId,
                DeviceTypeId = deviceTypeId,
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

        private string GetCategoryDescription(FactorCategory category)
        {
            return category switch
            {
                FactorCategory.General => "通用监测因子",
                FactorCategory.Environmental => "环境相关的监测因子，如温度、湿度等",
                FactorCategory.Electrical => "电气相关的监测因子，如电压、电流等",
                FactorCategory.Mechanical => "机械相关的监测因子，如转速、振动等",
                FactorCategory.Safety => "安全相关的监测因子，如烟雾、泄漏等",
                FactorCategory.Performance => "性能相关的监测因子",
                FactorCategory.Quality => "质量相关的监测因子",
                FactorCategory.Custom => "用户自定义的监测因子",
                _ => "未知分类"
            };
        }

        private string GetDataTypeName(IotDataType dataType)
        {
            return dataType switch
            {
                IotDataType.Integer => "整数",
                IotDataType.Decimal => "小数",
                IotDataType.Boolean => "布尔值",
                IotDataType.String => "字符串",
                IotDataType.DateTime => "日期时间",
                IotDataType.Enum => "枚举",
                _ => "未知"
            };
        }

        private string GetDataTypeDescription(IotDataType dataType)
        {
            return dataType switch
            {
                IotDataType.Integer => "整数值类型",
                IotDataType.Decimal => "小数值类型，可设置精度和小数位",
                IotDataType.Boolean => "布尔值类型，true/false",
                IotDataType.String => "字符串类型",
                IotDataType.DateTime => "日期时间类型",
                IotDataType.Enum => "枚举类型，需要定义枚举值",
                _ => "未知数据类型"
            };
        }
    }

    public class CategoryDto
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class SortOrderDto
    {
        public int Id { get; set; }
        public int SortOrder { get; set; }
    }
}