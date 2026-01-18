/**
 * 配置管理类
 */
class ConfigManager {
  constructor() {
    this.config = null
    this.isLoaded = false
  }

  /**
   * 加载配置文件
   */
  async load() {
    // 避免重复加载
    if (this.isLoaded && this.config) {
      return this.config
    }

    try {
      // 从 public 目录加载配置文件
      const response = await fetch('/config.json?t=' + Date.now())
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`)
      }
      
      this.config = await response.json()
      this.isLoaded = true
      
      console.log('[Config] Configuration loaded successfully')
      return this.config
    } catch (error) {
      console.error('[Config] Failed to load configuration:', error)
      
      // 返回默认配置
      return this.getDefaultConfig()
    }
  }

  /**
   * 获取默认配置（当配置文件加载失败时使用）
   */
  getDefaultConfig() {
    this.config = {
      API_BASE_URL: 'https://localhost:8011/api',
      TIMEOUT: 10000,
      ENABLE_DEBUG: import.meta.env.MODE === 'development',
      APP_NAME: 'Vue 3 App',
      VERSION: '1.0.0'
    }
    
    console.warn('[Config] Using default configuration')
    return this.config
  }

  /**
   * 获取配置值
   */
  get(key, defaultValue = null) {
    if (!this.config) {
      console.warn('[Config] Configuration not loaded yet')
      return defaultValue
    }
    
    const value = this.config[key]
    return value !== undefined ? value : defaultValue
  }

  /**
   * 获取所有配置
   */
  getAll() {
    return this.config || this.getDefaultConfig()
  }
}

// 创建单例实例
const configManager = new ConfigManager()
export default configManager