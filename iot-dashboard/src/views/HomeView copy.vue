<template>
  <el-container style="height:100vh">
    <!-- 顶部 -->
    <el-header style="background: #1f2d3d; color:white; font-size:20px; font-weight:bold;">
      物联网管理系统
    </el-header>

    <!-- 左右布局 -->
    <el-container>
      <!-- 左侧菜单 -->
      <el-aside width="200px" style="background: #2e3c4e; color:white;">
        <el-menu
          v-model="activeMenu"
          default-active="realtime"
          class="el-menu-vertical-demo"
          background-color="#2e3c4e"
          text-color="#bfcbd9"
          active-text-color="#409EFF"
        >
          <el-menu-item index="realtime">实时监控</el-menu-item>
          <el-menu-item index="history">历史数据</el-menu-item>
          <el-menu-item index="devices">设备管理</el-menu-item>
          <el-menu-item index="threshold">阈值设置</el-menu-item>
        </el-menu>
      </el-aside>

      <!-- 主体内容 -->
      <el-main style="padding:20px; background:#f0f2f5;">
        <component 
          :is="currentComponent" 
          :selectedDeviceId="selectedDeviceId" 
          :devices="devices"
        />
      </el-main>
    </el-container>
  </el-container>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'

// 页面组件
import RealtimeMonitor from '@/components/RealtimeMonitor.vue'
import HistoryData from '@/components/HistoryData.vue'
import DevicesManagement from '@/components/DevicesManagement.vue'
import ThresholdSetting from '@/components/ThresholdSetting.vue'

// API
import { getDevices } from '@/api/device'

// 菜单选中
const activeMenu = ref('realtime')

// 设备列表
const devices = ref([])
const selectedDeviceId = ref(null)

// 根据菜单切换组件
const currentComponent = computed(() => {
  debugger
  switch(activeMenu.value) {
    case 'realtime': return RealtimeMonitor
    case 'history': return HistoryData
    case 'devices': return DevicesManagement
    case 'threshold': return ThresholdSetting
    default: return RealtimeMonitor
  }
})

// 获取设备列表
const fetchDevices = async () => {
  const res = await getDevices()
  devices.value = res.data
  if (devices.value.length > 0) selectedDeviceId.value = devices.value[0].deviceId
}

onMounted(() => {
  fetchDevices()
})
</script>

<style scoped>
.el-header {
  display: flex;
  align-items: center;
  padding-left: 20px;
}

.el-aside {
  height: 100%;
}

.el-main {
  overflow-y: auto;
}
</style>
