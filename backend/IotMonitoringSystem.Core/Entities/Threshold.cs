using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IotMonitoringSystem.Core.Entities
{
    [Table("Thresholds")]
    public class Threshold : BaseEntity
    {
        [Required]
        public int DeviceId { get; set; }
        
        [Required]
        [Range(1, 4)]
        public FactorType FactorType { get; set; }
        
        [StringLength(50)]
        public string? FactorName { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal UpperLimit { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal LowerLimit { get; set; }
        
        public bool IsRealTimeAlert { get; set; } = true;
        
        [StringLength(200)]
        public string? AlertMessage { get; set; }
        
        // 导航属性
        [JsonIgnore]
        public virtual Device? Device { get; set; }
        
        [JsonIgnore]
        public virtual ICollection<Alarm> Alarms { get; set; } = new List<Alarm>();
 

    }
    
    public enum FactorType
    {
        Temperature = 1,  // 温度
        Humidity = 2,     // 湿度
        Current = 3,      // 电流
        Voltage = 4       // 电压
    }
}