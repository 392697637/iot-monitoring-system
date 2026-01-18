using IoTMonitor.Models;            // 添加模型引用
using IoTMonitor.Services;
using Microsoft.AspNetCore.Mvc;    // 添加 ControllerBase 和 ApiController
using System;
using System.Threading.Tasks;

namespace IoTMonitor.Controllers
{
    using Humanizer;

    // Controllers/DeviceDataController.cs
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore.Metadata.Internal;

    [ApiController]
    [Route("api/[controller]")]
    public class DeviceDataController : ControllerBase
    {
        private readonly DeviceDataService _service;

        public DeviceDataController(DeviceDataService service)
        {
            _service = service;
        }

        [HttpPost("add")]
        public async Task<IActionResult> AddDeviceData([FromBody] DeviceData data)
        {
            var result = await _service.AddDeviceDataAsync(data);
            return Ok(result);
        }

        [HttpGet("latest")]
        public async Task<IActionResult> GetLatest(int deviceId)
        {
            var result = await _service.GetLatestDeviceDataAsync(deviceId);
            return Ok(result);
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetHistory(int deviceId, DateTime start, DateTime end)
        {
            try
            {
                var result = await _service.GetDeviceHistoryAsync(deviceId, start, end);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // 记录错误，方便调试s
                Console.WriteLine(ex);
                return StatusCode(500, ex.Message);
            }
        }
    }

    // Controllers/DevicesController.cs
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly DeviceService _service;

        public DevicesController(DeviceService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllDevicesAsync());
        }

        [HttpPost("add")]
        public async Task<IActionResult> Add([FromBody] Device device)
        {
            return Ok(await _service.AddDeviceAsync(device));
        }
    }

    // Controllers/ThresholdsController.cs
    [ApiController]
    [Route("api/[controller]")]
    public class ThresholdsController : ControllerBase
    {
        private readonly ThresholdService _service;

        public ThresholdsController(ThresholdService service)
        {
            _service = service;
        }

        [HttpGet("{deviceId}")]
        public async Task<IActionResult> GetThresholds(int deviceId)
        {
            return Ok(await _service.GetThresholdsByDeviceAsync(deviceId));
        }

        [HttpPost("set")]
        public async Task<IActionResult> SetThreshold([FromBody] DeviceThreshold threshold)
        {
            return Ok(await _service.AddOrUpdateThresholdAsync(threshold));
        }
    }

    // Controllers/DeviceTableController.cs
    [ApiController]
    [Route("api/[controller]")]
    public class DeviceTableController : ControllerBase
    {
        private readonly DeviceTableService _service;

        public DeviceTableController(DeviceTableService service)
        {
            _service = service;
        }
        /// <summary>
        /// 根据表名查询数据
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns>表数据</returns>
        [HttpGet("dataByTableName")]
        public async Task<IActionResult> GetDataByTableName(string tableName, string orderby, int topNumber)
        {
            try
            {
                var result = await _service.GetDataByTableNameAsync(tableName, orderby, topNumber);
                return Ok(new
                {
                    success = true,
                    data = result,
                    tableName = tableName,
                    count = result?.Count() ?? 0
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                // 记录日志
                Console.WriteLine($"查询表 {tableName} 时出错: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "服务器内部错误",
                    detail = ex.Message
                });
            }
        }

        /// <summary>
        /// 分页查询表数据
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <param name="tableName">表名</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <param name="pageNumber">页码</param>
        /// <param name="pageSize">每页大小</param>
        /// <returns></returns>
        [HttpGet("dataByHistory")]
        public async Task<IActionResult> GetDataByHistoryPaged(
            [FromQuery] string tableName,
            [FromQuery] string orderby,
            [FromQuery] string where,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                // 参数验证
                if (string.IsNullOrEmpty(tableName))
                {
                    return BadRequest(new { success = false, message = "表名不能为空" });
                }

                if (pageNumber < 1) pageNumber = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var (data, totalCount) = await _service.GetDataByTableNamePagedAsync(tableName, orderby, where, pageNumber, pageSize);

                return Ok(new
                {
                    success = true,
                    data = data,
                    tableName = tableName,
                    pageNumber = pageNumber,
                    pageSize = pageSize,
                    totalCount = totalCount,
                    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
                //return Ok(new
                //{
                //    success = true,
                //    data = data,
                //    tableName = tableName,
                //    deviceName = device.DeviceName,
                //    pageNumber = pageNumber,
                //    pageSize = pageSize,
                //    totalCount = totalCount,
                //    totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                //    startTime = startTime,
                //    endTime = endTime
                //});
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"分页查询表 {tableName} 时出错: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "服务器内部错误"
                });
            }
        }
        /// <summary>
        /// 获取数据库中所有表名
        /// </summary>
        [HttpGet("tables")]
        public async Task<IActionResult> GetAllTableNames()
        {
            try
            {
                var tables = await _service.GetAllTableNamesAsync();
                return Ok(new
                {
                    success = true,
                    tables = tables,
                    count = tables.Count()
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取表名列表时出错: {ex.Message}");
                return StatusCode(500, new
                {
                    success = false,
                    message = "服务器内部错误"
                });
            }
        }
        [HttpGet("fieldByTableName")]
        public async Task<IActionResult> GetfieldByTableName(string tablename)
        {
            try
            {
                var result = await _service.GetablenameByDataAsync(tablename);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // 记录错误，方便调试s
                Console.WriteLine(ex);
                return StatusCode(500, ex.Message);
            }
        }
    }
}
