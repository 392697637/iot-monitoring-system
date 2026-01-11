import Vue from 'vue'
import VueRouter from 'vue-router'

Vue.use(VueRouter)

// 路由懒加载
const Dashboard = () => import(/* webpackChunkName: "dashboard" */ '@/views/Dashboard.vue')
const HistoryData = () => import(/* webpackChunkName: "history" */ '@/views/HistoryData.vue')
const Alarms = () => import(/* webpackChunkName: "alarms" */ '@/views/Alarms.vue')
const Thresholds = () => import(/* webpackChunkName: "thresholds" */ '@/views/Thresholds.vue')
const Devices = () => import(/* webpackChunkName: "devices" */ '@/views/Devices.vue')
const Statistics = () => import(/* webpackChunkName: "statistics" */ '@/views/Statistics.vue')
const Reports = () => import(/* webpackChunkName: "reports" */ '@/views/Reports.vue')
const SystemSettings = () => import(/* webpackChunkName: "settings" */ '@/views/SystemSettings.vue')
const Login = () => import(/* webpackChunkName: "login" */ '@/views/Login.vue')
const Profile = () => import(/* webpackChunkName: "profile" */ '@/views/Profile.vue')
const Notifications = () => import(/* webpackChunkName: "notifications" */ '@/views/Notifications.vue')
const NotFound = () => import(/* webpackChunkName: "notfound" */ '@/views/NotFound.vue')

const routes = [
  {
    path: '/',
    redirect: '/dashboard'
  },
  {
    path: '/login',
    name: 'Login',
    component: Login,
    meta: {
      title: '登录',
      requiresAuth: false,
      hideHeader: true,
      hideFooter: true
    }
  },
  {
    path: '/dashboard',
    name: 'Dashboard',
    component: Dashboard,
    meta: {
      title: '实时监控',
      requiresAuth: true,
      keepAlive: true,
      icon: 'el-icon-monitor'
    }
  },
  {
    path: '/history',
    name: 'HistoryData',
    component: HistoryData,
    meta: {
      title: '历史数据',
      requiresAuth: true,
      keepAlive: true,
      icon: 'el-icon-timer'
    }
  },
  {
    path: '/alarms',
    name: 'Alarms',
    component: Alarms,
    meta: {
      title: '报警管理',
      requiresAuth: true,
      keepAlive: false,
      icon: 'el-icon-warning'
    }
  },
  {
    path: '/thresholds',
    name: 'Thresholds',
    component: Thresholds,
    meta: {
      title: '阈值设置',
      requiresAuth: true,
      keepAlive: false,
      icon: 'el-icon-setting'
    }
  },
  {
    path: '/devices',
    name: 'Devices',
    component: Devices,
    meta: {
      title: '设备管理',
      requiresAuth: true,
      keepAlive: true,
      icon: 'el-icon-cpu'
    }
  },
  {
    path: '/statistics',
    name: 'Statistics',
    component: Statistics,
    meta: {
      title: '统计分析',
      requiresAuth: true,
      keepAlive: false,
      icon: 'el-icon-data-analysis'
    }
  },
  {
    path: '/reports',
    name: 'Reports',
    component: Reports,
    meta: {
      title: '报表导出',
      requiresAuth: true,
      keepAlive: false,
      icon: 'el-icon-document'
    }
  },
  {
    path: '/system',
    name: 'SystemSettings',
    component: SystemSettings,
    meta: {
      title: '系统设置',
      requiresAuth: true,
      keepAlive: false,
      icon: 'el-icon-s-tools'
    }
  },
  {
    path: '/profile',
    name: 'Profile',
    component: Profile,
    meta: {
      title: '个人中心',
      requiresAuth: true,
      keepAlive: false
    }
  },
  {
    path: '/notifications',
    name: 'Notifications',
    component: Notifications,
    meta: {
      title: '消息通知',
      requiresAuth: true,
      keepAlive: false
    }
  },
  {
    path: '/404',
    name: 'NotFound',
    component: NotFound,
    meta: {
      title: '页面不存在',
      requiresAuth: false
    }
  },
  {
    path: '*',
    redirect: '/404'
  }
]

const router = new VueRouter({
  mode: 'history',
  base: process.env.BASE_URL,
  routes,
  scrollBehavior(to, from, savedPosition) {
    if (savedPosition) {
      return savedPosition
    } else {
      return { x: 0, y: 0 }
    }
  }
})

// 全局前置守卫
router.beforeEach((to, from, next) => {
  // 设置页面标题
  const title = to.meta.title || 'IoT监测系统'
  document.title = `${title} - IoT监测系统`
  
  // 检查登录状态
  const token = localStorage.getItem('token')
  if (to.meta.requiresAuth && !token) {
    next('/login')
    return
  }
  
  // 如果已登录且访问登录页，重定向到首页
  if (to.path === '/login' && token) {
    next('/dashboard')
    return
  }
  
  next()
})

// 全局后置钩子
router.afterEach((to) => {
  // 在这里可以添加页面访问统计等逻辑
  console.log(`路由跳转: ${to.fullPath}`)
})

export default router