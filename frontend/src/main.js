import Vue from 'vue'
import App from './App.vue'
import router from './router'
import store from './store'
import ElementUI from 'element-ui'
import 'element-ui/lib/theme-chalk/index.css'
import VueECharts from 'vue-echarts'
import { use } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'
import {
  LineChart,
  BarChart,
  PieChart,
  GaugeChart,
  ScatterChart,
  HeatmapChart
} from 'echarts/charts'
import {
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DatasetComponent,
  DataZoomComponent,
  VisualMapComponent
} from 'echarts/components'
import VueMeta from 'vue-meta'
import VueI18n from 'vue-i18n'
import './assets/styles/global.css'
// import './assets/styles/element-variables.scss'

// 配置ECharts
use([
  CanvasRenderer,
  LineChart,
  BarChart,
  PieChart,
  GaugeChart,
  ScatterChart,
  HeatmapChart,
  TitleComponent,
  TooltipComponent,
  LegendComponent,
  GridComponent,
  DatasetComponent,
  DataZoomComponent,
  VisualMapComponent
])

// 配置Vue插件
Vue.config.productionTip = false
Vue.use(ElementUI)
Vue.use(VueMeta)
Vue.use(VueI18n)
Vue.component('v-chart', VueECharts)

// 配置全局过滤器
Vue.filter('formatTime', (value) => {
  if (!value) return ''
  return new Date(value).toLocaleString()
})

Vue.filter('formatNumber', (value, decimals = 2) => {
  if (value === null || value === undefined) return '--'
  return Number(value).toFixed(decimals)
})

Vue.filter('formatStatus', (status) => {
  const statusMap = {
    'Normal': '正常',
    'Warning': '警告',
    'Fault': '故障',
    'Offline': '离线'
  }
  return statusMap[status] || status
})

// 配置全局组件
Vue.component('loading', {
  template: `
    <div class="loading-container">
      <div class="loading-spinner">
        <div class="spinner"></div>
      </div>
      <div class="loading-text">{{ text }}</div>
    </div>
  `,
  props: {
    text: {
      type: String,
      default: '加载中...'
    }
  }
})

Vue.component('empty-state', {
  template: `
    <div class="empty-state">
      <div class="empty-icon">
        <slot name="icon">
          <i class="el-icon-files"></i>
        </slot>
      </div>
      <div class="empty-text">
        <slot>{{ message }}</slot>
      </div>
      <div class="empty-action" v-if="$slots.action">
        <slot name="action"></slot>
      </div>
    </div>
  `,
  props: {
    message: {
      type: String,
      default: '暂无数据'
    }
  }
})

// 配置i18n
const i18n = new VueI18n({
  locale: 'zh-CN',
  messages: {
    'zh-CN': require('./locales/zh-CN.json'),
    'en-US': require('./locales/en-US.json')
  }
})

// 配置Vue实例
new Vue({
  router,
  store,
  i18n,
  render: h => h(App),
  mounted() {
    // 页面加载完成
    console.log(`IoT监测系统 v${process.env.VUE_APP_VERSION} 已启动`)
  }
}).$mount('#app')