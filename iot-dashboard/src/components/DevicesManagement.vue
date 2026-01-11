<!-- DevicesManagement.vue（设备管理） -->
 <template>
  <el-card>
    <h2>设备管理</h2>
    <el-row :gutter="20" style="margin-bottom: 20px;">
      <el-col :span="6">
        <el-input v-model="newDeviceName" placeholder="设备名称"></el-input>
      </el-col>
      <el-col :span="6">
        <el-input v-model="newDeviceLocation" placeholder="设备位置"></el-input>
      </el-col>
      <el-col :span="4">
        <el-button type="primary" @click="addDeviceAction">添加设备</el-button>
      </el-col>
    </el-row>

    <el-table :data="devices" style="width: 100%">
      <el-table-column prop="deviceId" label="ID" />
      <el-table-column prop="deviceName" label="名称" />
      <el-table-column prop="location" label="位置" />
      <el-table-column prop="createdAt" label="创建时间" />
    </el-table>
  </el-card>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getDevices, addDevice } from '../api/device'

const devices = ref([])
const newDeviceName = ref('')
const newDeviceLocation = ref('')

const fetchDevices = async () => {
  const res = await getDevices()
  devices.value = res.data
}

// ⚡ 必须使用，否则 ESLint 报错
const addDeviceAction = async () => {
  if (!newDeviceName.value) return
  await addDevice({
    deviceName: newDeviceName.value,
    location: newDeviceLocation.value
  })
  newDeviceName.value = ''
  newDeviceLocation.value = ''
  fetchDevices()
}

onMounted(() => {
  fetchDevices()
})
</script>
