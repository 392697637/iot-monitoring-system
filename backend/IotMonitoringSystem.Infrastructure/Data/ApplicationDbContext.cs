using Microsoft.EntityFrameworkCore;
using IotMonitoringSystem.Core.Entities;
using System.Reflection;

namespace IotMonitoringSystem.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // 已有表
        public DbSet<Device> Devices { get; set; }
        public DbSet<DeviceData> DeviceData { get; set; }
        public DbSet<Threshold> Thresholds { get; set; }
        public DbSet<Alarm> Alarms { get; set; }

        // DeviceFactor相关表
        public DbSet<DeviceFactor> DeviceFactors { get; set; }
        public DbSet<DeviceType> DeviceTypes { get; set; }
        public DbSet<DeviceTypeFactor> DeviceTypeFactors { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            try
            {
                base.OnModelCreating(modelBuilder);

                // 应用实体配置
                //modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

                // 设备表配置
                modelBuilder.Entity<Device>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.HasIndex(e => e.DeviceCode).IsUnique();
                    entity.Property(e => e.DeviceCode).IsRequired().HasMaxLength(50);
                    entity.Property(e => e.DeviceName).IsRequired().HasMaxLength(100);
                    entity.Property(e => e.Location).HasMaxLength(200);
                    entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                    entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

                    //// 添加设备类型关联
                    //entity.HasOne(d => d.DeviceType)
                    //      .WithMany(dt => dt.Devices)
                    //      .HasForeignKey(d => d.DeviceTypeId)
                    //      .OnDelete(DeleteBehavior.SetNull);

                    // 一对多关系
                    entity.HasMany(d => d.DeviceData)
                          .WithOne(dd => dd.Device)
                          .HasForeignKey(dd => dd.DeviceId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasMany(d => d.Thresholds)
                          .WithOne(t => t.Device)
                          .HasForeignKey(t => t.DeviceId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasMany(d => d.Alarms)
                          .WithOne(a => a.Device)
                          .HasForeignKey(a => a.DeviceId)
                          .OnDelete(DeleteBehavior.Cascade);
                });

                // 设备数据表配置
                modelBuilder.Entity<DeviceData>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Temperature).HasPrecision(5, 2);
                    entity.Property(e => e.Humidity).HasPrecision(5, 2);
                    entity.Property(e => e.Current).HasPrecision(5, 2);
                    entity.Property(e => e.Voltage).HasPrecision(5, 2);
                    entity.Property(e => e.Timestamp).HasDefaultValueSql("GETDATE()");

                    // 索引优化
                    entity.HasIndex(e => new { e.DeviceId, e.Timestamp })
                          .IsDescending(false, true)
                          .HasDatabaseName("IX_DeviceData_DeviceId_Timestamp");

                    entity.HasIndex(e => e.Timestamp)
                          .HasDatabaseName("IX_DeviceData_Timestamp");
                });

                // 阈值表配置
                modelBuilder.Entity<Threshold>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.UpperLimit).HasPrecision(5, 2);
                    entity.Property(e => e.LowerLimit).HasPrecision(5, 2);
                    entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
                    entity.Property(e => e.UpdatedAt).HasDefaultValueSql("GETDATE()");

                    // 唯一约束
                    entity.HasIndex(e => new { e.DeviceId, e.FactorType })
                          .IsUnique()
                          .HasDatabaseName("UQ_Device_Factor");

                    //// 关联DeviceFactor
                    //entity.HasOne(t => t.DeviceFactor)
                    //      .WithMany(f => f.Thresholds)
                    //      .HasForeignKey(t => t.FactorType)
                    //      .HasPrincipalKey(f => (int?)f.FactorType)
                    //      .OnDelete(DeleteBehavior.SetNull);
                });

                // 报警表配置
                modelBuilder.Entity<Alarm>(entity =>
                {
                    entity.HasKey(e => e.Id);
                    entity.Property(e => e.Value).HasPrecision(5, 2);
                    entity.Property(e => e.LimitType).HasMaxLength(10);
                    entity.Property(e => e.Message).HasMaxLength(500);
                    entity.Property(e => e.Timestamp).HasDefaultValueSql("GETDATE()");

                    // 索引优化
                    entity.HasIndex(e => new { e.DeviceId, e.Timestamp })
                          .IsDescending(false, true)
                          .HasDatabaseName("IX_Alarms_DeviceId_Timestamp");

                    entity.HasIndex(e => e.IsAcknowledged)
                          .HasDatabaseName("IX_Alarms_IsAcknowledged");

                    // 关联DeviceFactor
                    entity.HasOne(a => a.DeviceFactor)
                          .WithMany(f => f.Alarms)
                          .HasForeignKey(a => a.FactorType)
                          .OnDelete(DeleteBehavior.SetNull);
                });

                // 设备因子表配置
                modelBuilder.Entity<DeviceFactor>(entity =>
                {
                    entity.HasKey(e => e.Id);

                    entity.HasIndex(e => e.FactorCode).IsUnique();
                    entity.Property(e => e.FactorCode)
                        .IsRequired()
                        .HasMaxLength(50);

                    entity.Property(e => e.FactorName)
                        .IsRequired()
                        .HasMaxLength(100);

                    entity.Property(e => e.DisplayName)
                        .HasMaxLength(50);

                    entity.Property(e => e.Category)
                        .IsRequired()
                        .HasConversion<int>();

                    entity.Property(e => e.Unit)
                        .HasMaxLength(20);

                    entity.Property(e => e.DefaultUpperLimit)
                        .HasPrecision(5, 2);

                    entity.Property(e => e.DefaultLowerLimit)
                        .HasPrecision(5, 2);

                    entity.Property(e => e.Description)
                        .HasMaxLength(200);

                    entity.Property(e => e.DataType)
                        .IsRequired()
                        .HasConversion<int>();

                    entity.Property(e => e.Precision)
                        .HasPrecision(5, 2);

                    entity.Property(e => e.Scale)
                        .HasPrecision(5, 2);

                    entity.Property(e => e.Icon)
                        .HasMaxLength(100);

                    entity.Property(e => e.Color)
                        .HasMaxLength(50);

                    entity.Property(e => e.CreatedAt)
                        .HasDefaultValueSql("GETDATE()");

                    entity.Property(e => e.UpdatedAt)
                        .HasDefaultValueSql("GETDATE()");

                    // 索引
                    entity.HasIndex(e => e.Category)
                        .HasDatabaseName("IX_DeviceFactors_Category");

                    entity.HasIndex(e => e.IsActive)
                        .HasDatabaseName("IX_DeviceFactors_IsActive");

                    entity.HasIndex(e => e.SortOrder)
                        .HasDatabaseName("IX_DeviceFactors_SortOrder");

                    entity.HasIndex(e => new { e.Category, e.IsActive })
                        .HasDatabaseName("IX_DeviceFactors_Category_Active");
                });

                // 设备类型表配置
                modelBuilder.Entity<DeviceType>(entity =>
                {
                    entity.HasKey(e => e.Id);

                    entity.HasIndex(e => e.TypeCode).IsUnique();
                    entity.Property(e => e.TypeCode)
                        .IsRequired()
                        .HasMaxLength(50);

                    entity.Property(e => e.TypeName)
                        .IsRequired()
                        .HasMaxLength(100);

                    entity.Property(e => e.Description)
                        .HasMaxLength(200);

                    entity.Property(e => e.Icon)
                        .HasMaxLength(100);

                    entity.Property(e => e.CreatedAt)
                        .HasDefaultValueSql("GETDATE()");

                    entity.Property(e => e.UpdatedAt)
                        .HasDefaultValueSql("GETDATE()");

                    // 索引
                    entity.HasIndex(e => e.IsActive)
                        .HasDatabaseName("IX_DeviceTypes_IsActive");
                });

                // 设备类型-因子关联表配置
                modelBuilder.Entity<DeviceTypeFactor>(entity =>
                {
                    // 复合主键
                    entity.HasKey(e => new { e.DeviceTypeId, e.FactorId });

                    // 配置外键关系
                    entity.HasOne(dtf => dtf.DeviceType)
                          .WithMany(dt => dt.DeviceTypeFactors)
                          .HasForeignKey(dtf => dtf.DeviceTypeId)
                          .OnDelete(DeleteBehavior.Cascade);

                    entity.HasOne(dtf => dtf.DeviceFactor)
                          .WithMany(df => df.DeviceTypeFactors)
                          .HasForeignKey(dtf => dtf.FactorId)
                          .OnDelete(DeleteBehavior.Cascade);

                    // 属性配置
                    entity.Property(e => e.SortOrder)
                        .HasDefaultValue(0);

                    entity.Property(e => e.IsVisible)
                        .HasDefaultValue(true);

                    entity.Property(e => e.IsRequired)
                        .HasDefaultValue(false);

                    // 索引
                    entity.HasIndex(e => e.FactorId)
                        .HasDatabaseName("IX_DeviceTypeFactors_FactorId");

                    entity.HasIndex(e => new { e.DeviceTypeId, e.SortOrder })
                        .HasDatabaseName("IX_DeviceTypeFactors_TypeId_SortOrder");

                    entity.HasIndex(e => new { e.DeviceTypeId, e.IsVisible })
                        .HasDatabaseName("IX_DeviceTypeFactors_TypeId_Visible");
                });

            }
            catch (Exception ex)
            {
                
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is BaseEntity &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entry in entries)
            {
                var entity = (BaseEntity)entry.Entity;
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedAt = DateTime.UtcNow;
                }
                entity.UpdatedAt = DateTime.UtcNow;
            }

            return await base.SaveChangesAsync(cancellationToken);
        }

        // 添加一些便捷方法
        public async Task<List<DeviceFactor>> GetFactorsByDeviceTypeAsync(int deviceTypeId)
        {
            return await DeviceTypeFactors
                .Where(dtf => dtf.DeviceTypeId == deviceTypeId && dtf.IsVisible)
                .Include(dtf => dtf.DeviceFactor)
                .OrderBy(dtf => dtf.SortOrder)
                .Select(dtf => dtf.DeviceFactor!)
                .ToListAsync();
        }

        public async Task<List<DeviceType>> GetDeviceTypesByFactorAsync(int factorId)
        {
            return await DeviceTypeFactors
                .Where(dtf => dtf.FactorId == factorId)
                .Include(dtf => dtf.DeviceType)
                .OrderBy(dtf => dtf.DeviceType!.TypeName)
                .Select(dtf => dtf.DeviceType!)
                .ToListAsync();
        }

        public async Task<List<DeviceFactor>> GetActiveFactorsAsync()
        {
            return await DeviceFactors
                .Where(f => f.IsActive)
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.FactorName)
                .ToListAsync();
        }

        public async Task<List<DeviceType>> GetActiveDeviceTypesAsync()
        {
            return await DeviceTypes
                .Where(dt => dt.IsActive)
                .OrderBy(dt => dt.TypeName)
                .ToListAsync();
        }

        public async Task<bool> FactorExistsAsync(string factorCode)
        {
            return await DeviceFactors
                .AnyAsync(f => f.FactorCode == factorCode);
        }

        public async Task<bool> DeviceTypeExistsAsync(string typeCode)
        {
            return await DeviceTypes
                .AnyAsync(dt => dt.TypeCode == typeCode);
        }

        public async Task<DeviceFactor?> GetFactorByCodeAsync(string factorCode)
        {
            return await DeviceFactors
                .FirstOrDefaultAsync(f => f.FactorCode == factorCode);
        }

        public async Task<DeviceType?> GetDeviceTypeByCodeAsync(string typeCode)
        {
            return await DeviceTypes
                .FirstOrDefaultAsync(dt => dt.TypeCode == typeCode);
        }
    }
}