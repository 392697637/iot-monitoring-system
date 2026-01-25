using IoTMonitor.Models;            // 添加模型引用
using IoTMonitor.Services;
using Microsoft.AspNetCore.Mvc;    // 添加 ControllerBase 和 ApiController
using System;
using System.Threading.Tasks;

namespace IoTMonitor.Controllers
{
    using Humanizer;
    using Microsoft.AspNetCore.Mvc;
 

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
        /// <summary>
        /// 获取设备
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            return Ok(await _service.GetAllDevicesAsync());
        }
        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="device"></param>
        /// <returns></returns>
        [HttpPost("addDevice")]
        public async Task<IActionResult> AddDevice([FromBody] Device device)
        {
            return Ok(await _service.AddDeviceAsync(device));
        }
        /// <summary>
        /// 修改设备
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpPost("updateDevice")]
        public async Task<IActionResult> UpdateDevice([FromBody] Device device)
        {
            return Ok(await _service.UpdateDeviceAsync(device));
        }

        /// <summary>
        /// 删除设备
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        [HttpGet("deleteDevice")]
        public async Task<IActionResult> DeleteDevice(int deviceId)
        {
            return Ok(await _service.DeleteDeviceAsync(deviceId));
        }
    }

  }
