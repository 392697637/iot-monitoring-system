<template>
  <el-card>
    <h2>设备阈值设置</h2>
    <el-row :gutter="20" style="margin-bottom:20px">
      <el-col :span="6">
        <el-select v-model="deviceId" placeholder="选择设备" @change="loadThreshold">
          <el-option
            v-for="d in devices"
            :key="d.deviceId"
            :label="d.deviceName"
            :value="d.deviceId"
          />
        </el-select>
      </el-col>
    </el-row>

    <div v-if="threshold">
      <el-row :gutter="20" style="margin-bottom:10px">
        <el-col :span="6">
          <el-input-number v-model="threshold.temperatureUpper" :min="0" :max="100" label="温度上限" />
        </el-col>
        <el-col :span="6">
          <el-input-number v-model="threshold.temperatureLower" :min="0" :max="100" label="温度下限" />
        </el-col>
      </el-row>

      <el-row :gutter="20" style="margin-bottom:10px">
        <el-col :span="6">
          <el-input-number v-model="threshold.humidityUpper" :min="0" :max="100" label="湿度上限" />
        </el-col>
        <el-col :span="6">
          <el-input-number v-model="threshold.humidityLower" :min="0" :max="100" label="湿度下限" />
        </el-col>
      </el-row>

      <el-row :gutter="20" style="margin-bottom:10px">
        <el-col :span="6">
          <el-input-number v-model="threshold.currentUpper" :min="0" :max="50" label="电流上限" />
        </el-col>
        <el-col :span="6">
          <el-input-number v-model="threshold.currentLower" :min="0" :max="50" label="电流下限" />
        </el-col>
      </el-row>

      <el-row :gutter="20" style="margin-bottom:10px">
        <el-col :span="6">
          <el-input-number v-model="threshold.voltageUpper" :min="0" :max="500" label="电压上限" />
        </el-col>
        <el-col :span="6">
          <el-input-number v-model="threshold.voltageLower" :min="0" :max="500" label="电压下限" />
        </el-col>
      </el-row>

      <el-button type="primary" @click="saveThreshold">保存阈值</el-button>
    </div>
  </el-card>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getDevices, getThresholdsByDevice, setThreshold } from '@/api/device'

const devices = ref([])
const deviceId = ref(null)
const threshold = ref(null)

// 获取设备列表
const loadDevices = async () => {
  const res = await getDevices()
  devices.value = res.data
  if (devices.value.length > 0) {
    deviceId.value = devices.value[0].deviceId
    loadThreshold()
  }
}

// 加载指定设备阈值
const loadThreshold = async () => {
  if (!deviceId.value) return
  const res = await getThresholdsByDevice(deviceId.value)
  if (res.data) {
    threshold.value = { ...res.data } // 克隆数据
  } else {
    threshold.value = {
      temperatureUpper: 50,
      temperatureLower: 0,
      humidityUpper: 80,
      humidityLower: 10,
      currentUpper: 10,
      currentLower: 0,
      voltageUpper: 240,
      voltageLower: 180
    }
  }
}

// 保存阈值
const saveThreshold = async () => {
  if (!deviceId.value || !threshold.value) return
  await setThreshold({ ...threshold.value, deviceId: deviceId.value })
  alert('阈值保存成功')
}

onMounted(() => {
  loadDevices()
})
</script>

<style scoped>
.el-card {
  padding: 20px;
}
</style>
