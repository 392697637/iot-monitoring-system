<template>
  <el-card>
    <h2>实时监控</h2>
    <el-select v-model="deviceId" placeholder="选择设备">
      <el-option
        v-for="d in devices"
        :key="d.deviceId"
        :label="d.deviceName"
        :value="d.deviceId"
      />
    </el-select>

    <el-row :gutter="20" style="margin-top: 20px;">
      <el-col :span="6" v-for="metric in metrics" :key="metric.label">
        <el-card :class="{ alarm: metric.alarm }">
          <div>{{ metric.label }}</div>
          <div>{{ metric.value }}</div>
        </el-card>
      </el-col>
    </el-row>
  </el-card>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue' // ✅ 添加 onUnmounted
import { getDevices, getLatestData } from '../api/device'

const devices = ref([])
const deviceId = ref(null)

const metrics = ref([
  { label: '温度 (℃)', value: '-', alarm: false },
  { label: '湿度 (%)', value: '-', alarm: false },
  { label: '电压 (V)', value: '-', alarm: false },
  { label: '电流 (A)', value: '-', alarm: false }
])

let timer = null
const alarmAudio = new Audio('/data/alarm.mp3')

const loadData = async () => {
  if (!deviceId.value) return
  const res = await getLatestData(deviceId.value)
  const d = res.data
  if (!d) return

  metrics.value[0].value = d.temperature
  metrics.value[1].value = d.humidity
  metrics.value[2].value = d.voltage
  metrics.value[3].value = d.current

  metrics.value.forEach(m => m.alarm = d.status === 1)

  if (d.status === 1) alarmAudio.play()
}

onMounted(async () => {
  const res = await getDevices()
  devices.value = res.data
  if (devices.value.length > 0) {
    deviceId.value = devices.value[0].deviceId
    loadData()
    timer = setInterval(loadData, 5000)
  }
})

// ✅ 修复 no-undef
onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped>
.alarm {
  border: 2px solid red;
}
</style>
