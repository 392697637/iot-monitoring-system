import axios from 'axios'
import { Message } from 'element-ui'
import store from '@/store'
import router from '@/router'

// 创建axios实例
const service = axios.create({
  baseURL: process.env.VUE_APP_API_BASE_URL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json'
  }
})

// 请求拦截器
service.interceptors.request.use(
  config => {
    // 添加token
    const token = localStorage.getItem('token')
    if (token) {
      config.headers.Authorization = `Bearer ${token}`
    }
    
    // 添加请求时间戳
    config.headers['X-Request-Timestamp'] = Date.now()
    
    // 显示加载状态
    if (config.showLoading !== false) {
      store.dispatch('showLoading')
    }
    
    return config
  },
  error => {
    console.error('请求配置错误:', error)
    return Promise.reject(error)
  }
)

// 响应拦截器
service.interceptors.response.use(
  response => {
    // 隐藏加载状态
    if (response.config.showLoading !== false) {
      store.dispatch('hideLoading')
    }
    
    // 处理文件下载
    if (response.config.responseType === 'blob') {
      return response
    }
    
    // 处理API响应格式
    const res = response.data
    
    if (res.success === false) {
      // 业务逻辑错误
      Message.error(res.message || '请求失败')
      return Promise.reject(new Error(res.message || 'Error'))
    }
    
    return res
  },
  error => {
    // 隐藏加载状态
    if (error.config && error.config.showLoading !== false) {
      store.dispatch('hideLoading')
    }
    
    if (error.response) {
      const { status, data } = error.response
      
      switch (status) {
        case 401:
          // 未授权，跳转到登录页
          Message.error('登录已过期，请重新登录')
          localStorage.removeItem('token')
          router.push('/login')
          break
          
        case 403:
          Message.error('没有权限访问该资源')
          break
          
        case 404:
          Message.error('请求的资源不存在')
          break
          
        case 500:
          Message.error(data?.message || '服务器内部错误')
          break
          
        default:
          Message.error(data?.message || `请求失败 (${status})`)
      }
    } else if (error.request) {
      // 请求已发送但没有收到响应
      Message.error('网络错误，请检查网络连接')
    } else {
      // 请求配置错误
      Message.error('请求配置错误')
    }
    
    return Promise.reject(error)
  }
)

// API方法
const api = {
  // 系统健康检查
  getHealth() {
    return service.get('/health')
  },
  
  // 设备管理
  getDevices(params) {
    return service.get('/api/devices', { params })
  },
  
  getDevice(id) {
    return service.get(`/api/devices/${id}`)
  },
  
  createDevice(data) {
    return service.post('/api/devices', data)
  },
  
  updateDevice(id, data) {
    return service.put(`/api/devices/${id}`, data)
  },
  
  deleteDevice(id) {
    return service.delete(`/api/devices/${id}`)
  },
  
  getDeviceStatusSummary() {
    return service.get('/api/devices/status-summary')
  },
  
  // 设备数据
  getRealTimeData(deviceId, minutes = 30) {
    return service.get(`/api/devicedata/realtime/${deviceId}`, {
      params: { minutes }
    })
  },
  
  getLatestData(deviceId) {
    return service.get(`/api/devicedata/latest/${deviceId}`)
  },
  
  getHistoricalData(params) {
    return service.get('/api/devicedata/history', { params })
  },
  
  exportDeviceData(deviceId, startTime, endTime) {
    return service.get(`/api/devicedata/export/${deviceId}`, {
      params: { startTime, endTime },
      responseType: 'blob',
      showLoading: false
    })
  },
  
  getDataStatistics(deviceId, date) {
    return service.get(`/api/devicedata/statistics/${deviceId}`, {
      params: { date }
    })
  },
  
  reportDeviceData(data) {
    return service.post('/api/devicedata/report', data)
  },
  
  // 阈值管理
  getThresholds(params) {
    return service.get('/api/thresholds', { params })
  },
  
  getDeviceThresholds(deviceId) {
    return service.get(`/api/thresholds/device/${deviceId}`)
  },
  
  getThreshold(id) {
    return service.get(`/api/thresholds/${id}`)
  },
  
  createThreshold(data) {
    return service.post('/api/thresholds', data)
  },
  
  updateThreshold(id, data) {
    return service.put(`/api/thresholds/${id}`, data)
  },
  
  batchUpdateThresholds(data) {
    return service.put('/api/thresholds/batch', data)
  },
  
  deleteThreshold(id) {
    return service.delete(`/api/thresholds/${id}`)
  },
  
  validateThreshold(data) {
    return service.post('/api/thresholds/validate', data)
  },
  
  // 报警管理
  getAlarms(params) {
    return service.get('/api/alarms', { params })
  },
  
  getUnacknowledgedAlarms(params) {
    return service.get('/api/alarms/unacknowledged', { params })
  },
  
  acknowledgeAlarm(id) {
    return service.post(`/api/alarms/${id}/acknowledge`)
  },
  
  batchAcknowledgeAlarms(ids) {
    return service.post('/api/alarms/batch-acknowledge', ids)
  },
  
  getAlarmStatistics(params) {
    return service.get('/api/alarms/statistics', { params })
  },
  
  cleanupOldAlarms(days = 180) {
    return service.delete('/api/alarms/cleanup', { params: { days } })
  },
  
  // 仪表板
  getDashboardSummary() {
    return service.get('/api/dashboard/summary')
  },
  
  getMonitoringData() {
    return service.get('/api/dashboard/monitoring')
  },
  
  getPerformanceMetrics() {
    return service.get('/api/dashboard/performance')
  },
  
  // 用户管理
  login(credentials) {
    return service.post('/api/auth/login', credentials)
  },
  
  logout() {
    return service.post('/api/auth/logout')
  },
  
  getProfile() {
    return service.get('/api/user/profile')
  },
  
  updateProfile(data) {
    return service.put('/api/user/profile', data)
  },
  
  changePassword(data) {
    return service.post('/api/user/change-password', data)
  },
  
  // 系统管理
  getSystemSettings() {
    return service.get('/api/system/settings')
  },
  
  updateSystemSettings(data) {
    return service.put('/api/system/settings', data)
  },
  
  getSystemLogs(params) {
    return service.get('/api/system/logs', { params })
  },
  
  backupDatabase() {
    return service.post('/api/system/backup')
  },
  
  restoreDatabase(data) {
    return service.post('/api/system/restore', data)
  }
}

export default api