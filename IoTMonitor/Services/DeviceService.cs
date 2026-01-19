using Dapper;
using IoTMonitor.Data;
using IoTMonitor.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;

namespace IoTMonitor.Services
{

    /// <summary>
    /// 设备服务
    /// </summary>
    public class DeviceService
    {
        private readonly IoTDbContext _context;

        public DeviceService(IoTDbContext context)
        {
            _context = context;
        }
        /// <summary>
        /// 获取所有设备
        /// </summary>
        /// <returns></returns>
        public async Task<List<Device>> GetAllDevicesAsync()
        {
            try
            {
                return await _context.Devices.ToListAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("获取设备列表失败， ", ex);
            }

        }
        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<Device> AddDeviceAsync(Device device)
        {
            try
            {
                device.CreatedAt = DateTime.Now;
                _context.Devices.Add(device);
                await _context.SaveChangesAsync();
                return device;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("添加设备失败， ", ex);

            }
        }
        /// <summary>
        /// 更新设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<Device> UpdateDeviceAsync(Device device)
        {
            try
            {
                device.CreatedAt = DateTime.Now;
                _context.Devices.Update(device);
                await _context.SaveChangesAsync();
                return device;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("更新设备失败， ", ex);
            }

        }
        /// <summary>
        /// 删除设备
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task<bool> DeleteDeviceAsync(int deviceId)
        {
            try
            {
                // 使用 Include 加载所有关联数据
                var device = await _context.Devices.FirstOrDefaultAsync(d => d.DeviceId == deviceId);
                if (device == null)
                {
                    return false;
                }
                _context.Devices.Remove(device);

                return await _context.SaveChangesAsync() > 0;
            }
            catch (DbUpdateException ex)
            {
                throw new ApplicationException("删除设备失败， ", ex);
            }
        }
    }
    public class DeviceTableService
    {
        private readonly IConfiguration _configuration;
        private readonly IoTDbContext _context;

        public DeviceTableService(IoTDbContext context)
        {
            _context = context;

            _configuration = _context.GetService<IConfiguration>();
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
        public async Task<IEnumerable<dynamic>> GetDataByTableNameAsync(string tableName, string orderby, int topNumber)
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

        /// <summary>
        /// 分页查询表数据
        /// </summary>
        public async Task<(IEnumerable<dynamic> Data, int TotalCount)> GetDataByTableNamePagedAsync(
            string tableName, string orderby, string where, int pageNumber = 1, int pageSize = 20)
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

            //// 分页查询数据
            //var dataSql = $@"
            //    SELECT * FROM [{tableName}]
            //    ORDER BY (SELECT NULL)
            //    OFFSET @Offset ROWS
            //    FETCH NEXT @PageSize ROWS ONLY";

            // 安全地构建SQL语句
            var sqlBuilder = new StringBuilder();

            // 构建基础SQL
            sqlBuilder.AppendLine($"SELECT * FROM [{tableName}]");
            sqlBuilder.AppendLine(where);
            sqlBuilder.AppendLine(orderby);
            sqlBuilder.AppendLine($"OFFSET @Offset ROWS");
            sqlBuilder.AppendLine($"FETCH NEXT @PageSize ROWS ONLY");

            var dataSql = sqlBuilder.ToString();

            var offset = (pageNumber - 1) * pageSize;
            var data = await connection.QueryAsync(dataSql, new
            {
                Offset = offset,
                PageSize = pageSize
            });

            return (data, totalCount);
        }



        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<DeviceTable> AddFactorAsync(DeviceTable device)
        {
            try
            {
                device.CreatedTime = DateTime.Now;
                _context.DeviceTables.Add(device);
                await _context.SaveChangesAsync();
                return device;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("添加设备失败， ", ex);

            }
        }
        /// <summary>
        /// 更新设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        public async Task<DeviceTable> UpdateFactorAsync(DeviceTable device)
        {
            try
            {
                // 先查询现有的设备
                var existingDevice = await _context.DeviceTables
                    .FirstOrDefaultAsync(d => d.Id == device.Id);

                if (existingDevice == null)
                {
                    throw new KeyNotFoundException($"未找到ID为 {device.Id} 的设备");
                }
                existingDevice.Id = device.Id;//
                existingDevice.TableName = device.TableName;//
                existingDevice.FieldName = device.FieldName;//
                existingDevice.FieldType = device.FieldType;//数据类型
                existingDevice.DisplayName = device.DisplayName;//显示名称
                existingDevice.DisplayUnit = device.DisplayUnit;//显示单位
                existingDevice.SortOrder = device.SortOrder;//排序
                existingDevice.IsVisible = device.IsVisible;//是否显示
                existingDevice.CreatedTime = device.CreatedTime;//创建时间
                existingDevice.Remarks = device.Remarks;//说明
                existingDevice.ConfigMinValue = device.ConfigMinValue;//最小阈值
                existingDevice.ConfigMaxValue = device.ConfigMaxValue;//最大阈值
                existingDevice.IsAlarm = device.IsAlarm;//是否阈值
                existingDevice.ConfigType = device.ConfigType;// 阈值比较类型 



                // 更新时间
                existingDevice.CreatedTime = DateTime.Now;

                // 不更新 fieldName，保持原值
                // existingDevice.fieldName = device.fieldName; // 注释掉这行

                _context.DeviceTables.Update(existingDevice);
                await _context.SaveChangesAsync();
                return existingDevice;
                //device.CreatedTime = DateTime.Now;
                //_context.DeviceTables.Update(device);
                //await _context.SaveChangesAsync();
                //return device;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("更新设备失败， ", ex);
            }

        }
        /// <summary>
        /// 删除设备
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public async Task<bool> DeleteDeviceAsync(int Id)
        {
            try
            {
                // 使用 Include 加载所有关联数据
                var device = await _context.DeviceTables.FirstOrDefaultAsync(d => d.Id == Id);
                if (device == null)
                {
                    return false;
                }
                _context.DeviceTables.Remove(device);

                return await _context.SaveChangesAsync() > 0;
            }
            catch (DbUpdateException ex)
            {
                throw new ApplicationException("删除设备失败， ", ex);
            }
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
    }
}
