using IotMonitoringSystem.Infrastructure.Data;
using IotMonitoringSystem.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace IotMonitoringSystem.API.Data
{
    public static class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context)
        {
            // 确保数据库已创建
            await context.Database.EnsureCreatedAsync();

            // 检查是否已有数据
            if (await context.Devices.AnyAsync())
            {
                return; // 数据库已有种子数据
            }

            // 创建设备
            var devices = new List<Device>
            {
                new Device
                {
                    DeviceCode = "DEV001",
                    DeviceName = "温度传感器-01",
                    Location = "实验室A区",
                    Manufacturer = "华为",
                    Model = "WS-100",
                    SerialNumber = "SN2023001",
                    InstallationDate = new DateOnly(2023, 1, 15),
                    IsActive = true
                },
                new Device
                {
                    DeviceCode = "DEV002",
                    DeviceName = "湿度传感器-01",
                    Location = "实验室B区",
                    Manufacturer = "海康威视",
                    Model = "HS-200",
                    SerialNumber = "SN2023002",
                    InstallationDate = new DateOnly(2023, 1, 20),
                    IsActive = true
                },
                new Device
                {
                    DeviceCode = "DEV003",
                    DeviceName = "电流监测器-01",
                    Location = "配电室",
                    Manufacturer = "西门子",
                    Model = "CM-300",
                    SerialNumber = "SN2023003",
                    InstallationDate = new DateOnly(2023, 2, 1),
                    IsActive = true
                },
                new Device
                {
                    DeviceCode = "DEV004",
                    DeviceName = "电压监测器-01",
                    Location = "主控室",
                    Manufacturer = "施耐德",
                    Model = "VM-400",
                    SerialNumber = "SN2023004",
                    InstallationDate = new DateOnly(2023, 2, 10),
                    IsActive = true
                },
                new Device
                {
                    DeviceCode = "DEV005",
                    DeviceName = "综合监测器-01",
                    Location = "数据中心",
                    Manufacturer = "霍尼韦尔",
                    Model = "IM-500",
                    SerialNumber = "SN2023005",
                    InstallationDate = new DateOnly(2023, 3, 1),
                    IsActive = true
                }
            };

            await context.Devices.AddRangeAsync(devices);
            await context.SaveChangesAsync();

            // 创建阈值设置
            var thresholds = new List<Threshold>();
            var random = new Random();
            
            foreach (var device in devices)
            {
                // 温度阈值
                thresholds.Add(new Threshold
                {
                    DeviceId = device.Id,
                    FactorType = FactorType.Temperature,
                    FactorName = "温度",
                    UpperLimit = 40.0m,
                    LowerLimit = 10.0m,
                    IsRealTimeAlert = true,
                    AlertMessage = "温度异常"
                });
                
                // 湿度阈值
                thresholds.Add(new Threshold
                {
                    DeviceId = device.Id,
                    FactorType = FactorType.Humidity,
                    FactorName = "湿度",
                    UpperLimit = 80.0m,
                    LowerLimit = 20.0m,
                    IsRealTimeAlert = true,
                    AlertMessage = "湿度异常"
                });
                
                // 电流阈值
                thresholds.Add(new Threshold
                {
                    DeviceId = device.Id,
                    FactorType = FactorType.Current,
                    FactorName = "电流",
                    UpperLimit = 20.0m,
                    LowerLimit = 0.5m,
                    IsRealTimeAlert = true,
                    AlertMessage = "电流异常"
                });
                
                // 电压阈值
                thresholds.Add(new Threshold
                {
                    DeviceId = device.Id,
                    FactorType = FactorType.Voltage,
                    FactorName = "电压",
                    UpperLimit = 250.0m,
                    LowerLimit = 210.0m,
                    IsRealTimeAlert = true,
                    AlertMessage = "电压异常"
                });
            }

            await context.Thresholds.AddRangeAsync(thresholds);
            await context.SaveChangesAsync();

            // 创建设备历史数据（最近24小时，每分钟一条）
            var deviceData = new List<DeviceData>();
            var now = DateTime.UtcNow;
            
            for (int i = 0; i < devices.Count; i++)
            {
                var device = devices[i];
                
                for (int j = 0; j < 1440; j++) // 24小时 = 1440分钟
                {
                    var timestamp = now.AddMinutes(-j);
                    
                    // 生成模拟数据
                    var temperature = 25.0m + (decimal)(Math.Sin(j / 60.0) * 5) + (decimal)(random.NextDouble() * 2 - 1);
                    var humidity = 50.0m + (decimal)(Math.Sin(j / 120.0) * 10) + (decimal)(random.NextDouble() * 3 - 1.5);
                    var current = 10.0m + (decimal)(Math.Sin(j / 180.0) * 3) + (decimal)(random.NextDouble() * 1 - 0.5);
                    var voltage = 220.0m + (decimal)(Math.Sin(j / 240.0) * 5) + (decimal)(random.NextDouble() * 2 - 1);
                    
                    // 随机生成报警数据（5%的概率）
                    var hasAlarm = random.Next(0, 100) < 5;
                    if (hasAlarm)
                    {
                        temperature += 20; // 模拟温度过高
                    }
                    
                    var status = hasAlarm ? DeviceStatus.Warning : DeviceStatus.Normal;
                    
                    deviceData.Add(new DeviceData
                    {
                        DeviceId = device.Id,
                        Temperature = Math.Round(temperature, 2),
                        Humidity = Math.Round(humidity, 2),
                        Current = Math.Round(current, 2),
                        Voltage = Math.Round(voltage, 2),
                        Status = status,
                        Timestamp = timestamp
                    });
                }
            }

            await context.DeviceData.AddRangeAsync(deviceData);
            await context.SaveChangesAsync();

            // 创建一些报警记录
            var alarms = new List<Alarm>();
            var alarmCount = random.Next(10, 20);
            
            for (int i = 0; i < alarmCount; i++)
            {
                var deviceIndex = random.Next(0, devices.Count);
                var device = devices[deviceIndex];
                var factorType = (FactorType)random.Next(1, 5);
                var isUpper = random.Next(0, 2) == 1;
                
                alarms.Add(new Alarm
                {
                    DeviceId = device.Id,
                    FactorType = factorType,
                    Value = isUpper ? 
                        thresholds.First(t => t.DeviceId == device.Id && t.FactorType == factorType).UpperLimit + 5 :
                        thresholds.First(t => t.DeviceId == device.Id && t.FactorType == factorType).LowerLimit - 5,
                    LimitType = isUpper ? "Upper" : "Lower",
                    Message = $"{device.DeviceName} {GetFactorName(factorType)} {(isUpper ? "超过上限" : "低于下限")}",
                    Timestamp = now.AddHours(-random.Next(0, 48)),
                    IsAcknowledged = random.Next(0, 2) == 1
                });
            }

            await context.Alarms.AddRangeAsync(alarms);
            await context.SaveChangesAsync();

            Console.WriteLine("种子数据已成功添加");
        }

        private static string GetFactorName(FactorType factorType)
        {
            return factorType switch
            {
                FactorType.Temperature => "温度",
                FactorType.Humidity => "湿度",
                FactorType.Current => "电流",
                FactorType.Voltage => "电压",
                _ => "未知"
            };
        }
    }
}