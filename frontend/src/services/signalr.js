import * as signalR from '@microsoft/signalr'
import store from '@/store'
import { Message } from 'element-ui'

class SignalRService {
  constructor() {
    this.connection = null
    this.callbacks = {
      connected: [],
      disconnected: [],
      reconnecting: [],
      deviceDataUpdate: [],
      newAlarm: [],
      thresholdUpdated: [],
      systemNotification: []
    }
  }

  // 初始化连接
  async initialize() {
    const baseUrl = process.env.VUE_APP_WS_BASE_URL || window.location.origin
    const hubUrl = `${baseUrl}/hubs/device`
    
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => localStorage.getItem('token') || '',
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          // 重连策略
          if (retryContext.previousRetryCount < 10) {
            return 1000 // 前10次重连，间隔1秒
          } else if (retryContext.previousRetryCount < 20) {
            return 5000 // 10-20次重连，间隔5秒
          } else {
            return 10000 // 20次以后，间隔10秒
          }
        }
      })
      .configureLogging(signalR.LogLevel.Warning)
      .build()

    // 设置事件处理
    this.setupEventHandlers()
  }

  // 设置事件处理器
  setupEventHandlers() {
    if (!this.connection) return

    // 连接事件
    this.connection.onclose(error => {
      console.log('SignalR连接关闭', error)
      this.triggerCallbacks('disconnected', { error })
      store.commit('connection/SET_CONNECTION_STATUS', 'disconnected')
    })

    this.connection.onreconnecting(error => {
      console.log('SignalR正在重连...', error)
      this.triggerCallbacks('reconnecting', { error })
      store.commit('connection/SET_CONNECTION_STATUS', 'connecting')
    })

    this.connection.onreconnected(connectionId => {
      console.log('SignalR重连成功', connectionId)
      this.triggerCallbacks('connected', { connectionId })
      store.commit('connection/SET_CONNECTION_STATUS', 'connected')
      Message.success('连接已恢复')
    })

    // 自定义事件
    this.connection.on('DeviceDataUpdate', data => {
      console.log('收到设备数据更新:', data)
      this.triggerCallbacks('deviceDataUpdate', data)
    })

    this.connection.on('NewAlarm', alarm => {
      console.log('收到新报警:', alarm)
      this.triggerCallbacks('newAlarm', alarm)
      
      // 播放报警音（如果未静音）
      if (!store.state.connection.isAlarmMuted) {
        this.playAlarmSound()
      }
      
      // 显示报警通知
      Message.warning({
        message: `设备 ${alarm.deviceName} 发生报警: ${alarm.message}`,
        duration: 5000,
        showClose: true
      })
    })

    this.connection.on('ThresholdUpdated', threshold => {
      console.log('收到阈值更新:', threshold)
      this.triggerCallbacks('thresholdUpdated', threshold)
      Message.success(`设备 ${threshold.deviceName} 的阈值已更新`)
    })

    this.connection.on('SystemNotification', notification => {
      console.log('收到系统通知:', notification)
      this.triggerCallbacks('systemNotification', notification)
      
      const { type, message } = notification
      const notificationType = type || 'info'
      
      Message[notificationType]({
        message,
        duration: 3000,
        showClose: true
      })
    })

    this.connection.on('CurrentDeviceData', data => {
      console.log('收到当前设备数据:', data)
      this.triggerCallbacks('deviceDataUpdate', data)
    })

    this.connection.on('SubscriptionConfirmed', result => {
      console.log('订阅确认:', result)
      Message.success(result.message)
    })

    this.connection.on('UnsubscriptionConfirmed', result => {
      console.log('取消订阅确认:', result)
      Message.success(result.message)
    })

    this.connection.on('HeartbeatResponse', response => {
      console.log('心跳响应:', response)
      store.commit('connection/SET_LAST_HEARTBEAT', Date.now())
    })
  }

  // 开始连接
  async start() {
    if (!this.connection) {
      await this.initialize()
    }

    try {
      await this.connection.start()
      console.log('SignalR连接成功')
      
      this.triggerCallbacks('connected', { connectionId: this.connection.connectionId })
      store.commit('connection/SET_CONNECTION_STATUS', 'connected')
      store.commit('connection/SET_LAST_HEARTBEAT', Date.now())
      
      // 开始心跳检测
      this.startHeartbeat()
      
      return true
    } catch (error) {
      console.error('SignalR连接失败:', error)
      store.commit('connection/SET_CONNECTION_STATUS', 'disconnected')
      store.commit('connection/SET_CONNECTION_ERROR', error.message)
      
      Message.error('实时连接失败，请刷新页面重试')
      throw error
    }
  }

  // 停止连接
  async stop() {
    if (this.connection) {
      try {
        // 停止心跳检测
        this.stopHeartbeat()
        
        await this.connection.stop()
        console.log('SignalR连接已停止')
        
        this.triggerCallbacks('disconnected')
        store.commit('connection/SET_CONNECTION_STATUS', 'disconnected')
        store.commit('connection/CLEAR_SUBSCRIPTIONS')
      } catch (error) {
        console.error('停止SignalR连接失败:', error)
        throw error
      }
    }
  }

  // 订阅设备
  async subscribeToDevice(deviceId) {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('连接未就绪')
    }

    try {
      await this.connection.invoke('SubscribeToDevice', deviceId)
      console.log(`已订阅设备 ${deviceId}`)
      return true
    } catch (error) {
      console.error(`订阅设备 ${deviceId} 失败:`, error)
      Message.error(`订阅设备 ${deviceId} 失败`)
      throw error
    }
  }

  // 取消订阅设备
  async unsubscribeFromDevice(deviceId) {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return
    }

    try {
      await this.connection.invoke('UnsubscribeFromDevice', deviceId)
      console.log(`已取消订阅设备 ${deviceId}`)
    } catch (error) {
      console.error(`取消订阅设备 ${deviceId} 失败:`, error)
    }
  }

  // 批量订阅设备
  async subscribeToDevices(deviceIds) {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error('连接未就绪')
    }

    try {
      await this.connection.invoke('SubscribeToDevices', deviceIds)
      console.log(`已批量订阅设备: ${deviceIds.join(',')}`)
    } catch (error) {
      console.error('批量订阅设备失败:', error)
      throw error
    }
  }

  // 发送心跳
  async sendHeartbeat() {
    if (!this.connection || this.connection.state !== signalR.HubConnectionState.Connected) {
      return
    }

    try {
      await this.connection.invoke('Heartbeat')
    } catch (error) {
      console.error('发送心跳失败:', error)
    }
  }

  // 开始心跳检测
  startHeartbeat() {
    this.heartbeatInterval = setInterval(() => {
      this.sendHeartbeat()
    }, 30000) // 每30秒发送一次心跳
  }

  // 停止心跳检测
  stopHeartbeat() {
    if (this.heartbeatInterval) {
      clearInterval(this.heartbeatInterval)
      this.heartbeatInterval = null
    }
  }

  // 播放报警音
  playAlarmSound() {
    try {
      const audio = new Audio('/alarm.mp3')
      audio.volume = 0.5
      audio.play().catch(e => {
        console.log('播放报警音失败:', e)
      })
    } catch (error) {
      console.error('创建报警音频失败:', error)
    }
  }

  // 触发回调
  triggerCallbacks(eventName, data) {
    const callbacks = this.callbacks[eventName]
    if (callbacks && callbacks.length > 0) {
      callbacks.forEach(callback => {
        try {
          callback(data)
        } catch (error) {
          console.error(`执行 ${eventName} 回调失败:`, error)
        }
      })
    }
  }

  // 注册事件回调
  onConnected(callback) {
    this.callbacks.connected.push(callback)
  }

  onDisconnected(callback) {
    this.callbacks.disconnected.push(callback)
  }

  onReconnecting(callback) {
    this.callbacks.reconnecting.push(callback)
  }

  onDeviceDataUpdate(callback) {
    this.callbacks.deviceDataUpdate.push(callback)
  }

  onNewAlarm(callback) {
    this.callbacks.newAlarm.push(callback)
  }

  onThresholdUpdated(callback) {
    this.callbacks.thresholdUpdated.push(callback)
  }

  onSystemNotification(callback) {
    this.callbacks.systemNotification.push(callback)
  }

  onReceivedHeartbeat(callback) {
    // 心跳回调特殊处理
    if (!this.callbacks.heartbeat) {
      this.callbacks.heartbeat = []
    }
    this.callbacks.heartbeat.push(callback)
  }

  // 获取连接状态
  getConnectionState() {
    if (!this.connection) {
      return 'Disconnected'
    }
    return this.connection.state
  }

  // 获取连接ID
  getConnectionId() {
    return this.connection ? this.connection.connectionId : null
  }

  // 检查连接是否正常
  isConnected() {
    return this.connection && this.connection.state === signalR.HubConnectionState.Connected
  }
}

// 创建单例
const signalRService = new SignalRService()

export default signalRService

// 导出常用方法
export const initialize = () => signalRService.initialize()
export const start = () => signalRService.start()
export const stop = () => signalRService.stop()
export const subscribeToDevice = (deviceId) => signalRService.subscribeToDevice(deviceId)
export const unsubscribeFromDevice = (deviceId) => signalRService.unsubscribeFromDevice(deviceId)
export const sendHeartbeat = () => signalRService.sendHeartbeat()
export const onConnected = (callback) => signalRService.onConnected(callback)
export const onDisconnected = (callback) => signalRService.onDisconnected(callback)
export const onReconnecting = (callback) => signalRService.onReconnecting(callback)
export const onDeviceDataUpdate = (callback) => signalRService.onDeviceDataUpdate(callback)
export const onNewAlarm = (callback) => signalRService.onNewAlarm(callback)
export const onThresholdUpdated = (callback) => signalRService.onThresholdUpdated(callback)
export const onSystemNotification = (callback) => signalRService.onSystemNotification(callback)
export const onReceivedHeartbeat = (callback) => signalRService.onReceivedHeartbeat(callback)
export const getConnectionState = () => signalRService.getConnectionState()
export const getConnectionId = () => signalRService.getConnectionId()
export const isConnected = () => signalRService.isConnected()