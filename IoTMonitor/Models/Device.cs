using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IoTMonitor.Models
{
    public class Device
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string? DeviceType { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? DeviceTable { get; set; }
        public string? DeviceTableID { get; set; }
        
    }

    public class DeviceThreshold
    {
        public int ThresholdId { get; set; }
        public int DeviceId { get; set; }

        public double? TemperatureUpper { get; set; }
        public double? TemperatureLower { get; set; }
        public double? HumidityUpper { get; set; }
        public double? HumidityLower { get; set; }
        public double? CurrentUpper { get; set; }
        public double? CurrentLower { get; set; }
        public double? VoltageUpper { get; set; }
        public double? VoltageLower { get; set; }

        public DateTime CreatedAt { get; set; }

        public Device Device { get; set; } = null!;
    }

    public class DeviceAlarm
    {
        public long AlarmId { get; set; } // ✅ 主键
        public int DeviceId { get; set; }
        public string AlarmType { get; set; } = string.Empty;
        public double AlarmValue { get; set; }
        public DateTime CreatedAt { get; set; }

        public Device Device { get; set; } = null!;
    }


    /// <summary>
    /// 设备表配置实体
    /// 用于管理设备数据表的字段配置信息
    /// </summary>
    [Table("DeviceTable")]
    public class DeviceTable
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("ID")]
        public long Id { get; set; }

        /// <summary>
        /// 表名（如：water_sensor、smart_plug）
        /// </summary>
        [Required(ErrorMessage = "表名不能为空")]
        [StringLength(100, ErrorMessage = "表名长度不能超过100个字符")]
        [Column("TableName")]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 表字段名（如：ph、power、temperature）
        /// </summary>
        [Required(ErrorMessage = "字段名不能为空")]
        [StringLength(100, ErrorMessage = "字段名长度不能超过100个字符")]
        [Column("FieldName")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 表字段类型（如：FLOAT、INT、VARCHAR、DATETIME、BIT）
        /// </summary>
        [Required(ErrorMessage = "字段类型不能为空")]
        [StringLength(50, ErrorMessage = "字段类型长度不能超过50个字符")]
        [Column("FieldType")]
        public string FieldType { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称（如：pH值、当前功率、温度）
        /// </summary>
        [StringLength(100, ErrorMessage = "显示名称长度不能超过100个字符")]
        [Column("DisplayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// 显示单位（如：°C、kWh、mg/L）
        /// </summary>
        [StringLength(50, ErrorMessage = "显示单位长度不能超过50个字符")]
        [Column("DisplayUnit")]
        public string? DisplayUnit { get; set; }

        /// <summary>
        /// 排序序号（用于界面显示顺序）
        /// </summary>
        [Column("SortOrder")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 是否显示（true=显示，false=不显示）
        /// </summary>
        [Column("IsVisible")]
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [Column("CreatedTime")]
        public DateTime CreatedTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 备注信息
        /// </summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [Column("Remarks")]
        public string? Remarks { get; set; }

        /// <summary>
        /// 最小阈值
        /// 存储为字符串以适应不同类型的阈值（数值、文本匹配等）
        /// </summary>
        [StringLength(100, ErrorMessage = "最小阈值长度不能超过100个字符")]
        [Column("minValue")]
        public string? MinValue { get; set; }

        /// <summary>
        /// 最大阈值
        /// 存储为字符串以适应不同类型的阈值（数值、文本匹配等）
        /// </summary>
        [StringLength(100, ErrorMessage = "最大阈值长度不能超过100个字符")]
        [Column("maxValue")]
        public string? MaxValue { get; set; }

        /// <summary>
        /// 是否阈值字段（true=需要阈值监测，false=不需要）
        /// </summary>
        [Column("IsThreshold")]
        public bool IsThreshold { get; set; } = false;

        /// <summary>
        /// 数据单位（实际存储的单位，与DisplayUnit可能不同）
        /// </summary>
        [StringLength(50, ErrorMessage = "数据单位长度不能超过50个字符")]
        [Column("DataUnit")]
        public string? DataUnit { get; set; }

    }
}
