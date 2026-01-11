using IoTMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitor.Data
{
    public class IoTDbContext : DbContext
    {
        public IoTDbContext(DbContextOptions<IoTDbContext> options) : base(options) { }

        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceData> DeviceDatas { get; set; }
        public DbSet<DeviceThreshold> DeviceThresholds { get; set; }
        public DbSet<DeviceAlarm> DeviceAlarms { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 主键
            modelBuilder.Entity<Device>().HasKey(d => d.DeviceId);
            modelBuilder.Entity<DeviceData>().HasKey(dd => dd.DataId);
            modelBuilder.Entity<DeviceThreshold>().HasKey(dt => dt.ThresholdId);
            modelBuilder.Entity<DeviceAlarm>().HasKey(da => da.AlarmId);

            // 设备与数据
            modelBuilder.Entity<Device>()
                .HasMany(d => d.DeviceDatas)
                .WithOne(dd => dd.Device)
                .HasForeignKey(dd => dd.DeviceId);

            // 设备与阈值
            modelBuilder.Entity<Device>()
                .HasMany(d => d.DeviceThresholds)
                .WithOne(dt => dt.Device)
                .HasForeignKey(dt => dt.DeviceId);

            // 设备与报警
            modelBuilder.Entity<Device>()
                .HasMany(d => d.DeviceAlarms)
                .WithOne(da => da.Device)
                .HasForeignKey(da => da.DeviceId);
        }
    }
}
