using IoTMonitor.Data;
using IoTMonitor.Models;
using Microsoft.EntityFrameworkCore;

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
            return await _context.DeviceDatas
                .Where(d => d.DeviceId == deviceId && d.CreatedAt >= start && d.CreatedAt <= end)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
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
}
