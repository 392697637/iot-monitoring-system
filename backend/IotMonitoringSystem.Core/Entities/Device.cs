using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace IotMonitoringSystem.Core.Entities
{
    public class Device : BaseEntity
    {
        [Required]
        [StringLength(50)]
        public string DeviceCode { get; set; } = string.Empty;
        
        [Required]
        [StringLength(100)]
        public string DeviceName { get; set; } = string.Empty;
        
        [StringLength(200)]
        public string? Location { get; set; }
        
        [StringLength(100)]
        public string? Manufacturer { get; set; }
        
        [StringLength(50)]
        public string? Model { get; set; }
        
        [StringLength(100)]
        public string? SerialNumber { get; set; }
        
        public DateOnly? InstallationDate { get; set; }
        
        public bool IsActive { get; set; } = true;


        // 导航属性
        public virtual ICollection<DeviceData> DeviceData { get; set; } = new List<DeviceData>();
        public virtual ICollection<Threshold> Thresholds { get; set; } = new List<Threshold>();
        public virtual ICollection<Alarm> Alarms { get; set; } = new List<Alarm>();
    }
    
    public class BaseEntity
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}