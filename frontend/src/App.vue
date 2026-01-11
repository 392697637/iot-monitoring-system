<template>
  <div id="app" :class="{'dark-mode': isDarkMode}">
    <!-- 导航栏 -->
    <el-header class="app-header">
      <div class="header-left">
        <div class="logo">
          <img src="@/assets/logo.png" alt="IoT监测系统">
          <span class="logo-text">IoT监测系统</span>
        </div>
        <div class="header-version">v{{ appVersion }}</div>
      </div>
      
      <div class="header-center">
        <el-menu
          :default-active="activeMenu"
          mode="horizontal"
          @select="handleMenuSelect"
          background-color="transparent"
          text-color="#fff"
          active-text-color="#ffd04b"
          class="main-menu"
        >
          <el-menu-item index="/dashboard">
            <i class="el-icon-monitor"></i>
            实时监控
          </el-menu-item>
          <el-menu-item index="/history">
            <i class="el-icon-timer"></i>
            历史数据
          </el-menu-item>
          <el-menu-item index="/alarms">
            <i class="el-icon-warning"></i>
            报警管理
            <el-badge 
              v-if="unacknowledgedAlarms > 0" 
              :value="unacknowledgedAlarms" 
              class="badge"
            />
          </el-menu-item>
          <el-menu-item index="/thresholds">
            <i class="el-icon-setting"></i>
            阈值设置
          </el-menu-item>
          <el-menu-item index="/devices">
            <i class="el-icon-cpu"></i>
            设备管理
          </el-menu-item>
          <el-submenu index="/more" v-if="showMoreMenu">
            <template slot="title">
              <i class="el-icon-more"></i>
              更多
            </template>
            <el-menu-item index="/statistics">
              <i class="el-icon-data-analysis"></i>
              统计分析
            </el-menu-item>
            <el-menu-item index="/reports">
              <i class="el-icon-document"></i>
              报表导出
            </el-menu-item>
            <el-menu-item index="/system">
              <i class="el-icon-s-tools"></i>
              系统设置
            </el-menu-item>
          </el-submenu>
        </el-menu>
      </div>
      
      <div class="header-right">
        <!-- 实时状态 -->
        <div class="status-indicator" :class="connectionStatusClass">
          <i class="el-icon-connection"></i>
          <span>{{ connectionStatusText }}</span>
        </div>
        
        <!-- 报警音控制 -->
        <el-tooltip content="报警音控制" placement="bottom">
          <el-button
            type="text"
            class="alarm-sound-control"
            @click="toggleAlarmSound"
          >
            <i :class="alarmSoundIcon"></i>
          </el-button>
        </el-tooltip>
        
        <!-- 主题切换 -->
        <el-tooltip :content="isDarkMode ? '切换至亮色主题' : '切换至暗色主题'" placement="bottom">
          <el-button
            type="text"
            class="theme-toggle"
            @click="toggleTheme"
          >
            <i :class="themeIcon"></i>
          </el-button>
        </el-tooltip>
        
        <!-- 用户菜单 -->
        <el-dropdown @command="handleUserCommand">
          <div class="user-info">
            <el-avatar size="small" :src="userAvatar"></el-avatar>
            <span class="user-name">{{ userName }}</span>
            <i class="el-icon-arrow-down"></i>
          </div>
          <el-dropdown-menu slot="dropdown">
            <el-dropdown-item command="profile">
              <i class="el-icon-user"></i>
              个人中心
            </el-dropdown-item>
            <el-dropdown-item command="notifications">
              <i class="el-icon-bell"></i>
              消息通知
              <el-badge 
                v-if="unreadNotifications > 0" 
                :value="unreadNotifications" 
                class="dropdown-badge"
              />
            </el-dropdown-item>
            <el-dropdown-item divided command="settings">
              <i class="el-icon-setting"></i>
              系统设置
            </el-dropdown-item>
            <el-dropdown-item command="about">
              <i class="el-icon-info"></i>
              关于系统
            </el-dropdown-item>
            <el-dropdown-item divided command="logout">
              <i class="el-icon-switch-button"></i>
              退出登录
            </el-dropdown-item>
          </el-dropdown-menu>
        </el-dropdown>
      </div>
    </el-header>
    
    <!-- 主内容区 -->
    <el-main class="app-main">
      <transition name="fade" mode="out-in">
        <router-view class="router-view" />
      </transition>
    </el-main>
    
    <!-- 全局报警音 -->
    <audio ref="globalAlarmAudio" src="/alarm.mp3" preload="auto"></audio>
    
    <!-- 全局报警通知 -->
    <global-alarm-notification />
    
    <!-- 系统通知 -->
    <system-notification />
    
    <!-- 连接状态提示 -->
    <connection-status-modal />
    
    <!-- 全局加载遮罩 -->
    <el-dialog
      :visible.sync="globalLoading"
      :modal="true"
      :show-close="false"
      :close-on-click-modal="false"
      :close-on-press-escape="false"
      width="300px"
      custom-class="loading-dialog"
    >
      <div class="loading-content">
        <div class="loading-spinner">
          <div class="spinner"></div>
        </div>
        <div class="loading-text">{{ loadingText }}</div>
      </div>
    </el-dialog>
    
    <!-- 版权信息 -->
    <el-footer class="app-footer">
      <div class="footer-content">
        <div class="copyright">
          © 2023 IoT监测系统 v{{ appVersion }} - 版权所有
        </div>
        <div class="footer-links">
          <el-link type="info" :underline="false" @click="showAbout">关于我们</el-link>
          <el-divider direction="vertical"></el-divider>
          <el-link type="info" :underline="false" @click="showHelp">帮助文档</el-link>
          <el-divider direction="vertical"></el-divider>
          <el-link type="info" :underline="false" @click="showFeedback">意见反馈</el-link>
        </div>
      </div>
    </el-footer>
  </div>
</template>

<script>
import { mapState, mapGetters, mapActions } from 'vuex'
import GlobalAlarmNotification from '@/components/alarms/GlobalAlarmNotification.vue'
import SystemNotification from '@/components/common/SystemNotification.vue'
import ConnectionStatusModal from '@/components/common/ConnectionStatusModal.vue'

export default {
  name: 'App',
  components: {
    GlobalAlarmNotification,
    SystemNotification,
    ConnectionStatusModal
  },
  data() {
    return {
      isDarkMode: false,
      globalLoading: false,
      loadingText: '加载中...',
      unreadNotifications: 0,
      userAvatar: require('@/assets/images/avatar-default.png'),
      userName: '管理员'
    }
  },
  computed: {
    ...mapState(['connectionStatus', 'unacknowledgedAlarms', 'isAlarmMuted']),
    ...mapGetters(['isConnected', 'hasActiveAlarms']),
    
    appVersion() {
      return process.env.VUE_APP_VERSION
    },
    
    activeMenu() {
      return this.$route.path
    },
    
    showMoreMenu() {
      return this.$route.meta.showMoreMenu !== false
    },
    
    connectionStatusClass() {
      return {
        'status-connected': this.connectionStatus === 'connected',
        'status-connecting': this.connectionStatus === 'connecting',
        'status-disconnected': this.connectionStatus === 'disconnected'
      }
    },
    
    connectionStatusText() {
      const statusMap = {
        'connected': '已连接',
        'connecting': '连接中...',
        'disconnected': '未连接'
      }
      return statusMap[this.connectionStatus] || '未知'
    },
    
    alarmSoundIcon() {
      return this.isAlarmMuted ? 'el-icon-close-notification' : 'el-icon-bell'
    },
    
    themeIcon() {
      return this.isDarkMode ? 'el-icon-sunny' : 'el-icon-moon'
    }
  },
  created() {
    // 初始化主题
    this.initTheme()
    
    // 初始化连接
    this.initConnection()
    
    // 监听路由变化
    this.$router.beforeEach((to, from, next) => {
      // 显示加载状态
      this.showLoading()
      next()
    })
    
    this.$router.afterEach(() => {
      // 隐藏加载状态
      setTimeout(() => {
        this.hideLoading()
      }, 300)
    })
    
    // 监听全局事件
    this.$eventBus.$on('showLoading', this.showLoading)
    this.$eventBus.$on('hideLoading', this.hideLoading)
    this.$eventBus.$on('systemNotification', this.handleSystemNotification)
  },
  beforeDestroy() {
    this.$eventBus.$off('showLoading', this.showLoading)
    this.$eventBus.$off('hideLoading', this.hideLoading)
    this.$eventBus.$off('systemNotification', this.handleSystemNotification)
  },
  methods: {
    ...mapActions([
      'initializeConnection',
      'toggleAlarmMute',
      'connectSignalR',
      'disconnectSignalR'
    ]),
    
    initTheme() {
      const savedTheme = localStorage.getItem('theme')
      if (savedTheme === 'dark') {
        this.isDarkMode = true
        document.documentElement.setAttribute('data-theme', 'dark')
      }
    },
    
    async initConnection() {
      try {
        await this.initializeConnection()
        await this.connectSignalR()
      } catch (error) {
        console.error('初始化连接失败:', error)
      }
    },
    
    handleMenuSelect(index) {
      if (index.startsWith('/')) {
        this.$router.push(index)
      }
    },
    
    toggleAlarmSound() {
      this.toggleAlarmMute()
      const message = this.isAlarmMuted ? '报警音已开启' : '报警音已关闭'
      this.$message({
        message,
        type: 'success',
        duration: 1500
      })
    },
    
    toggleTheme() {
      this.isDarkMode = !this.isDarkMode
      if (this.isDarkMode) {
        document.documentElement.setAttribute('data-theme', 'dark')
        localStorage.setItem('theme', 'dark')
      } else {
        document.documentElement.removeAttribute('data-theme')
        localStorage.setItem('theme', 'light')
      }
    },
    
    handleUserCommand(command) {
      switch (command) {
        case 'profile':
          this.$router.push('/profile')
          break
        case 'notifications':
          this.$router.push('/notifications')
          break
        case 'settings':
          this.$router.push('/settings')
          break
        case 'about':
          this.showAbout()
          break
        case 'logout':
          this.handleLogout()
          break
      }
    },
    
    showAbout() {
      this.$alert(
        `
        <div class="about-content">
          <h3>IoT监测系统</h3>
          <p>版本: v${this.appVersion}</p>
          <p>描述: 物联网设备实时监测与管理系统</p>
          <p>技术支持: support@iotmonitoring.com</p>
          <p>© 2023 IoT监测系统 版权所有</p>
        </div>
        `,
        '关于系统',
        {
          dangerouslyUseHTMLString: true,
          customClass: 'about-dialog'
        }
      )
    },
    
    showHelp() {
      window.open('https://docs.iotmonitoring.com', '_blank')
    },
    
    showFeedback() {
      this.$prompt('请输入您的反馈意见', '意见反馈', {
        inputType: 'textarea',
        inputPlaceholder: '请详细描述您的问题或建议...',
        inputValidator: (value) => {
          if (!value) {
            return '反馈内容不能为空'
          }
          if (value.length < 10) {
            return '反馈内容至少10个字符'
          }
          return true
        }
      }).then(({ value }) => {
        this.submitFeedback(value)
      }).catch(() => {})
    },
    
    async submitFeedback(content) {
      try {
        // 这里调用API提交反馈
        this.$message.success('感谢您的反馈！')
      } catch (error) {
        this.$message.error('提交反馈失败')
      }
    },
    
    async handleLogout() {
      try {
        await this.$confirm('确定要退出登录吗？', '提示', {
          confirmButtonText: '确定',
          cancelButtonText: '取消',
          type: 'warning'
        })
        
        // 断开SignalR连接
        await this.disconnectSignalR()
        
        // 清除用户数据
        localStorage.removeItem('token')
        localStorage.removeItem('userInfo')
        
        // 跳转到登录页
        this.$router.push('/login')
        
        this.$message.success('已成功退出登录')
      } catch (error) {
        if (error !== 'cancel') {
          this.$message.error('退出登录失败')
        }
      }
    },
    
    showLoading(text = '加载中...') {
      this.loadingText = text
      this.globalLoading = true
    },
    
    hideLoading() {
      this.globalLoading = false
    },
    
    handleSystemNotification(notification) {
      const { type, title, message } = notification
      
      switch (type) {
        case 'success':
          this.$notify.success({ title, message })
          break
        case 'warning':
          this.$notify.warning({ title, message })
          break
        case 'error':
          this.$notify.error({ title, message })
          break
        default:
          this.$notify.info({ title, message })
      }
    }
  }
}
</script>

<style lang="scss" scoped>
#app {
  height: 100vh;
  display: flex;
  flex-direction: column;
  background: var(--bg-color-primary);
  color: var(--text-color-primary);
  transition: background-color 0.3s, color 0.3s;
  
  &.dark-mode {
    --bg-color-primary: #1a1a1a;
    --bg-color-secondary: #2d2d2d;
    --text-color-primary: #ffffff;
    --text-color-secondary: #a0a0a0;
    --border-color: #404040;
  }
}

.app-header {
  height: 60px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
  background: linear-gradient(135deg, var(--primary-color) 0%, var(--secondary-color) 100%);
  box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
  z-index: 1000;
  
  .header-left {
    display: flex;
    align-items: center;
    
    .logo {
      display: flex;
      align-items: center;
      cursor: pointer;
      
      img {
        width: 32px;
        height: 32px;
        margin-right: 10px;
      }
      
      .logo-text {
        font-size: 18px;
        font-weight: bold;
        color: white;
      }
    }
    
    .header-version {
      margin-left: 10px;
      padding: 2px 8px;
      background: rgba(255, 255, 255, 0.2);
      border-radius: 4px;
      font-size: 12px;
      color: white;
    }
  }
  
  .header-center {
    flex: 1;
    display: flex;
    justify-content: center;
    
    .main-menu {
      border-bottom: none;
      
      .el-menu-item {
        height: 60px;
        line-height: 60px;
        margin: 0 5px;
        
        &:hover {
          background-color: rgba(255, 255, 255, 0.1) !important;
        }
        
        &.is-active {
          border-bottom: 2px solid #ffd04b;
        }
        
        .badge {
          ::v-deep .el-badge__content {
            top: 12px;
            right: 5px;
          }
        }
      }
    }
  }
  
  .header-right {
    display: flex;
    align-items: center;
    gap: 15px;
    
    .status-indicator {
      display: flex;
      align-items: center;
      gap: 5px;
      padding: 5px 10px;
      border-radius: 4px;
      font-size: 14px;
      color: white;
      
      &.status-connected {
        background: rgba(76, 175, 80, 0.2);
        i { color: #4CAF50; }
      }
      
      &.status-connecting {
        background: rgba(255, 152, 0, 0.2);
        i { color: #FF9800; }
      }
      
      &.status-disconnected {
        background: rgba(244, 67, 54, 0.2);
        i { color: #F44336; }
      }
    }
    
    .alarm-sound-control,
    .theme-toggle {
      color: white;
      font-size: 18px;
      
      &:hover {
        color: #ffd04b;
      }
    }
    
    .user-info {
      display: flex;
      align-items: center;
      gap: 8px;
      padding: 5px 10px;
      border-radius: 4px;
      cursor: pointer;
      transition: background-color 0.3s;
      
      &:hover {
        background: rgba(255, 255, 255, 0.1);
      }
      
      .user-name {
        color: white;
        font-size: 14px;
      }
      
      .el-icon-arrow-down {
        color: white;
        font-size: 12px;
      }
    }
  }
}

.app-main {
  flex: 1;
  padding: 20px;
  overflow: auto;
  background: var(--bg-color-secondary);
  
  .router-view {
    min-height: 100%;
    animation: fadeIn 0.3s;
  }
}

.app-footer {
  height: 40px;
  padding: 0 20px;
  background: var(--bg-color-primary);
  border-top: 1px solid var(--border-color);
  
  .footer-content {
    height: 100%;
    display: flex;
    align-items: center;
    justify-content: space-between;
    color: var(--text-color-secondary);
    font-size: 12px;
    
    .footer-links {
      display: flex;
      align-items: center;
      gap: 5px;
      
      .el-divider {
        background-color: var(--text-color-secondary);
        margin: 0 5px;
      }
    }
  }
}

.loading-dialog {
  ::v-deep .el-dialog__body {
    padding: 40px 20px;
  }
  
  .loading-content {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 20px;
    
    .loading-spinner {
      .spinner {
        width: 40px;
        height: 40px;
        border: 3px solid #f3f3f3;
        border-top: 3px solid var(--primary-color);
        border-radius: 50%;
        animation: spin 1s linear infinite;
      }
    }
    
    .loading-text {
      color: var(--text-color-secondary);
      font-size: 14px;
    }
  }
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateY(10px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

@keyframes spin {
  0% { transform: rotate(0deg); }
  100% { transform: rotate(360deg); }
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s;
}

.fade-enter,
.fade-leave-to {
  opacity: 0;
}

@media (max-width: 768px) {
  .app-header {
    padding: 0 10px;
    
    .logo-text {
      display: none;
    }
    
    .header-center {
      .main-menu {
        .el-menu-item {
          padding: 0 8px;
          font-size: 12px;
          
          i {
            margin-right: 3px;
          }
          
          span:not(.el-badge) {
            display: none;
          }
        }
      }
    }
    
    .header-right {
      .status-indicator {
        span {
          display: none;
        }
      }
      
      .user-info {
        .user-name {
          display: none;
        }
      }
    }
  }
  
  .app-main {
    padding: 10px;
  }
}
</style>