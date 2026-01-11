using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IotMonitoringSystem.Core.Entities
{
    [Table("DeviceFactor")]
    public class DeviceFactor : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string FactorCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FactorName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? DisplayName { get; set; }

        [Required]
        public FactorCategory Category { get; set; } = FactorCategory.General;

        [StringLength(20)]
        public string? Unit { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DefaultUpperLimit { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? DefaultLowerLimit { get; set; }

        [StringLength(200)]
        public string? Description { get; set; }

        [Required]
        public IotDataType DataType { get; set; } = IotDataType.Decimal;

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Precision { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal? Scale { get; set; }

        public bool IsRequired { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; } = 0;

        [StringLength(100)]
        public string? Icon { get; set; }

        [StringLength(50)]
        public string? Color { get; set; }

        // 设备类型关联（多对多）
        [JsonIgnore]
        public virtual ICollection<DeviceTypeFactor> DeviceTypeFactors { get; set; } = new List<DeviceTypeFactor>();

        // 阈值配置
        [JsonIgnore]
        public virtual ICollection<Threshold> Thresholds { get; set; } = new List<Threshold>();

        // 报警记录
        [JsonIgnore]
        public virtual ICollection<Alarm> Alarms { get; set; } = new List<Alarm>();
    }

    public enum FactorCategory
    {
        General = 0,        // 通用
        Environmental = 1,  // 环境
        Electrical = 2,     // 电气
        Mechanical = 3,     // 机械
        Safety = 4,         // 安全
        Performance = 5,    // 性能
        Quality = 6,        // 质量
        Custom = 99         // 自定义
    }

    public enum IotDataType
    {
        Integer = 0,        // 整数
        Decimal = 1,        // 小数
        Boolean = 2,        // 布尔值
        String = 3,         // 字符串
        DateTime = 4,       // 日期时间
        Enum = 5            // 枚举
    }

    public class DeviceTypeFactor
    {
        public int DeviceTypeId { get; set; }
        public int FactorId { get; set; }

        public int SortOrder { get; set; }
        public bool IsVisible { get; set; } = true;
        public bool IsRequired { get; set; } = false;

        public virtual DeviceType? DeviceType { get; set; }
        public virtual DeviceFactor? DeviceFactor { get; set; }
    }

    public class DeviceType : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string TypeCode { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string TypeName { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        [StringLength(100)]
        public string? Icon { get; set; }

        public bool IsActive { get; set; } = true;

        // 多对多关系
        [JsonIgnore]
        public virtual ICollection<DeviceTypeFactor> DeviceTypeFactors { get; set; } = new List<DeviceTypeFactor>();

        // 设备关联
        [JsonIgnore]
        public virtual ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}