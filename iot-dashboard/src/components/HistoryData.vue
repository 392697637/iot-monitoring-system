<!-- HistoryData.vue（历史数据） -->
 <template>
  <el-card>
    <el-row :gutter="20" style="margin-bottom:20px">
      <el-col :span="6">
        <el-select v-model="selectedDeviceId" placeholder="选择设备">
          <el-option
            v-for="d in devices"
            :key="d.deviceId"
            :label="d.deviceName"
            :value="d.deviceId"
          />
        </el-select>
      </el-col>
      <el-col :span="10">
        <el-date-picker
          v-model="timeRange"
          type="datetimerange"
          start-placeholder="开始时间"
          end-placeholder="结束时间"
          style="width:100%"
        />
      </el-col>
      <el-col :span="4">
        <el-button type="primary" @click="search">查询</el-button>
      </el-col>
    </el-row>

    <el-table :data="list" style="margin-top:20px">
      <el-table-column prop="createdAt" label="时间" />
      <el-table-column prop="temperature" label="温度" />
      <el-table-column prop="humidity" label="湿度" />
      <el-table-column prop="voltage" label="电压" />
      <el-table-column prop="current" label="电流" />
      <el-table-column prop="status" label="状态">
        <template #default="scope">
          <el-tag :type="scope.row.status === 1 ? 'danger' : 'success'">
            {{ scope.row.status === 1 ? '报警' : '正常' }}
          </el-tag>
        </template>
      </el-table-column>
    </el-table>
  </el-card>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { getDevices, getHistoryData } from '@/api/device'

const devices = ref([])
const selectedDeviceId = ref(null)
const timeRange = ref([])
const list = ref([])

onMounted(async () => {
  const res = await getDevices()
  devices.value = res.data
})

const search = async () => {
  if (!selectedDeviceId.value || timeRange.value.length !== 2) return
  const [start, end] = timeRange.value
  const res = await getHistoryData(selectedDeviceId.value, start, end)
  list.value = res.data
}
</script>
