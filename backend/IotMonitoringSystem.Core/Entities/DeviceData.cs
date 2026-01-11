using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IotMonitoringSystem.Core.Entities
{
    [Table("DeviceData")]
    public class DeviceData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        
        [Required]
        public int DeviceId { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Temperature { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Humidity { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Current { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Voltage { get; set; }
        
        public DeviceStatus Status { get; set; } = DeviceStatus.Normal;
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        // 导航属性
        [JsonIgnore]
        public virtual Device? Device { get; set; }
    }
    
    public enum DeviceStatus
    {
        Normal = 0,      // 正常
        Warning = 1,     // 警告
        Fault = 2,       // 故障
        Offline = 3      // 离线
    }
}