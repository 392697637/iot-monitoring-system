// Services/OpcUaService.cs
using Microsoft.Extensions.Logging;
using Opc.Ua;
using Opc.Ua.Client;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpcUaSession = Opc.Ua.Client.Session;
using OpcUaStatusCodes = Opc.Ua.StatusCodes;

namespace IoTMonitor.Services
{
    public class OpcUaService : IDisposable
    {
        private readonly ILogger<OpcUaService> _logger;
        private readonly DeviceTableService _deviceTableService;
        private readonly ConcurrentDictionary<string, SessionWrapper> _sessions = new();
        private readonly ConcurrentDictionary<int, SubscriptionWrapper> _subscriptions = new();
        private readonly SemaphoreSlim _sessionLock = new(1, 1);
        private bool _disposed;

        public OpcUaService(ILogger<OpcUaService> logger, DeviceTableService deviceTableService)
        {
            _logger = logger;
            _deviceTableService = deviceTableService;
        }

        /// <summary>
        /// 创建 OPC UA 应用程序配置
        /// </summary>
        private Opc.Ua.ApplicationConfiguration CreateApplicationConfiguration()
        {
            return new Opc.Ua.ApplicationConfiguration
            {
                ApplicationName = "IoTMonitor OPC UA Client",
                ApplicationType = Opc.Ua.ApplicationType.Client,
                ApplicationUri = $"urn:{System.Net.Dns.GetHostName()}:IoTMonitor:OPCUAClient",
                ProductUri = "http://iotmonitor.com",

                SecurityConfiguration = new Opc.Ua.SecurityConfiguration
                {
                    ApplicationCertificate = new Opc.Ua.CertificateIdentifier
                    {
                        StoreType = Opc.Ua.CertificateStoreType.Directory,
                        StorePath = "OPC Foundation/CertificateStores/MachineDefault/Applications",
                        SubjectName = "IoTMonitor OPC UA Client"
                    },
                    TrustedPeerCertificates = new Opc.Ua.CertificateTrustList
                    {
                        StoreType = Opc.Ua.CertificateStoreType.Directory,
                        StorePath = "OPC Foundation/CertificateStores/MachineDefault/Trusted"
                    },
                    TrustedIssuerCertificates = new Opc.Ua.CertificateTrustList
                    {
                        StoreType = Opc.Ua.CertificateStoreType.Directory,
                        StorePath = "OPC Foundation/CertificateStores/MachineDefault/Issuers"
                    },
                    RejectedCertificateStore = new Opc.Ua.CertificateTrustList
                    {
                        StoreType = Opc.Ua.CertificateStoreType.Directory,
                        StorePath = "OPC Foundation/CertificateStores/MachineDefault/Rejected"
                    },
                    AutoAcceptUntrustedCertificates = true,
                    MinimumCertificateKeySize = 1024
                },

                TransportConfigurations = new Opc.Ua.TransportConfigurationCollection(),

                ClientConfiguration = new Opc.Ua.ClientConfiguration
                {
                    DefaultSessionTimeout = 60 * 1000, // 60 seconds
                    MinSubscriptionLifetime = 10 * 1000 // 10 seconds
                },

                ServerConfiguration = new Opc.Ua.ServerConfiguration
                {
                    BaseAddresses = { "localhost" },
                    SecurityPolicies = new Opc.Ua.ServerSecurityPolicyCollection()
                },

                TraceConfiguration = new Opc.Ua.TraceConfiguration
                {
                    OutputFilePath = "Logs/opcua_trace.log",
                    DeleteOnLoad = true
                }
            };
        }

        /// <summary>
        /// 连接到 OPC UA 服务器
        /// </summary>
        public async Task<OpcUaSession> ConnectToServerAsync(string serverUrl, string username = null, string password = null, int timeout = 30000)
        {
            await _sessionLock.WaitAsync();
            try
            {
                if (_sessions.TryGetValue(serverUrl, out var sessionWrapper) &&
                    sessionWrapper.Session != null &&
                    sessionWrapper.Session.Connected)
                {
                    _logger.LogDebug($"使用现有连接: {serverUrl}");
                    return sessionWrapper.Session;
                }

                _logger.LogInformation($"正在连接到 OPC UA 服务器: {serverUrl}");

                // 创建应用程序配置
                var config = CreateApplicationConfiguration();
                await config.ValidateAsync(ApplicationType.Client, CancellationToken.None);

                // 方法1: 使用 ApplicationConfiguration 来查找端点
                // 在 OPC UA .NET Standard 1.5.x 中，SelectEndpointAsync 通常需要 ApplicationConfiguration
                var endpointDescription = await CoreClientUtils.SelectEndpointAsync(
                    config,              // ApplicationConfiguration
                    serverUrl,           // discoveryUrl
                    false,               // useSecurity
                    15000,               // timeout
                    CancellationToken.None);

                // 方法2: 或者使用这个重载（如果存在）
                // var endpointDescription = await CoreClientUtils.SelectEndpointAsync(
                //     config,
                //     serverUrl,
                //     false,
                //     token: CancellationToken.None);

                // 方法3: 或者使用静态方法（如果存在）
                // var endpointDescription = await Opc.Ua.Client.Session.SelectEndpointAsync(
                //     serverUrl,
                //     false,
                //     15000,
                //     CancellationToken.None);

                // 如果终端点支持安全，使用安全连接
                if (endpointDescription.SecurityMode != Opc.Ua.MessageSecurityMode.None)
                {
                    _logger.LogInformation($"使用安全连接: {endpointDescription.SecurityMode}");
                }

                var endpointConfiguration = Opc.Ua.EndpointConfiguration.Create(config);
                var endpoint = new Opc.Ua.ConfiguredEndpoint(null, endpointDescription, endpointConfiguration);

                // 创建用户身份
                Opc.Ua.UserIdentity userIdentity;
                if (!string.IsNullOrEmpty(username))
                {
                    var pwdBytes = string.IsNullOrEmpty(password) ? Array.Empty<byte>() : System.Text.Encoding.UTF8.GetBytes(password);
                    userIdentity = new UserIdentity(username, pwdBytes);
                    _logger.LogInformation($"使用用户认证: {username}");
                }
                else
                {
                    userIdentity = new Opc.Ua.UserIdentity();
                    _logger.LogInformation("使用匿名认证");
                }

                // 禁用证书验证
                config.CertificateValidator.CertificateValidation += (sender, args) =>
                {
                    args.Accept = true;
                };

                // 使用正确的 Session.Create 方法
                var session = await OpcUaSession.Create(
                    config,
                    endpoint,
                    updateBeforeConnect: true,
                    checkDomain: false,
                    sessionName: config.ApplicationName,
                    sessionTimeout: (uint)timeout,
                    identity: userIdentity,
                    preferredLocales: null);

                // 设置会话参数
                session.KeepAliveInterval = 5000; // 5秒心跳
                session.KeepAlive += (sender, e) => OnKeepAlive(sender as OpcUaSession, e);

                // 包装并存储会话
                sessionWrapper = new SessionWrapper(session);
                _sessions[serverUrl] = sessionWrapper;

                _logger.LogInformation($"成功连接到 OPC UA 服务器: {serverUrl}");
                return session;
            }
            catch (Exception ex)
            {
                _logger.LogError($"连接 OPC UA 服务器失败 ({serverUrl}): {ex.Message}");
                throw new Exception($"连接失败: {ex.Message}", ex);
            }
            finally
            {
                _sessionLock.Release();
            }
        }

        /// <summary>
        /// 心跳事件处理
        /// </summary>
        private void OnKeepAlive(OpcUaSession session, Opc.Ua.Client.KeepAliveEventArgs e)
        {
            if (e.Status != null && Opc.Ua.ServiceResult.IsNotGood(e.Status))
            {
                _logger.LogWarning($"会话心跳异常: {e.Status}");

                // 尝试重新连接
                var sessionWrapper = _sessions.Values.FirstOrDefault(s => s.Session == session);
                if (sessionWrapper != null)
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            await ReconnectSessionAsync(sessionWrapper);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"重新连接失败: {ex.Message}");
                        }
                    });
                }
            }
        }

        /// <summary>
        /// 重新连接会话
        /// </summary>
        private async Task ReconnectSessionAsync(SessionWrapper sessionWrapper)
        {
            _logger.LogInformation("正在重新连接会话...");

            try
            {
                var oldSession = sessionWrapper.Session;
                var serverUrl = _sessions.FirstOrDefault(x => x.Value == sessionWrapper).Key;

                if (!string.IsNullOrEmpty(serverUrl))
                {
                    // 关闭旧会话
                    oldSession?.Dispose();

                    // 重新连接
                    var newSession = await ConnectToServerAsync(
                        serverUrl,
                        sessionWrapper.Username,
                        sessionWrapper.Password);

                    // 重新创建订阅
                    await RestoreSubscriptionsAsync(sessionWrapper, newSession);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"重新连接失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 创建订阅并监控节点
        /// </summary>
        public async Task<Subscription> CreateSubscriptionForDeviceAsync(
            string serverUrl, string username, string password, string tableName,
            List<string> nodeIds, int publishingInterval = 1000, int deviceId = 0)
        {
            try
            {
                var session = await ConnectToServerAsync(serverUrl, username, password);

                // 创建订阅
                var subscription = new Subscription(session.DefaultSubscription)
                {
                    PublishingInterval = publishingInterval,
                    KeepAliveCount = 10,
                    LifetimeCount = 100,
                    PublishingEnabled = true,
                    Priority = 1,
                    DisplayName = $"Device_{deviceId}_{tableName}"
                };

                // 添加订阅到会话
                session.AddSubscription(subscription);
                await subscription.CreateAsync();

                // 为每个节点创建监控项
                foreach (var nodeId in nodeIds)
                {
                    var monitoredItem = new MonitoredItem(subscription.DefaultItem)
                    {
                        StartNodeId = nodeId,
                        AttributeId = Opc.Ua.Attributes.Value,
                        DisplayName = nodeId,
                        // 修正：使用正确的 MonitoringMode 枚举值
                        MonitoringMode = MonitoringMode.Reporting,
                        SamplingInterval = publishingInterval,
                        QueueSize = 1,
                        DiscardOldest = true
                    };

                    monitoredItem.Notification += (MonitoredItem item, MonitoredItemNotificationEventArgs e) =>
                    {
                        try
                        {
                            OnDataChanged(item, e, tableName, nodeId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"处理数据变更通知失败: {ex.Message}");
                        }
                    };

                    subscription.AddItem(monitoredItem);
                }

                // 应用变更
                await subscription.ApplyChangesAsync();

                // 存储订阅信息
                var subscriptionWrapper = new SubscriptionWrapper(subscription, deviceId, tableName, nodeIds);
                if (deviceId > 0)
                {
                    _subscriptions[deviceId] = subscriptionWrapper;
                }

                // 更新会话包装器中的认证信息
                if (_sessions.TryGetValue(serverUrl, out var sessionWrapper))
                {
                    sessionWrapper.Username = username;
                    sessionWrapper.Password = password;
                }

                _logger.LogInformation($"为设备 {deviceId} 表 {tableName} 创建了 {nodeIds.Count} 个监控项");
                return subscription;
            }
            catch (Exception ex)
            {
                _logger.LogError($"创建订阅失败: {ex.Message}");
                throw new Exception($"创建订阅失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 恢复订阅
        /// </summary>
        private async Task RestoreSubscriptionsAsync(SessionWrapper oldSessionWrapper, OpcUaSession newSession)
        {
            // 查找所有关联的订阅
            var subscriptionsToRestore = _subscriptions.Values
                .Where(s => s.DeviceId > 0)
                .ToList();

            foreach (var subscriptionWrapper in subscriptionsToRestore)
            {
                try
                {
                    await CreateSubscriptionForDeviceAsync(
                        _sessions.First(x => x.Value == oldSessionWrapper).Key,
                        oldSessionWrapper.Username,
                        oldSessionWrapper.Password,
                        subscriptionWrapper.TableName,
                        subscriptionWrapper.NodeIds,
                        (int)subscriptionWrapper.Subscription.PublishingInterval,
                        subscriptionWrapper.DeviceId);

                    _logger.LogInformation($"已恢复设备 {subscriptionWrapper.DeviceId} 的订阅");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"恢复订阅失败 (设备 {subscriptionWrapper.DeviceId}): {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 数据变化回调
        /// </summary>
        private async void OnDataChanged(Opc.Ua.Client.MonitoredItem item, Opc.Ua.Client.MonitoredItemNotificationEventArgs e, string tableName, string nodeId)
        {
            try
            {
                foreach (var value in item.DequeueValues())
                {
                    if (value.Value != null && value.Value.GetType() != typeof(Opc.Ua.ExtensionObject))
                    {
                        await SaveOpcUaDataAsync(tableName, nodeId, value.Value, value.SourceTimestamp);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"保存 OPC UA 数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 保存 OPC UA 数据到数据库
        /// </summary>
        private async Task SaveOpcUaDataAsync(string tableName, string nodeId, object value, DateTime timestamp)
        {
            try
            {
                // 获取表字段信息
                var tableFields = await _deviceTableService.GetTableFieldsAsync(tableName);

                // 根据 nodeId 映射到字段名
                var fieldName = MapNodeIdToFieldName(nodeId);

                if (tableFields.ContainsKey(fieldName))
                {
                    // 转换数据类型
                    var convertedValue = ConvertOpcUaValueToDatabaseType(value, tableFields[fieldName]);

                    if (convertedValue != null)
                    {
                        var data = new Dictionary<string, object>
                        {
                            ["Timestamp"] = timestamp,
                            [fieldName] = convertedValue
                        };

                        await _deviceTableService.InsertDataAsync(tableName, data);

                        _logger.LogDebug($"保存数据到 {tableName}.{fieldName}: {convertedValue}");
                    }
                }
                else
                {
                    _logger.LogWarning($"表 {tableName} 中没有找到字段 {fieldName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"保存数据到表 {tableName} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 映射节点ID到字段名
        /// </summary>
        private string MapNodeIdToFieldName(string nodeId)
        {
            try
            {
                // 解析节点ID
                var parsedNodeId = Opc.Ua.NodeId.Parse(nodeId);

                // 使用标识符部分作为字段名
                var identifier = parsedNodeId.Identifier.ToString();

                // 清理特殊字符
                return identifier
                    .Replace('.', '_')
                    .Replace(' ', '_')
                    .Replace('-', '_')
                    .Replace(':', '_')
                    .Replace('/', '_')
                    .Replace('\\', '_');
            }
            catch
            {
                // 如果解析失败，使用简单清理
                return nodeId
                    .Replace("ns=", "")
                    .Replace(";s=", "_")
                    .Replace(";i=", "_")
                    .Replace(";g=", "_")
                    .Replace(";b=", "_")
                    .Replace(".", "_")
                    .Replace(" ", "_");
            }
        }

        /// <summary>
        /// 转换 OPC UA 值到数据库类型
        /// </summary>
        private object ConvertOpcUaValueToDatabaseType(object opcValue, Type dbType)
        {
            try
            {
                if (opcValue == null)
                    return DBNull.Value;

                // 处理特殊类型
                if (opcValue is Opc.Ua.Variant variant)
                {
                    opcValue = variant.Value;
                }

                // 处理数组
                if (opcValue is Array array && dbType.IsArray == false)
                {
                    // 如果数据库不是数组类型，取第一个元素
                    if (array.Length > 0)
                        opcValue = array.GetValue(0);
                    else
                        return DBNull.Value;
                }

                // 转换为目标类型
                return Convert.ChangeType(opcValue, dbType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"类型转换失败: {opcValue?.GetType().Name} -> {dbType.Name}: {ex.Message}");
                return DBNull.Value;
            }
        }

        /// <summary>
        /// 浏览节点
        /// </summary>
        public async Task<List<NodeInfo>> BrowseNodesAsync(
            string serverUrl,
            string nodeId = null,
            string username = null,
            string password = null,
            int maxDepth = 2)
        {
            var nodes = new List<NodeInfo>();

            try
            {
                var session = await ConnectToServerAsync(serverUrl, username, password);

                // 确定起始节点
                var startNode = string.IsNullOrEmpty(nodeId)
                    ? Opc.Ua.ObjectIds.ObjectsFolder
                    : Opc.Ua.NodeId.Parse(nodeId);

                // 递归浏览
                await BrowseRecursiveAsync(session, startNode, nodes, 0, maxDepth);

                return nodes;
            }
            catch (Exception ex)
            {
                _logger.LogError($"浏览节点失败: {ex.Message}");
                throw new Exception($"浏览节点失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 递归浏览节点
        /// </summary>
        private async Task BrowseRecursiveAsync(
            OpcUaSession session,
            Opc.Ua.NodeId nodeId,
            List<NodeInfo> nodes,
            int currentDepth,
            int maxDepth)
        {
            if (currentDepth >= maxDepth)
                return;

            try
            {
                // 创建浏览描述
                var browseDescription = new Opc.Ua.BrowseDescription
                {
                    NodeId = nodeId,
                    BrowseDirection = Opc.Ua.BrowseDirection.Forward,
                    ReferenceTypeId = Opc.Ua.ReferenceTypeIds.HierarchicalReferences,
                    IncludeSubtypes = true,
                    NodeClassMask = (uint)(Opc.Ua.NodeClass.Object | Opc.Ua.NodeClass.Variable | Opc.Ua.NodeClass.Method),
                    ResultMask = (uint)Opc.Ua.BrowseResultMask.All
                };

                var browseDescriptionCollection = new Opc.Ua.BrowseDescriptionCollection { browseDescription };

                // 执行浏览
                session.Browse(
                    null,
                    null,
                    0,
                    browseDescriptionCollection,
                    out var results,
                    out var diagnosticInfos);

                if (results == null || results.Count == 0)
                    return;

                var result = results[0];
                if (result.StatusCode != OpcUaStatusCodes.Good || result.References == null)
                    return;

                foreach (var reference in result.References)
                {
                    var nodeInfo = new NodeInfo
                    {
                        NodeId = reference.NodeId.ToString(),
                        DisplayName = reference.DisplayName?.Text ?? reference.NodeId.ToString(),
                        NodeClass = reference.NodeClass.ToString(),
                        BrowseName = reference.BrowseName.ToString()
                    };

                    nodes.Add(nodeInfo);

                    // 如果是对象或变量文件夹，递归浏览
                    if (reference.NodeClass == Opc.Ua.NodeClass.Object ||
                        reference.BrowseName.Name == "Objects" ||
                        reference.BrowseName.Name == "Variables")
                    {
                        await BrowseRecursiveAsync(
                            session,
                            Opc.Ua.ExpandedNodeId.ToNodeId(reference.NodeId, session.NamespaceUris),
                            nodes,
                            currentDepth + 1,
                            maxDepth);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"浏览节点 {nodeId} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取节点值
        /// </summary>
        public async Task<NodeValue> ReadNodeValueAsync(
            string serverUrl,
            string nodeId,
            string username = null,
            string password = null)
        {
            try
            {
                var session = await ConnectToServerAsync(serverUrl, username, password);
                var nodeIdParsed = Opc.Ua.NodeId.Parse(nodeId);

                var readValueId = new Opc.Ua.ReadValueId
                {
                    NodeId = nodeIdParsed,
                    AttributeId = Opc.Ua.Attributes.Value
                };

                var readValueIdCollection = new Opc.Ua.ReadValueIdCollection { readValueId };

                session.Read(
                    null,
                    0,
                    Opc.Ua.TimestampsToReturn.Both,
                    readValueIdCollection,
                    out var dataValues,
                    out var diagnosticInfos);

                if (dataValues == null || dataValues.Count == 0)
                    throw new Exception("未读取到数据");

                var dataValue = dataValues[0];

                if (!Opc.Ua.StatusCode.IsGood(dataValue.StatusCode))
                    throw new Exception($"读取失败: {dataValue.StatusCode}");

                return new NodeValue
                {
                    NodeId = nodeId,
                    Value = dataValue.Value,
                    StatusCode = dataValue.StatusCode.ToString(),
                    SourceTimestamp = dataValue.SourceTimestamp,
                    ServerTimestamp = dataValue.ServerTimestamp
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"读取节点值失败 ({nodeId}): {ex.Message}");
                throw new Exception($"读取节点值失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 写入节点值
        /// </summary>
        public async Task<bool> WriteNodeValueAsync(
            string serverUrl,
            string nodeId,
            object value,
            string username = null,
            string password = null)
        {
            try
            {
                var session = await ConnectToServerAsync(serverUrl, username, password);
                var nodeIdParsed = Opc.Ua.NodeId.Parse(nodeId);

                // 先读取数据类型
                var readValueId = new Opc.Ua.ReadValueId
                {
                    NodeId = nodeIdParsed,
                    AttributeId = Opc.Ua.Attributes.DataType
                };

                session.Read(
                    null,
                    0,
                    Opc.Ua.TimestampsToReturn.Neither,
                    new Opc.Ua.ReadValueIdCollection { readValueId },
                    out var dataValues,
                    out _);

                if (dataValues == null || dataValues.Count == 0 || !Opc.Ua.StatusCode.IsGood(dataValues[0].StatusCode))
                    throw new Exception("无法读取节点数据类型");

                var dataTypeId = (Opc.Ua.NodeId)dataValues[0].Value;
                // 修正：使用正确的 BuiltInType 转换方法
                var builtInType = Opc.Ua.TypeInfo.GetBuiltInType(dataTypeId, session.TypeTree);

                // 创建写入值
                var writeValue = new Opc.Ua.WriteValue
                {
                    NodeId = nodeIdParsed,
                    AttributeId = Opc.Ua.Attributes.Value,
                    Value = new Opc.Ua.DataValue
                    {
                        // 修正：使用正确的 Variant 构造函数
                        Value = new Opc.Ua.Variant(value),
                        StatusCode = OpcUaStatusCodes.Good,
                        SourceTimestamp = DateTime.UtcNow
                    }
                };

                var writeValueCollection = new Opc.Ua.WriteValueCollection { writeValue };

                session.Write(
                    null,
                    writeValueCollection,
                    out var results,
                    out var diagnosticInfos);

                if (results == null || results.Count == 0 || !Opc.Ua.StatusCode.IsGood(results[0]))
                    throw new Exception($"写入失败: {results?[0]}");

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"写入节点值失败 ({nodeId}): {ex.Message}");
                throw new Exception($"写入节点值失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取设备订阅状态
        /// </summary>
        public SubscriptionStatus? GetSubscriptionStatus(int deviceId)
        {
            if (!_subscriptions.TryGetValue(deviceId, out var wrapper) ||
                wrapper.Subscription == null)
            {
                return null;
            }

            var sub = wrapper.Subscription;

            return new SubscriptionStatus
            {
                DeviceId = deviceId,
                TableName = wrapper.TableName,
                IsConnected = sub.Created,
                NodeCount = (int)sub.MonitoredItemCount,
                PublishingInterval = (int)sub.PublishingInterval,
                LastActivity = wrapper.LastActivity
            };
        }

        /// <summary>
        /// 停止设备订阅
        /// </summary>
        public async Task<bool> StopSubscriptionAsync(int deviceId)
        {
            if (_subscriptions.TryRemove(deviceId, out var subscriptionWrapper))
            {
                try
                {
                    if (subscriptionWrapper.Subscription?.Created ?? false)
                    {
                        await subscriptionWrapper.Subscription.DeleteAsync(true);
                        subscriptionWrapper.Subscription.Dispose();
                    }

                    _logger.LogInformation($"已停止设备 {deviceId} 的订阅");
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError($"停止订阅失败 (设备 {deviceId}): {ex.Message}");
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// 获取所有活动会话
        /// </summary>
        public List<SessionInfo> GetActiveSessions()
        {
            return _sessions.Select(pair => new SessionInfo
            {
                ServerUrl = pair.Key,
                IsConnected = pair.Value.Session?.Connected ?? false,
                LastActivity = pair.Value.LastActivity,
                SubscriptionCount = (int)(pair.Value.Session?.SubscriptionCount ?? 0)
            }).ToList();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                // 停止所有订阅
                foreach (var subscriptionWrapper in _subscriptions.Values)
                {
                    try
                    {
                        subscriptionWrapper.Subscription?.DeleteAsync(true).Wait(5000);
                        subscriptionWrapper.Subscription?.Dispose();
                    }
                    catch
                    {
                        // 忽略清理错误
                    }
                }
                _subscriptions.Clear();

                // 关闭所有会话
                foreach (var sessionWrapper in _sessions.Values)
                {
                    try
                    {
                        sessionWrapper.Session?.Close();
                        sessionWrapper.Session?.Dispose();
                    }
                    catch
                    {
                        // 忽略关闭错误
                    }
                }
                _sessions.Clear();

                _sessionLock?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError($"释放资源时出错: {ex.Message}");
            }

            _logger.LogInformation("OPC UA 服务已释放");
        }

        #region 辅助类

        private class SessionWrapper
        {
            public OpcUaSession Session { get; }
            public string Username { get; set; }
            public string Password { get; set; }
            public DateTime LastActivity { get; set; }

            public SessionWrapper(OpcUaSession session)
            {
                Session = session;
                LastActivity = DateTime.UtcNow;
            }
        }

        private class SubscriptionWrapper
        {
            public Opc.Ua.Client.Subscription Subscription { get; }
            public int DeviceId { get; }
            public string TableName { get; }
            public List<string> NodeIds { get; }
            public DateTime LastActivity { get; set; }

            public SubscriptionWrapper(Opc.Ua.Client.Subscription subscription, int deviceId, string tableName, List<string> nodeIds)
            {
                Subscription = subscription;
                DeviceId = deviceId;
                TableName = tableName;
                NodeIds = nodeIds;
                LastActivity = DateTime.UtcNow;
            }
        }

        #endregion
    }

    #region DTO 类

    public class NodeInfo
    {
        public string NodeId { get; set; }
        public string DisplayName { get; set; }
        public string NodeClass { get; set; }
        public string BrowseName { get; set; }
        public string DataType { get; set; }
        public object Value { get; set; }
        public bool IsWritable { get; set; }
        public string Description { get; set; }
    }

    public class NodeValue
    {
        public string NodeId { get; set; }
        public object Value { get; set; }
        public string StatusCode { get; set; }
        public DateTime SourceTimestamp { get; set; }
        public DateTime ServerTimestamp { get; set; }
        public string DataType { get; set; }
    }

    public class SubscriptionStatus
    {
        public int DeviceId { get; set; }
        public string TableName { get; set; }
        public bool IsConnected { get; set; }
        public int NodeCount { get; set; }
        public int PublishingInterval { get; set; }
        public DateTime LastActivity { get; set; }
        public string Status { get; set; }
    }

    public class SessionInfo
    {
        public string ServerUrl { get; set; }
        public bool IsConnected { get; set; }
        public DateTime LastActivity { get; set; }
        public int SubscriptionCount { get; set; }
        public string Status { get; set; }
    }

    #endregion
}