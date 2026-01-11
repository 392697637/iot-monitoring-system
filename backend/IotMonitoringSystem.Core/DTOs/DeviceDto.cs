using IotMonitoringSystem.Core.Entities;
using System;
using System.ComponentModel.DataAnnotations;

namespace IotMonitoringSystem.Core.DTOs
{
    public class DeviceDto
    {
        public int Id { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public string DeviceName { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public DateOnly? InstallationDate { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        // 统计信息
        public int TotalDataCount { get; set; }
        public int TodayDataCount { get; set; }
        public int ActiveAlarmCount { get; set; }
    }
    
    public class CreateDeviceDto
    {
        [Required(ErrorMessage = "设备编码不能为空")]
        [StringLength(50, ErrorMessage = "设备编码长度不能超过50个字符")]
        public string DeviceCode { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "设备名称不能为空")]
        [StringLength(100, ErrorMessage = "设备名称长度不能超过100个字符")]
        public string DeviceName { get; set; } = string.Empty;
        
        [StringLength(200, ErrorMessage = "位置信息长度不能超过200个字符")]
        public string? Location { get; set; }
        
        [StringLength(100, ErrorMessage = "制造商长度不能超过100个字符")]
        public string? Manufacturer { get; set; }
        
        [StringLength(50, ErrorMessage = "型号长度不能超过50个字符")]
        public string? Model { get; set; }
        
        [StringLength(100, ErrorMessage = "序列号长度不能超过100个字符")]
        public string? SerialNumber { get; set; }
        
        public DateOnly? InstallationDate { get; set; }
        public bool IsActive { get; set; } = true;
    }
    
    public class UpdateDeviceDto
    {
        [StringLength(100, ErrorMessage = "设备名称长度不能超过100个字符")]
        public string? DeviceName { get; set; }
        
        [StringLength(200, ErrorMessage = "位置信息长度不能超过200个字符")]
        public string? Location { get; set; }
        
        public bool? IsActive { get; set; }
    }
    
    public class DeviceDataDto
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public decimal? Current { get; set; }
        public decimal? Voltage { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string? DeviceName { get; set; }
    }
    
    public class RealTimeDataDto
    {
        public DateTime Timestamp { get; set; }
        public decimal? Temperature { get; set; }
        public decimal? Humidity { get; set; }
        public decimal? Current { get; set; }
        public decimal? Voltage { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool HasAlarm { get; set; }
        public string? AlarmMessage { get; set; }
    }
    
    public class HistoricalDataQueryDto
    {
        [Required(ErrorMessage = "设备ID不能为空")]
        public int DeviceId { get; set; }
        
        [Required(ErrorMessage = "开始时间不能为空")]
        public DateTime StartTime { get; set; }
        
        [Required(ErrorMessage = "结束时间不能为空")]
        public DateTime EndTime { get; set; }
        
        [Range(1, 1000, ErrorMessage = "分页大小必须在1-1000之间")]
        public int PageSize { get; set; } = 50;
        
        [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
        public int PageNumber { get; set; } = 1;
        
        public string? SortBy { get; set; } = "Timestamp";
        public bool SortDescending { get; set; } = true;
    }
    
    public class ThresholdDto
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public FactorType FactorType { get; set; }
        public string FactorName { get; set; } = string.Empty;
        public decimal UpperLimit { get; set; }
        public decimal LowerLimit { get; set; }
        public bool IsRealTimeAlert { get; set; }
        public string? AlertMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? DeviceName { get; set; }
    }
    
    public class CreateThresholdDto
    {
        [Required(ErrorMessage = "设备ID不能为空")]
        public int DeviceId { get; set; }
        
        [Required(ErrorMessage = "监测因子类型不能为空")]
        [Range(1, 4, ErrorMessage = "监测因子类型必须在1-4之间")]
        public FactorType FactorType { get; set; }
        
        [Required(ErrorMessage = "上限值不能为空")]
        [Range(-1000, 1000, ErrorMessage = "上限值必须在-1000到1000之间")]
        public decimal UpperLimit { get; set; }
        
        [Required(ErrorMessage = "下限值不能为空")]
        [Range(-1000, 1000, ErrorMessage = "下限值必须在-1000到1000之间")]
        public decimal LowerLimit { get; set; }
        
        public bool IsRealTimeAlert { get; set; } = true;
        
        [StringLength(200, ErrorMessage = "报警消息长度不能超过200个字符")]
        public string? AlertMessage { get; set; }
    }
    
    public class AlarmDto
    {
        public long Id { get; set; }
        public int DeviceId { get; set; }
        public string? DeviceName { get; set; }
        public FactorType? FactorType { get; set; }
        public string? FactorName { get; set; }
        public decimal? Value { get; set; }
        public string? LimitType { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public TimeSpan Duration { get; set; }
    }
    
    public class DashboardSummaryDto
    {
        public int TotalDevices { get; set; }
        public int ActiveDevices { get; set; }
        public int TotalAlarmsToday { get; set; }
        public int UnacknowledgedAlarms { get; set; }
        public int TotalDataPointsToday { get; set; }
        public List<DeviceStatusSummaryDto> DeviceStatusSummary { get; set; } = new();
        public List<AlarmTrendDto> AlarmTrend { get; set; } = new();
    }
    
    public class DeviceStatusSummaryDto
    {
        public string DeviceName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal LastTemperature { get; set; }
        public decimal LastHumidity { get; set; }
        public bool HasAlarm { get; set; }
    }
    
    public class AlarmTrendDto
    {
        public DateTime Time { get; set; }
        public int AlarmCount { get; set; }
    }
}