using IotMonitoringSystem.Core.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IotMonitoringSystem.Core.DTOs
{
    public class DeviceFactorDto
    {
        public int Id { get; set; }
        public string FactorCode { get; set; } = string.Empty;
        public string FactorName { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public FactorCategory Category { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public decimal? DefaultUpperLimit { get; set; }
        public decimal? DefaultLowerLimit { get; set; }
        public string? Description { get; set; }
        public IotDataType DataType { get; set; }
        public decimal? Precision { get; set; }
        public decimal? Scale { get; set; }
        public bool IsRequired { get; set; }
        public bool IsActive { get; set; }
        public int SortOrder { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 关联信息
        public int ThresholdCount { get; set; }
        public int DeviceTypeCount { get; set; }
        public List<string>? DeviceTypeNames { get; set; }
    }

    public class CreateDeviceFactorDto
    {
        [Required(ErrorMessage = "因子编码不能为空")]
        [StringLength(50, ErrorMessage = "因子编码长度不能超过50个字符")]
        [RegularExpression(@"^[A-Z][A-Z0-9_]*$", ErrorMessage = "因子编码必须以大写字母开头，只能包含大写字母、数字和下划线")]
        public string FactorCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "因子名称不能为空")]
        [StringLength(100, ErrorMessage = "因子名称长度不能超过100个字符")]
        public string FactorName { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "显示名称长度不能超过50个字符")]
        public string? DisplayName { get; set; }

        [Required(ErrorMessage = "分类不能为空")]
        public FactorCategory Category { get; set; }

        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        public string? Unit { get; set; }

        [Range(-1000, 1000, ErrorMessage = "默认上限值必须在-1000到1000之间")]
        public decimal? DefaultUpperLimit { get; set; }

        [Range(-1000, 1000, ErrorMessage = "默认下限值必须在-1000到1000之间")]
        public decimal? DefaultLowerLimit { get; set; }

        [StringLength(200, ErrorMessage = "描述长度不能超过200个字符")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "数据类型不能为空")]
        public IotDataType DataType { get; set; } = IotDataType.Decimal;

        [Range(0.01, 10, ErrorMessage = "精度必须在0.01到10之间")]
        public decimal? Precision { get; set; }

        [Range(0, 10, ErrorMessage = "小数位数必须在0到10之间")]
        public decimal? Scale { get; set; }

        public bool IsRequired { get; set; } = true;
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; } = 0;

        [StringLength(100, ErrorMessage = "图标长度不能超过100个字符")]
        public string? Icon { get; set; }

        [StringLength(50, ErrorMessage = "颜色长度不能超过50个字符")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "颜色格式不正确")]
        public string? Color { get; set; }

        // 关联的设备类型
        public List<int>? DeviceTypeIds { get; set; }
    }

    public class UpdateDeviceFactorDto
    {
        [StringLength(100, ErrorMessage = "因子名称长度不能超过100个字符")]
        public string? FactorName { get; set; }

        [StringLength(50, ErrorMessage = "显示名称长度不能超过50个字符")]
        public string? DisplayName { get; set; }

        public FactorCategory? Category { get; set; }

        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        public string? Unit { get; set; }

        [Range(-1000, 1000, ErrorMessage = "默认上限值必须在-1000到1000之间")]
        public decimal? DefaultUpperLimit { get; set; }

        [Range(-1000, 1000, ErrorMessage = "默认下限值必须在-1000到1000之间")]
        public decimal? DefaultLowerLimit { get; set; }

        [StringLength(200, ErrorMessage = "描述长度不能超过200个字符")]
        public string? Description { get; set; }

        public IotDataType? DataType { get; set; }

        [Range(0.01, 10, ErrorMessage = "精度必须在0.01到10之间")]
        public decimal? Precision { get; set; }

        [Range(0, 10, ErrorMessage = "小数位数必须在0到10之间")]
        public decimal? Scale { get; set; }

        public bool? IsRequired { get; set; }
        public bool? IsActive { get; set; }
        public int? SortOrder { get; set; }

        [StringLength(100, ErrorMessage = "图标长度不能超过100个字符")]
        public string? Icon { get; set; }

        [StringLength(50, ErrorMessage = "颜色长度不能超过50个字符")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$", ErrorMessage = "颜色格式不正确")]
        public string? Color { get; set; }

        // 关联的设备类型
        public List<int>? DeviceTypeIds { get; set; }
    }

    public class DeviceTypeDto
    {
        public int Id { get; set; }
        public string TypeCode { get; set; } = string.Empty;
        public string TypeName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // 关联信息
        public int DeviceCount { get; set; }
        public int FactorCount { get; set; }
        public List<DeviceFactorSimpleDto>? Factors { get; set; }
    }

    public class DeviceFactorSimpleDto
    {
        public int Id { get; set; }
        public string FactorCode { get; set; } = string.Empty;
        public string FactorName { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public bool IsVisible { get; set; }
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }
    }

    public class DeviceFactorMappingDto
    {
        public int DeviceTypeId { get; set; }
        public string DeviceTypeName { get; set; } = string.Empty;
        public List<DeviceFactorMappingItemDto> Factors { get; set; } = new();
    }

    public class DeviceFactorMappingItemDto
    {
        public int FactorId { get; set; }
        public string FactorName { get; set; } = string.Empty;
        public string? Unit { get; set; }
        public bool IsVisible { get; set; }
        public bool IsRequired { get; set; }
        public int SortOrder { get; set; }
    }

    public class DeviceFactorStatisticsDto
    {
        public int TotalFactors { get; set; }
        public int ActiveFactors { get; set; }
        public Dictionary<string, int> CategoryDistribution { get; set; } = new();
        public Dictionary<string, int> DataTypeDistribution { get; set; } = new();
        public List<FactorUsageDto> MostUsedFactors { get; set; } = new();
    }

    public class FactorUsageDto
    {
        public string FactorName { get; set; } = string.Empty;
        public int DeviceCount { get; set; }
        public int ThresholdCount { get; set; }
        public int DataRecordCount { get; set; }
    }
}