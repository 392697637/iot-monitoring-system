import * as signalRService from '@/services/signalr'
import * as apiService from '@/services/api'

const state = {
  connectionStatus: 'disconnected', // connected, connecting, disconnected
  isAlarmMuted: false,
  autoRefresh: true,
  refreshInterval: 5000,
  lastHeartbeat: null,
  connectionError: null,
  subscriptions: new Set()
}

const getters = {
  isConnected: state => state.connectionStatus === 'connected',
  isConnecting: state => state.connectionStatus === 'connecting',
  isDisconnected: state => state.connectionStatus === 'disconnected',
  connectionLatency: state => {
    if (!state.lastHeartbeat) return null
    return Date.now() - state.lastHeartbeat
  }
}

const mutations = {
  SET_CONNECTION_STATUS(state, status) {
    state.connectionStatus = status
  },
  
  SET_ALARM_MUTED(state, muted) {
    state.isAlarmMuted = muted
  },
  
  SET_AUTO_REFRESH(state, enabled) {
    state.autoRefresh = enabled
  },
  
  SET_REFRESH_INTERVAL(state, interval) {
    state.refreshInterval = interval
  },
  
  SET_LAST_HEARTBEAT(state, timestamp) {
    state.lastHeartbeat = timestamp
  },
  
  SET_CONNECTION_ERROR(state, error) {
    state.connectionError = error
  },
  
  ADD_SUBSCRIPTION(state, deviceId) {
    state.subscriptions.add(deviceId)
  },
  
  REMOVE_SUBSCRIPTION(state, deviceId) {
    state.subscriptions.delete(deviceId)
  },
  
  CLEAR_SUBSCRIPTIONS(state) {
    state.subscriptions.clear()
  }
}

const actions = {
  async initializeConnection({ commit, dispatch }) {
    try {
      commit('SET_CONNECTION_STATUS', 'connecting')
      
      // 检查API连通性
      const healthResponse = await apiService.getHealth()
      if (!healthResponse.success) {
        throw new Error('API服务不可用')
      }
      
      // 初始化SignalR连接
      await signalRService.initialize()
      
      // 设置事件监听
      signalRService.onConnected(() => {
        commit('SET_CONNECTION_STATUS', 'connected')
        commit('SET_CONNECTION_ERROR', null)
        
        // 重新订阅之前的设备
        state.subscriptions.forEach(deviceId => {
          signalRService.subscribeToDevice(deviceId)
        })
      })
      
      signalRService.onDisconnected(() => {
        commit('SET_CONNECTION_STATUS', 'disconnected')
      })
      
      signalRService.onReconnecting(() => {
        commit('SET_CONNECTION_STATUS', 'connecting')
      })
      
      signalRService.onReceivedHeartbeat(() => {
        commit('SET_LAST_HEARTBEAT', Date.now())
      })
      
      signalRService.onDeviceDataUpdate((data) => {
        dispatch('devices/updateDeviceData', data, { root: true })
      })
      
      signalRService.onNewAlarm((alarm) => {
        dispatch('alarms/addNewAlarm', alarm, { root: true })
      })
      
      signalRService.onThresholdUpdated((threshold) => {
        dispatch('thresholds/updateThreshold', threshold, { root: true })
      })
      
      commit('SET_CONNECTION_STATUS', 'connected')
      commit('SET_LAST_HEARTBEAT', Date.now())
      
    } catch (error) {
      commit('SET_CONNECTION_STATUS', 'disconnected')
      commit('SET_CONNECTION_ERROR', error.message)
      console.error('连接初始化失败:', error)
    }
  },
  
  async connectSignalR({ commit }) {
    try {
      await signalRService.start()
      commit('SET_CONNECTION_STATUS', 'connected')
    } catch (error) {
      commit('SET_CONNECTION_STATUS', 'disconnected')
      commit('SET_CONNECTION_ERROR', error.message)
      throw error
    }
  },
  
  async disconnectSignalR({ commit }) {
    try {
      await signalRService.stop()
      commit('SET_CONNECTION_STATUS', 'disconnected')
      commit('CLEAR_SUBSCRIPTIONS')
    } catch (error) {
      console.error('断开连接失败:', error)
    }
  },
  
  toggleAlarmMute({ commit, state }) {
    const muted = !state.isAlarmMuted
    commit('SET_ALARM_MUTED', muted)
  },
  
  toggleAutoRefresh({ commit, state }) {
    const enabled = !state.autoRefresh
    commit('SET_AUTO_REFRESH', enabled)
  },
  
  setRefreshInterval({ commit }, interval) {
    if (interval >= 1000 && interval <= 30000) {
      commit('SET_REFRESH_INTERVAL', interval)
    }
  },
  
  subscribeToDevice({ commit, state }, deviceId) {
    if (!state.subscriptions.has(deviceId)) {
      signalRService.subscribeToDevice(deviceId)
      commit('ADD_SUBSCRIPTION', deviceId)
    }
  },
  
  unsubscribeFromDevice({ commit, state }, deviceId) {
    if (state.subscriptions.has(deviceId)) {
      signalRService.unsubscribeFromDevice(deviceId)
      commit('REMOVE_SUBSCRIPTION', deviceId)
    }
  },
  
  sendHeartbeat({ commit }) {
    signalRService.sendHeartbeat().then(() => {
      commit('SET_LAST_HEARTBEAT', Date.now())
    })
  }
}

export default {
  namespaced: true,
  state,
  getters,
  mutations,
  actions
}