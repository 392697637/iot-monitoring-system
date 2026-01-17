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
        public string? Location { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? DeviceTable { get; set; }


        public ICollection<DeviceData> DeviceDatas { get; set; } = new List<DeviceData>();
        public ICollection<DeviceThreshold> DeviceThresholds { get; set; } = new List<DeviceThreshold>();
        public ICollection<DeviceAlarm> DeviceAlarms { get; set; } = new List<DeviceAlarm>();

        
    }

    public class DeviceData
    {
        public long DataId { get; set; }
        public int DeviceId { get; set; }

        public double Temperature { get; set; }
        public double Humidity { get; set; }
        public double Current { get; set; }
        public double Voltage { get; set; }
        public string Status { get; set; } = "正常";
        public DateTime CreatedAt { get; set; }

        public Device Device { get; set; } = null!;
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
    /// 设备表结构配置表实体类
    /// 对应数据库表：DeviceTable
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
        [Required]
        [StringLength(100)]
        [Column("TableName")]
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// 表字段名（如：ph、power）
        /// </summary>
        [Required]
        [StringLength(100)]
        [Column("FieldName")]
        public string FieldName { get; set; } = string.Empty;

        /// <summary>
        /// 表字段类型（如：FLOAT、INT、VARCHAR）
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("FieldType")]
        public string FieldType { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称（如：pH值、当前功率）
        /// </summary>
        [StringLength(100)]
        [Column("DisplayName")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// 显示单位（如：°C、kWh）
        /// </summary>
        [StringLength(50)]
        [Column("DisplayUnit")]
        public string? DisplayUnit { get; set; }

        /// <summary>
        /// 排序序号
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
        [StringLength(500)]
        [Column("Remarks")]
        public string? Remarks { get; set; }
    }


}
