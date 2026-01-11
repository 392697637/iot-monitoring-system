<template>
  <el-container>
    <el-header>
      <el-select v-model="deviceId" placeholder="选择设备" @change="loadData">
        <el-option
          v-for="d in devices"
          :key="d.deviceId"
          :label="d.deviceName"
          :value="d.deviceId"
        />
      </el-select>
      
    </el-header>

    <el-main>
      <el-row :gutter="20">
        <el-col :span="6" v-for="item in metrics" :key="item.label">
          <el-card :class="{ alarm: status === 1 }">
            <div class="metric">
              <div class="label">{{ item.label }}</div>
              <div class="value">{{ item.value }}</div>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <audio ref="alarmAudio" :src="alarmUrl" autoplay loop v-if="play"></audio>
    </el-main>
  </el-container>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch } from 'vue'
import { getDevices, getLatestData } from '../api/device'

const devices = ref([])
const deviceId = ref(null)
const status = ref(0)
const play = ref(false)

const metrics = ref([
  { label: '温度 (℃)', value: '-' },
  { label: '湿度 (%)', value: '-' },
  { label: '电压 (V)', value: '-' },
  { label: '电流 (A)', value: '-' }
])

// 🔔 报警音路径（public 文件夹下）
const alarmUrl = '/data/alarm.mp3'
const alarmAudio = ref(null)

const loadData = async () => {
  if (!deviceId.value) return
  try {
    const res = await getLatestData(deviceId.value)
    const d = res.data
 
    if (!d) return

    metrics.value[0].value = d.temperature
    metrics.value[1].value = d.humidity
    metrics.value[2].value = d.voltage
    metrics.value[3].value = d.current

    status.value = d.status
    play.value = d.status === 1
  } catch (err) {
    console.error(err)
  }
}

// 控制报警音
watch(play, (val) => {
  if (alarmAudio.value) {
    if (val) alarmAudio.value.play()
    else alarmAudio.value.pause()
  }
})

let timer = null

onMounted(async () => {
  const res = await getDevices()
  // devices.value = res.data
    devices.value = res.data.$values
   
  if (devices.value.length > 0) {
    deviceId.value = devices.value[0].deviceId
    await loadData()
    timer = setInterval(loadData, 5000) // 每5秒刷新
  }
})

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<style scoped>
.metric {
  text-align: center;
}
.label {
  font-size: 14px;
  color: #666;
}
.value {
  font-size: 28px;
  font-weight: bold;
}
.alarm {
  border: 2px solid red;
}
</style>
