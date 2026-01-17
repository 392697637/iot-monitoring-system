using Dapper;
using IoTMonitor.Data;
using IoTMonitor.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace IoTMonitor.Services
{

    public class DeviceDataService  
    {
        
        private readonly IoTDbContext _context;

        public DeviceDataService(IoTDbContext context)
        {
            _context = context;
        }
        // 添加设备数据，并检查阈值
        public async Task<DeviceData> AddDeviceDataAsync(DeviceData data)
        {
            data.Status = "正常";

            var thresholds = await _context.DeviceThresholds
                .FirstOrDefaultAsync(t => t.DeviceId == data.DeviceId);

            if (thresholds != null)
            {
                if ((thresholds.TemperatureUpper.HasValue && data.Temperature > thresholds.TemperatureUpper.Value) ||
                    (thresholds.TemperatureLower.HasValue && data.Temperature < thresholds.TemperatureLower.Value))
                {
                    data.Status = "温度异常";
                    await AddAlarmAsync(data.DeviceId, "Temperature", data.Temperature);
                }

                if ((thresholds.HumidityUpper.HasValue && data.Humidity > thresholds.HumidityUpper.Value) ||
                    (thresholds.HumidityLower.HasValue && data.Humidity < thresholds.HumidityLower.Value))
                {
                    data.Status = "湿度异常";
                    await AddAlarmAsync(data.DeviceId, "Humidity", data.Humidity);
                }

                if ((thresholds.CurrentUpper.HasValue && data.Current > thresholds.CurrentUpper.Value) ||
                    (thresholds.CurrentLower.HasValue && data.Current < thresholds.CurrentLower.Value))
                {
                    data.Status = "电流异常";
                    await AddAlarmAsync(data.DeviceId, "Current", data.Current);
                }

                if ((thresholds.VoltageUpper.HasValue && data.Voltage > thresholds.VoltageUpper.Value) ||
                    (thresholds.VoltageLower.HasValue && data.Voltage < thresholds.VoltageLower.Value))
                {
                    data.Status = "电压异常";
                    await AddAlarmAsync(data.DeviceId, "Voltage", data.Voltage);
                }
            }

            _context.DeviceDatas.Add(data);
            await _context.SaveChangesAsync();
            return data;
        }

        private async Task AddAlarmAsync(int deviceId, string factor, double value)
        {
            var alarm = new DeviceAlarm
            {
                DeviceId = deviceId,
                AlarmType = factor,
                AlarmValue = value,
                CreatedAt = DateTime.Now
            };
            _context.DeviceAlarms.Add(alarm);
            await _context.SaveChangesAsync();
        }

        public async Task<List<DeviceData>> GetDeviceHistoryAsync(int deviceId, DateTime start, DateTime end)
        {

            var list = GetDeviceHistoryList(deviceId, start, end);
            return await list;
            //return await _context.DeviceDatas
            //    .Where(d => d.DeviceId == deviceId && d.CreatedAt >= start && d.CreatedAt <= end)
            //    .OrderByDescending(d => d.CreatedAt)
            //    .ToListAsync();
        }
        public async Task<List<DeviceData>> GetDeviceHistoryList(int deviceId, DateTime start, DateTime end)
        {
            return await _context.DeviceDatas
                .Where(d => d.DeviceId == deviceId && d.CreatedAt >= start && d.CreatedAt <= end)
                .OrderByDescending(d => d.CreatedAt).ToListAsync();
            ////start = DateTime.Parse("2026-01-01 00:00");
            ////end = DateTime.Parse("2026-02-01 00:00");
            //return await _context.DeviceDatas
            //   .Where(d => d.DeviceId == deviceId && d.CreatedAt >= start && d.CreatedAt <= end).ToListAsync();
            ////return await _context.DeviceDatas.Where(d => d.DeviceId == deviceId).ToListAsync();
            ////return await _context.DeviceDatas.ToListAsync();
        }
        public async Task<DeviceData?> GetLatestDeviceDataAsync(int deviceId)
        {
            return await _context.DeviceDatas
                .Where(d => d.DeviceId == deviceId)
                .OrderByDescending(d => d.CreatedAt)
                .FirstOrDefaultAsync();
        }
     
    }
    

    public class DeviceService
    {
        private readonly IoTDbContext _context;

        public DeviceService(IoTDbContext context)
        {
            _context = context;
        }

        public async Task<List<Device>> GetAllDevicesAsync()
        {
            return await _context.Devices
                .Include(d => d.DeviceDatas)
                .Include(d => d.DeviceThresholds)
                .Include(d => d.DeviceAlarms)
                .ToListAsync();

        }

        public async Task<List<Device>> GetAllDevicesAsyncs()
        {
            return await _context.Devices
                .Include(d => d.DeviceDatas)
                .Include(d => d.DeviceThresholds)
                .Include(d => d.DeviceAlarms)
                .ToListAsync();
        }
        public async Task<Device> AddDeviceAsync(Device device)
        {
            device.CreatedAt = DateTime.Now;
            _context.Devices.Add(device);
            await _context.SaveChangesAsync();
            return device;
        }
    }

    public class ThresholdService
    {
        private readonly IoTDbContext _context;

        public ThresholdService(IoTDbContext context)
        {
            _context = context;
        }

        public async Task<DeviceThreshold?> GetThresholdsByDeviceAsync(int deviceId)
        {
            return await _context.DeviceThresholds
                .FirstOrDefaultAsync(t => t.DeviceId == deviceId);
        }

        public async Task<DeviceThreshold> AddOrUpdateThresholdAsync(DeviceThreshold threshold)
        {
            var existing = await _context.DeviceThresholds
                .FirstOrDefaultAsync(t => t.DeviceId == threshold.DeviceId);

            if (existing != null)
            {
                existing.TemperatureUpper = threshold.TemperatureUpper;
                existing.TemperatureLower = threshold.TemperatureLower;
                existing.HumidityUpper = threshold.HumidityUpper;
                existing.HumidityLower = threshold.HumidityLower;
                existing.CurrentUpper = threshold.CurrentUpper;
                existing.CurrentLower = threshold.CurrentLower;
                existing.VoltageUpper = threshold.VoltageUpper;
                existing.VoltageLower = threshold.VoltageLower;
            }
            else
            {
                threshold.CreatedAt = DateTime.Now;
                _context.DeviceThresholds.Add(threshold);
            }

            await _context.SaveChangesAsync();
            return threshold;
        }
    }


    public class DeviceTableService
    {
        private readonly IConfiguration _configuration;
        private readonly IoTDbContext _context;

        public DeviceTableService(IoTDbContext context)
        {
            _context = context;
            
            _configuration= _context.GetService<IConfiguration>();
        }
     

        /// <summary>
        /// 获取数据库中所有表名
        /// </summary>
        public async Task<IEnumerable<string>> GetAllTableNamesAsync()
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            var sql = @"SELECT TABLE_NAME 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_TYPE = 'BASE TABLE' 
                        AND TABLE_CATALOG = @DatabaseName";

            var databaseName = _configuration.GetConnectionString("DefaultConnection")
                .Split(';')
                .FirstOrDefault(x => x.StartsWith("Initial Catalog=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=')[1];

            return await connection.QueryAsync<string>(sql, new { DatabaseName = databaseName });
        }

        /// <summary>
        /// 验证表名是否合法（防止SQL注入）
        /// </summary>
        private bool IsValidTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                return false;

            // 只允许字母、数字、下划线
            return System.Text.RegularExpressions.Regex.IsMatch(tableName, @"^[a-zA-Z0-9_]+$");
        }

        /// <summary>
        /// 检查表是否存在
        /// </summary>
        private async Task<bool> TableExistsAsync(string tableName)
        {
            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            var sql = @"SELECT COUNT(*) 
                        FROM INFORMATION_SCHEMA.TABLES 
                        WHERE TABLE_NAME = @TableName 
                        AND TABLE_TYPE = 'BASE TABLE'";

            var count = await connection.ExecuteScalarAsync<int>(sql, new { TableName = tableName });
            return count > 0;
        }

        /// <summary>
        /// 分页查询表数据
        /// </summary>
        public async Task<(IEnumerable<dynamic> Data, int TotalCount)> GetDataByTableNamePagedAsync(
            string tableName, int pageNumber = 1, int pageSize = 20)
        {
            if (!IsValidTableName(tableName))
                throw new ArgumentException("无效的表名");

            if (!await TableExistsAsync(tableName))
                throw new ArgumentException($"表 '{tableName}' 不存在");

            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            // 获取总记录数
            var countSql = $"SELECT COUNT(*) FROM [{tableName}]";
            var totalCount = await connection.ExecuteScalarAsync<int>(countSql);

            // 分页查询数据
            var dataSql = $@"
                SELECT * FROM [{tableName}]
                ORDER BY (SELECT NULL)
                OFFSET @Offset ROWS
                FETCH NEXT @PageSize ROWS ONLY";

            var offset = (pageNumber - 1) * pageSize;
            var data = await connection.QueryAsync(dataSql, new
            {
                Offset = offset,
                PageSize = pageSize
            });

            return (data, totalCount);
        }
        /// <summary>
        /// 获取表名
        /// </summary>
        /// <param name="tablename"></param>
        /// <returns></returns>
        public async Task<List<DeviceTable>> GetablenameByDataAsync(string tablename)
        { 
            
            return await _context.DeviceTables.Where(d => d.TableName == tablename).ToListAsync(); 
        }
        /// <summary>
        /// 根据表名动态查询数据
        /// </summary>
        public async Task<IEnumerable<dynamic>> GetDataByTableNameAsync(string tableName,int topNumber, string orderby)
        {
            // 验证表名合法性，防止SQL注入
            if (!IsValidTableName(tableName))
            {
                throw new ArgumentException("无效的表名");
            }

            // 检查表是否存在
            if (!await TableExistsAsync(tableName))
            {
                throw new ArgumentException($"表 '{tableName}' 不存在");
            }

            using var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();

            // 动态查询表数据
            var sql = $"SELECT TOP({topNumber})* FROM [{tableName}]   ORDER BY {orderby} DESC ";
            return await connection.QueryAsync(sql);
        }
    }
}
