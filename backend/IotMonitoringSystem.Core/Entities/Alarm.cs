using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace IotMonitoringSystem.Core.Entities
{
    [Table("Alarms")]
    public class Alarm
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        
        [Required]
        public int DeviceId { get; set; }
        
        public int? ThresholdId { get; set; }
        
        public FactorType? FactorType { get; set; }
        
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Value { get; set; }
        
        [StringLength(10)]
        public string? LimitType { get; set; }  // Upper/Lower
        
        [StringLength(500)]
        public string? Message { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public bool IsAcknowledged { get; set; } = false;
        
        public DateTime? AcknowledgedAt { get; set; }
        
        // 导航属性
        [JsonIgnore]
        public virtual Device? Device { get; set; }
        
        [JsonIgnore]
        public virtual Threshold? Threshold { get; set; }

        [JsonIgnore]
        public virtual DeviceFactor? DeviceFactor { get; set; }
    }
}