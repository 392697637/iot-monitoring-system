using System;
using System.Collections.Generic;

namespace IoTMonitor.Models
{
    public class Device
    {
        public int DeviceId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }

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
}
