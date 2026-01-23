using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace IoTMonitor.Controllers
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
    [ApiController]
    [Route("api/[controller]")]
    public class AlarmController : ControllerBase
    {
        private readonly IAlarmService _service;
        private readonly ILogger<AlarmController> _logger;

        public AlarmController(IAlarmService service, ILogger<AlarmController> logger)
        {
            _service = service;
            _logger = logger;
        }

        /// <summary>
        /// 分页查询报警历史数据
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="alarmFactor">报警因子</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="orderby">排序条件</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns></returns>
        [HttpGet("history")]
        public async Task<IActionResult> GetAlarmHistoryPaged(
            [FromQuery] string deviceId = null,
            [FromQuery] string alarmFactor = null,
            [FromQuery] string startTime = null,
            [FromQuery] string endTime = null,
            [FromQuery] string orderby = "alarm_time DESC",
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // 参数验证
                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                // 构建查询条件
                string where = BuildWhereCondition(deviceId, alarmFactor, startTime, endTime);

                var (data, totalCount) = await _service.GetAlarmHistoryPagedAsync(
                    orderby, where, pageNumber, pageSize);

                var result = new
                {
                    success = true,
                    dataTable = data,
                    pageNumber = pageNumber,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                };

                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询报警历史数据时出错");
                return StatusCode(500, new
                {
                    success = false,
                    message = "服务器内部错误"
                });
            }
        }

        /// <summary>
        /// 获取报警统计信息
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetAlarmStats()
        {
            try
            {
                var stats = await _service.GetAlarmStatsAsync();

                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取报警统计信息时出错");
                return StatusCode(500, new
                {
                    success = false,
                    message = "服务器内部错误"
                });
            }
        }

        /// <summary>
        /// 删除单条报警记录
        /// </summary>
        /// <param name="id">报警ID</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAlarm(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { success = false, message = "ID参数无效" });
                }

                var result = await _service.DeleteAlarmAsync(id);

                if (!result)
                {
                    return NotFound(new { success = false, message = "报警记录不存在" });
                }

                return Ok(new
                {
                    success = true,
                    message = "删除成功"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除报警记录时出错，ID: {Id}", id);
                return StatusCode(500, new
                {
                    success = false,
                    message = "服务器内部错误"
                });
            }
        }

        /// <summary>
        /// 批量删除报警记录
        /// </summary>
        /// <param name="ids">报警ID数组</param>
        [HttpDelete("batch")]
        public async Task<IActionResult> BatchDeleteAlarm([FromBody] int[] ids)
        {
            try
            {
                if (ids == null || ids.Length == 0)
                {
                    return BadRequest(new { success = false, message = "请选择要删除的记录" });
                }

                // 限制一次最多删除100条
                if (ids.Length > 100)
                {
                    return BadRequest(new { success = false, message = "一次最多删除100条记录" });
                }

                var deletedCount = await _service.BatchDeleteAlarmAsync(ids);

                return Ok(new
                {
                    success = true,
                    message = $"成功删除 {deletedCount} 条记录",
                    deletedCount = deletedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除报警记录时出错");
                return StatusCode(500, new
                {
                    success = false,
                    message = "服务器内部错误"
                });
            }
        }

        /// <summary>
        /// 构建查询条件
        /// </summary>
        private string BuildWhereCondition(string deviceId, string alarmFactor, string startTime, string endTime)
        {
            var conditions = new List<string>();

            if (!string.IsNullOrEmpty(deviceId))
            {
                conditions.Add($"device_id = '{deviceId.Replace("'", "''")}'");
            }

            if (!string.IsNullOrEmpty(alarmFactor))
            {
                conditions.Add($"alarm_factor = '{alarmFactor.Replace("'", "''")}'");
            }

            if (!string.IsNullOrEmpty(startTime))
            {
                if (DateTime.TryParse(startTime, out DateTime start))
                {
                    conditions.Add($"alarm_time >= '{start:yyyy-MM-dd HH:mm:ss}'");
                }
            }

            if (!string.IsNullOrEmpty(endTime))
            {
                if (DateTime.TryParse(endTime, out DateTime end))
                {
                    // 如果是日期格式，自动补全时间
                    if (endTime.Length == 10) // YYYY-MM-DD
                    {
                        end = end.AddDays(1).AddSeconds(-1);
                    }
                    conditions.Add($"alarm_time <= '{end:yyyy-MM-dd HH:mm:ss}'");
                }
            }

            if (conditions.Count == 0)
            {
                return "1=1";
            }

            return string.Join(" AND ", conditions);
        }
    }
}
