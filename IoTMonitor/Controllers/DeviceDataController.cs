using IoTMonitor.Models;            // 添加模型引用
using IoTMonitor.Services;
using Microsoft.AspNetCore.Mvc;    // 添加 ControllerBase 和 ApiController
using System;
using System.Threading.Tasks;

namespace IoTMonitor.Controllers
{

    // Controllers/DeviceDataController.cs
    using Microsoft.AspNetCore.Mvc;

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
            var result = await _service.GetDeviceHistoryAsync(deviceId, start, end);
            return Ok(result);
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

}
