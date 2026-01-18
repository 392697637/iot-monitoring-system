import axios from 'axios'
import { ElMessage, ElLoading } from 'element-plus'

class ApiService {
  constructor() {
    this.instance = null
    this.config = null
    this.loadingInstance = null
    this.requestCount = 0
  }

  /**
   * 初始化 API 服务
   */
  async init(appConfig) {
    this.config = appConfig
    
    // 创建 Axios 实例
    this.instance = axios.create({
      baseURL: this.config.API_BASE_URL,
      timeout: this.config.TIMEOUT,
      headers: {
        'Content-Type': 'application/json',
        'X-Requested-With': 'XMLHttpRequest'
      }
    })

    // 设置请求拦截器
    this.setupInterceptors()
    
    console.log('[API] Service initialized')
  }

  /**
   * 设置拦截器
   */
  setupInterceptors() {
    // 请求拦截器
    this.instance.interceptors.request.use(
      config => {
        // 显示加载动画
        this.showLoading()
        
        // 添加 token
        const token = this.getToken()
        if (token) {
          config.headers.Authorization = `Bearer ${token}`
        }
        
        // 调试模式输出日志
        if (this.config.ENABLE_DEBUG) {
          console.log(`[API Request] ${config.method.toUpperCase()} ${config.url}`, {
            params: config.params,
            data: config.data
          })
        }
        
        return config
      },
      error => {
        this.hideLoading()
        console.error('[API Request Error]', error)
        return Promise.reject(error)
      }
    )

    // 响应拦截器
    this.instance.interceptors.response.use(
      response => {
        // 隐藏加载动画
        this.hideLoading()
        
        // 调试模式输出日志
        if (this.config.ENABLE_DEBUG) {
          console.log(`[API Response] ${response.config.url}`, response.data)
        }
        
        // 处理响应数据
        return this.handleResponse(response)
      },
      error => {
        // 隐藏加载动画
        this.hideLoading()
        
        // 处理错误
        return this.handleError(error)
      }
    )
  }

  /**
   * 处理响应
   */
  handleResponse(response) {
    const { data } = response
    
    // 根据后端返回的数据结构进行调整
    // 假设后端返回 { code, message, data }
    if (data && typeof data === 'object') {
      if (data.code !== undefined && data.code !== 0) {
        // 业务逻辑错误
        ElMessage({
          message: data.message || '请求失败',
          type: 'error',
          duration: 3000
        })
        return Promise.reject(data)
      }
      
      // 返回实际数据
      return data.data !== undefined ? data.data : data
    }
    
    return response
  }

  /**
   * 处理错误
   */
  handleError(error) {
    if (this.config.ENABLE_DEBUG) {
      console.error('[API Error]', error)
    }
    
    let message = '请求失败'
    
    if (error.response) {
      // 服务器返回了错误状态码
      const { status, data } = error.response
      
      switch (status) {
        case 400:
          message = data.message || '请求参数错误'
          break
        case 401:
          message = '未授权，请重新登录'
          this.handleUnauthorized()
          break
        case 403:
          message = '拒绝访问'
          break
        case 404:
          message = '请求的资源不存在'
          break
        case 500:
          message = '服务器内部错误'
          break
        case 502:
          message = '网关错误'
          break
        case 503:
          message = '服务不可用'
          break
        case 504:
          message = '网关超时'
          break
        default:
          message = `请求错误: ${status}`
      }
    } else if (error.request) {
      // 请求发送但无响应
      message = '网络错误，请检查网络连接'
    } else {
      // 请求配置错误
      message = error.message || '请求配置错误'
    }
    
    // 显示错误消息
    ElMessage({
      message,
      type: 'error',
      duration: 5000
    })
    
    return Promise.reject(error)
  }

  /**
   * 显示加载动画
   */
  showLoading() {
    this.requestCount++
    if (this.requestCount === 1) {
      this.loadingInstance = ElLoading.service({
        lock: true,
        text: '加载中...',
        background: 'rgba(0, 0, 0, 0.7)'
      })
    }
  }

  /**
   * 隐藏加载动画
   */
  hideLoading() {
    this.requestCount--
    if (this.requestCount <= 0) {
      this.requestCount = 0
      if (this.loadingInstance) {
        this.loadingInstance.close()
        this.loadingInstance = null
      }
    }
  }

  /**
   * 获取 token
   */
  getToken() {
    return localStorage.getItem('token') || sessionStorage.getItem('token')
  }

  /**
   * 处理未授权
   */
  handleUnauthorized() {
    // 清除认证信息
    localStorage.removeItem('token')
    sessionStorage.removeItem('token')
    
    // 跳转到登录页
    setTimeout(() => {
      window.location.href = '/login'
    }, 1500)
  }

  /**
   * HTTP 方法封装
   */
  get(url, params = {}, config = {}) {
    return this.instance.get(url, { ...params, ...config })
  }

  post(url, data = {}, config = {}) {
    return this.instance.post(url, data, config)
  }

  put(url, data = {}, config = {}) {
    return this.instance.put(url, data, config)
  }

  delete(url, config = {}) {
    return this.instance.delete(url, config)
  }

  patch(url, data = {}, config = {}) {
    return this.instance.patch(url, data, config)
  }

  /**
   * 上传文件
   */
  upload(url, file, data = {}, onProgress = null) {
    const formData = new FormData()
    formData.append('file', file)
    
    // 添加额外数据
    Object.keys(data).forEach(key => {
      formData.append(key, data[key])
    })
    
    return this.instance.post(url, formData, {
      headers: {
        'Content-Type': 'multipart/form-data'
      },
      onUploadProgress: onProgress
    })
  }

  /**
   * 下载文件
   */
  download(url, params = {}, filename = 'download') {
    return this.instance.get(url, {
      params,
      responseType: 'blob'
    }).then(response => {
      const blob = new Blob([response.data])
      const downloadUrl = window.URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = downloadUrl
      link.download = filename
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      window.URL.revokeObjectURL(downloadUrl)
    })
  }
}

// 创建单例实例
const apiService = new ApiService()
export default apiService