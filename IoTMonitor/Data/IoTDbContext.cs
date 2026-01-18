using IoTMonitor.Models;
using Microsoft.EntityFrameworkCore;

namespace IoTMonitor.Data
{
    public class IoTDbContext : DbContext
    {
        public IoTDbContext(DbContextOptions<IoTDbContext> options) : base(options) { }

        public DbSet<Device> Devices { get; set; }

        public DbSet<DeviceTable> DeviceTables { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 主键
            modelBuilder.Entity<Device>().HasKey(d => d.DeviceId);
            modelBuilder.Entity<DeviceTable>().HasKey(da => da.Id);


        }
    }
}
