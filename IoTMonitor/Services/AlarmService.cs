using Dapper;
using IoTMonitor.Controllers;
using Microsoft.Data.SqlClient;

namespace IoTMonitor.Services
{
    public interface IAlarmService
    {
        /// <summary>
        /// 分页查询报警历史数据
        /// </summary>
        Task<(List<dynamic> data, int totalCount)> GetAlarmHistoryPagedAsync(
            string orderby, string where, int pageNumber, int pageSize);

        /// <summary>
        /// 获取报警统计信息
        /// </summary>
        Task<dynamic> GetAlarmStatsAsync();

        /// <summary>
        /// 删除单条报警记录
        /// </summary>
        Task<bool> DeleteAlarmAsync(int id);

        /// <summary>
        /// 批量删除报警记录
        /// </summary>
        Task<int> BatchDeleteAlarmAsync(int[] ids);
    }
    public class AlarmService : IAlarmService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<AlarmService> _logger;
        private readonly string _connectionString;

        public AlarmService(IConfiguration configuration, ILogger<AlarmService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<(List<dynamic> data, int totalCount)> GetAlarmHistoryPagedAsync(
            string orderby, string where, int pageNumber, int pageSize)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 计算分页参数
                int offset = (pageNumber - 1) * pageSize;

                // 构建SQL查询
                string countSql = $@"
                    SELECT COUNT(*) 
                    FROM alarm_history 
                    WHERE {where}";

                string dataSql = $@"
                    SELECT 
                        id as Id,
                        device_id as DeviceId,
                        device_name as DeviceName,
                        alarm_time as AlarmTime,
                        alarm_factor as AlarmFactor,
                        factor_value as FactorValue,
                        alarm_description as AlarmDescription,
                        create_time as CreateTime
                    FROM alarm_history 
                    WHERE {where}
                    ORDER BY {orderby}
                    OFFSET {offset} ROWS FETCH NEXT {pageSize} ROWS ONLY";

                // 执行查询
                var totalCount = await connection.ExecuteScalarAsync<int>(countSql);
                var data = (await connection.QueryAsync<dynamic>(dataSql)).AsList();

                return (data, totalCount);
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "数据库查询报警历史数据失败");
                throw new ArgumentException("数据库查询失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询报警历史数据失败");
                throw;
            }
        }

        public async Task<dynamic> GetAlarmStatsAsync()
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string sql = @"
                    SELECT 
                        COUNT(*) as Total,
                        COUNT(CASE WHEN CONVERT(date, alarm_time) = CONVERT(date, GETDATE()) THEN 1 END) as Today,
                        COUNT(DISTINCT device_id) as DeviceCount,
                        COUNT(DISTINCT alarm_factor) as FactorCount
                    FROM alarm_history";

                var stats = await connection.QueryFirstOrDefaultAsync<dynamic>(sql);

                return stats ?? new
                {
                    Total = 0,
                    Today = 0,
                    DeviceCount = 0,
                    FactorCount = 0
                };
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "数据库查询报警统计失败");
                throw new ArgumentException("数据库查询失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询报警统计失败");
                throw;
            }
        }

        public async Task<bool> DeleteAlarmAsync(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                string sql = @"
                    DELETE FROM alarm_history 
                    WHERE id = @Id";

                var affectedRows = await connection.ExecuteAsync(sql, new { Id = id });

                return affectedRows > 0;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "数据库删除报警记录失败，ID: {Id}", id);
                throw new ArgumentException("数据库操作失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除报警记录失败，ID: {Id}", id);
                throw;
            }
        }

        public async Task<int> BatchDeleteAlarmAsync(int[] ids)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                // 构建参数化SQL
                var parameters = new DynamicParameters();
                var conditions = new List<string>();

                for (int i = 0; i < ids.Length; i++)
                {
                    var paramName = $"@Id{i}";
                    parameters.Add(paramName, ids[i]);
                    conditions.Add($"id = {paramName}");
                }

                string whereClause = string.Join(" OR ", conditions);
                string sql = $@"
                    DELETE FROM alarm_history 
                    WHERE {whereClause}";

                var affectedRows = await connection.ExecuteAsync(sql, parameters);

                return affectedRows;
            }
            catch (SqlException ex)
            {
                _logger.LogError(ex, "数据库批量删除报警记录失败");
                throw new ArgumentException("数据库操作失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除报警记录失败");
                throw;
            }
        }
    }
}
