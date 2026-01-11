using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace IotMonitoringSystem.API.Hubs
{
    public class DeviceHub : Hub
    {
        private static readonly ConcurrentDictionary<string, string> _userConnections = new();
        private static readonly ConcurrentDictionary<int, HashSet<string>> _deviceSubscriptions = new();
        private readonly IMemoryCache _cache;
        private readonly ILogger<DeviceHub> _logger;

        public DeviceHub(IMemoryCache cache, ILogger<DeviceHub> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// 订阅设备实时数据
        /// </summary>
        public async Task SubscribeToDevice(int deviceId)
        {
            try
            {
                var connectionId = Context.ConnectionId;
                var groupName = GetDeviceGroupName(deviceId);
                
                await Groups.AddToGroupAsync(connectionId, groupName);
                
                // 记录订阅关系
                _deviceSubscriptions.AddOrUpdate(
                    deviceId,
                    new HashSet<string> { connectionId },
                    (key, existingSet) =>
                    {
                        existingSet.Add(connectionId);
                        return existingSet;
                    });
                
                // 记录用户连接
                _userConnections[connectionId] = Context.UserIdentifier ?? "anonymous";
                
                // 发送当前设备的最新数据
                var latestData = await GetLatestDeviceData(deviceId);
                if (latestData != null)
                {
                    await Clients.Caller.SendAsync("CurrentDeviceData", latestData);
                }
                
                _logger.LogInformation("用户 {User} 订阅设备 {DeviceId}", 
                    _userConnections[connectionId], deviceId);
                
                await Clients.Caller.SendAsync("SubscriptionConfirmed", 
                    new { DeviceId = deviceId, Message = "订阅成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "订阅设备失败，设备ID: {DeviceId}", deviceId);
                await Clients.Caller.SendAsync("SubscriptionError", 
                    new { DeviceId = deviceId, Message = "订阅失败" });
            }
        }

        /// <summary>
        /// 取消订阅设备
        /// </summary>
        public async Task UnsubscribeFromDevice(int deviceId)
        {
            try
            {
                var connectionId = Context.ConnectionId;
                var groupName = GetDeviceGroupName(deviceId);
                
                await Groups.RemoveFromGroupAsync(connectionId, groupName);
                
                // 更新订阅关系
                if (_deviceSubscriptions.TryGetValue(deviceId, out var connections))
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                    {
                        _deviceSubscriptions.TryRemove(deviceId, out _);
                    }
                }
                
                _logger.LogInformation("用户 {User} 取消订阅设备 {DeviceId}", 
                    _userConnections.TryGetValue(connectionId, out var user) ? user : "unknown", 
                    deviceId);
                
                await Clients.Caller.SendAsync("UnsubscriptionConfirmed", 
                    new { DeviceId = deviceId, Message = "取消订阅成功" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消订阅设备失败，设备ID: {DeviceId}", deviceId);
                await Clients.Caller.SendAsync("UnsubscriptionError", 
                    new { DeviceId = deviceId, Message = "取消订阅失败" });
            }
        }

        /// <summary>
        /// 批量订阅设备
        /// </summary>
        public async Task SubscribeToDevices(int[] deviceIds)
        {
            try
            {
                var connectionId = Context.ConnectionId;
                var tasks = new List<Task>();
                
                foreach (var deviceId in deviceIds)
                {
                    tasks.Add(SubscribeToDevice(deviceId));
                }
                
                await Task.WhenAll(tasks);
                
                _logger.LogInformation("用户 {User} 批量订阅设备: {DeviceIds}", 
                    _userConnections.TryGetValue(connectionId, out var user) ? user : "unknown", 
                    string.Join(",", deviceIds));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量订阅设备失败");
                await Clients.Caller.SendAsync("BatchSubscriptionError", 
                    new { Message = "批量订阅失败" });
            }
        }

        /// <summary>
        /// 发送设备数据更新
        /// </summary>
        public async Task SendDeviceUpdate(int deviceId, object data)
        {
            try
            {
                var groupName = GetDeviceGroupName(deviceId);
                await Clients.Group(groupName).SendAsync("DeviceDataUpdate", data);
                
                _logger.LogDebug("发送设备 {DeviceId} 数据更新", deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送设备数据更新失败，设备ID: {DeviceId}", deviceId);
            }
        }

        /// <summary>
        /// 发送报警通知
        /// </summary>
        public async Task SendAlarm(int deviceId, object alarm)
        {
            try
            {
                var groupName = GetDeviceGroupName(deviceId);
                await Clients.Group(groupName).SendAsync("NewAlarm", alarm);
                
                // 同时发送给所有连接（全局报警）
                await Clients.All.SendAsync("GlobalAlarm", new
                {
                    DeviceId = deviceId,
                    Alarm = alarm,
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogInformation("发送设备 {DeviceId} 报警通知", deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送报警通知失败，设备ID: {DeviceId}", deviceId);
            }
        }

        /// <summary>
        /// 发送阈值更新通知
        /// </summary>
        public async Task SendThresholdUpdate(int deviceId, object threshold)
        {
            try
            {
                var groupName = GetDeviceGroupName(deviceId);
                await Clients.Group(groupName).SendAsync("ThresholdUpdated", threshold);
                
                _logger.LogDebug("发送设备 {DeviceId} 阈值更新通知", deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送阈值更新通知失败，设备ID: {DeviceId}", deviceId);
            }
        }

        /// <summary>
        /// 发送系统通知
        /// </summary>
        public async Task SendSystemNotification(string message, string type = "info")
        {
            try
            {
                await Clients.All.SendAsync("SystemNotification", new
                {
                    Message = message,
                    Type = type,
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogInformation("发送系统通知: {Message}", message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发送系统通知失败");
            }
        }

        /// <summary>
        /// 心跳检测
        /// </summary>
        public async Task Heartbeat()
        {
            await Clients.Caller.SendAsync("HeartbeatResponse", new
            {
                Timestamp = DateTime.UtcNow,
                ServerTime = DateTime.UtcNow
            });
        }

        /// <summary>
        /// 获取订阅统计
        /// </summary>
        public async Task<object> GetSubscriptionStats()
        {
            var stats = new
            {
                TotalConnections = _userConnections.Count,
                TotalSubscriptions = _deviceSubscriptions.Sum(x => x.Value.Count),
                DeviceSubscriptions = _deviceSubscriptions.ToDictionary(
                    x => x.Key,
                    x => x.Value.Count),
                Timestamp = DateTime.UtcNow
            };
            
            return stats;
        }

        /// <summary>
        /// 连接建立时
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var connectionId = Context.ConnectionId;
            var userIdentifier = Context.UserIdentifier ?? "anonymous";
            
            _userConnections[connectionId] = userIdentifier;
            
            _logger.LogInformation("用户 {User} 连接建立，连接ID: {ConnectionId}", 
                userIdentifier, connectionId);
            
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 连接断开时
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            
            // 清理订阅关系
            foreach (var (deviceId, connections) in _deviceSubscriptions)
            {
                if (connections.Contains(connectionId))
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0)
                    {
                        _deviceSubscriptions.TryRemove(deviceId, out _);
                    }
                }
            }
            
            // 清理用户连接记录
            _userConnections.TryRemove(connectionId, out var user);
            
            if (exception != null)
            {
                _logger.LogWarning(exception, "用户 {User} 连接异常断开，连接ID: {ConnectionId}", 
                    user, connectionId);
            }
            else
            {
                _logger.LogInformation("用户 {User} 连接正常断开，连接ID: {ConnectionId}", 
                    user, connectionId);
            }
            
            await base.OnDisconnectedAsync(exception);
        }

        private string GetDeviceGroupName(int deviceId)
        {
            return $"device-{deviceId}";
        }

        private async Task<object?> GetLatestDeviceData(int deviceId)
        {
            var cacheKey = $"latest_device_data_{deviceId}";
            
            if (_cache.TryGetValue(cacheKey, out object? cachedData))
            {
                return cachedData;
            }
            
            // 这里应该从数据库或服务中获取最新数据
            // 简化实现，返回null
            return null;
        }
    }
}