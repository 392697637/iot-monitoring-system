import * as apiService from '@/services/api'

const state = {
  devices: [],
  selectedDeviceId: null,
  deviceData: {},
  realTimeData: {},
  loading: false,
  error: null,
  lastUpdateTime: null
}

const getters = {
  allDevices: state => state.devices,
  
  activeDevices: state => state.devices.filter(device => device.isActive),
  
  selectedDevice: (state) => {
    if (!state.selectedDeviceId) return null
    return state.devices.find(device => device.id === state.selectedDeviceId)
  },
  
  selectedDeviceData: (state) => {
    if (!state.selectedDeviceId) return []
    return state.deviceData[state.selectedDeviceId] || []
  },
  
  selectedDeviceRealtimeData: (state) => {
    if (!state.selectedDeviceId) return {}
    return state.realTimeData[state.selectedDeviceId] || {}
  },
  
  deviceById: (state) => (id) => {
    return state.devices.find(device => device.id === id)
  },
  
  isLoading: state => state.loading,
  
  hasData: state => Object.keys(state.deviceData).length > 0
}

const mutations = {
  SET_DEVICES(state, devices) {
    state.devices = devices
  },
  
  SET_SELECTED_DEVICE_ID(state, deviceId) {
    state.selectedDeviceId = deviceId
  },
  
  SET_DEVICE_DATA(state, { deviceId, data }) {
    state.deviceData[deviceId] = data
  },
  
  SET_REALTIME_DATA(state, { deviceId, data }) {
    state.realTimeData[deviceId] = data
  },
  
  ADD_DEVICE_DATA(state, { deviceId, data }) {
    if (!state.deviceData[deviceId]) {
      state.deviceData[deviceId] = []
    }
    state.deviceData[deviceId].push(data)
    
    // 保持数据量不超过1000条
    if (state.deviceData[deviceId].length > 1000) {
      state.deviceData[deviceId] = state.deviceData[deviceId].slice(-1000)
    }
  },
  
  UPDATE_DEVICE_DATA(state, { deviceId, data }) {
    if (state.realTimeData[deviceId]) {
      Object.assign(state.realTimeData[deviceId], data)
      state.lastUpdateTime = new Date()
    }
  },
  
  SET_LOADING(state, loading) {
    state.loading = loading
  },
  
  SET_ERROR(state, error) {
    state.error = error
  },
  
  ADD_DEVICE(state, device) {
    state.devices.push(device)
  },
  
  UPDATE_DEVICE(state, updatedDevice) {
    const index = state.devices.findIndex(d => d.id === updatedDevice.id)
    if (index !== -1) {
      state.devices.splice(index, 1, updatedDevice)
    }
  },
  
  DELETE_DEVICE(state, deviceId) {
    state.devices = state.devices.filter(device => device.id !== deviceId)
    
    // 清理相关数据
    delete state.deviceData[deviceId]
    delete state.realTimeData[deviceId]
    
    // 如果删除的是当前选中的设备，清空选中
    if (state.selectedDeviceId === deviceId) {
      state.selectedDeviceId = null
    }
  },
  
  CLEAR_DEVICE_DATA(state, deviceId) {
    if (deviceId) {
      delete state.deviceData[deviceId]
      delete state.realTimeData[deviceId]
    } else {
      state.deviceData = {}
      state.realTimeData = {}
    }
  }
}

const actions = {
  async fetchDevices({ commit, state }) {
    try {
      commit('SET_LOADING', true)
      commit('SET_ERROR', null)
      
      const response = await apiService.getDevices()
      if (response.success) {
        commit('SET_DEVICES', response.data)
        
        // 如果没有选中设备且设备列表不为空，默认选择第一个设备
        if (!state.selectedDeviceId && response.data.length > 0) {
          commit('SET_SELECTED_DEVICE_ID', response.data[0].id)
        }
      } else {
        throw new Error(response.message || '获取设备列表失败')
      }
    } catch (error) {
      commit('SET_ERROR', error.message)
      console.error('获取设备列表失败:', error)
    } finally {
      commit('SET_LOADING', false)
    }
  },
  
  async fetchDeviceData({ commit, state }, { deviceId, minutes = 30 }) {
    try {
      commit('SET_LOADING', true)
      commit('SET_ERROR', null)
      
      const response = await apiService.getRealTimeData(deviceId, minutes)
      if (response.success) {
        commit('SET_DEVICE_DATA', {
          deviceId,
          data: response.data
        })
      } else {
        throw new Error(response.message || '获取设备数据失败')
      }
    } catch (error) {
      commit('SET_ERROR', error.message)
      console.error('获取设备数据失败:', error)
    } finally {
      commit('SET_LOADING', false)
    }
  },
  
  async fetchLatestData({ commit }, deviceId) {
    try {
      const response = await apiService.getLatestData(deviceId)
      if (response.success) {
        commit('SET_REALTIME_DATA', {
          deviceId,
          data: response.data
        })
      }
    } catch (error) {
      console.error('获取最新数据失败:', error)
    }
  },
  
  async fetchHistoricalData({ commit }, { deviceId, startTime, endTime, page = 1, pageSize = 50 }) {
    try {
      commit('SET_LOADING', true)
      
      const response = await apiService.getHistoricalData({
        deviceId,
        startTime,
        endTime,
        page,
        pageSize
      })
      
      if (response.success) {
        return response.data
      } else {
        throw new Error(response.message)
      }
    } catch (error) {
      commit('SET_ERROR', error.message)
      console.error('获取历史数据失败:', error)
      throw error
    } finally {
      commit('SET_LOADING', false)
    }
  },
  
  async addDevice({ commit }, deviceData) {
    try {
      commit('SET_LOADING', true)
      
      const response = await apiService.createDevice(deviceData)
      if (response.success) {
        commit('ADD_DEVICE', response.data)
        return response.data
      } else {
        throw new Error(response.message)
      }
    } catch (error) {
      commit('SET_ERROR', error.message)
      console.error('创建设备失败:', error)
      throw error
    } finally {
      commit('SET_LOADING', false)
    }
  },
  
  async updateDevice({ commit }, { id, deviceData }) {
    try {
      commit('SET_LOADING', true)
      
      const response = await apiService.updateDevice(id, deviceData)
      if (response.success) {
        commit('UPDATE_DEVICE', response.data)
        return response.data
      } else {
        throw new Error(response.message)
      }
    } catch (error) {
      commit('SET_ERROR', error.message)
      console.error('更新设备失败:', error)
      throw error
    } finally {
      commit('SET_LOADING', false)
    }
  },
  
  async deleteDevice({ commit }, deviceId) {
    try {
      commit('SET_LOADING', true)
      
      const response = await apiService.deleteDevice(deviceId)
      if (response.success) {
        commit('DELETE_DEVICE', deviceId)
        return true
      } else {
        throw new Error(response.message)
      }
    } catch (error) {
      commit('SET_ERROR', error.message)
      console.error('删除设备失败:', error)
      throw error
    } finally {
      commit('SET_LOADING', false)
    }
  },
  
  async exportDeviceData({ commit }, { deviceId, startTime, endTime }) {
    try {
      commit('SET_LOADING', true)
      
      const response = await apiService.exportDeviceData(deviceId, startTime, endTime)
      return response
    } catch (error) {
      commit('SET_ERROR', error.message)
      console.error('导出数据失败:', error)
      throw error
    } finally {
      commit('SET_LOADING', false)
    }
  },
  
  updateDeviceData({ commit }, data) {
    const deviceId = data.deviceId
    if (deviceId) {
      commit('UPDATE_DEVICE_DATA', { deviceId, data })
    }
  },
  
  selectDevice({ commit, dispatch }, deviceId) {
    commit('SET_SELECTED_DEVICE_ID', deviceId)
    
    // 自动加载设备数据
    dispatch('fetchDeviceData', { deviceId })
    dispatch('fetchLatestData', deviceId)
    
    // 订阅设备实时数据
    dispatch('connection/subscribeToDevice', deviceId, { root: true })
  },
  
  clearDeviceData({ commit }, deviceId) {
    commit('CLEAR_DEVICE_DATA', deviceId)
  }
}

export default {
  namespaced: true,
  state,
  getters,
  mutations,
  actions
}