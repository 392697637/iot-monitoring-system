<template>
  <div class="alarm-history-container">
    <el-card shadow="never">
      <!-- 查询条件 -->
      <div class="search-container">
        <el-row :gutter="20">
          <el-col :span="6">
            <el-select
              v-model="queryParams.deviceId"
              placeholder="选择报警设备"
              clearable
              filterable
              style="width: 100%"
            >
              <el-option
                v-for="device in deviceOptions"
                :key="device.value"
                :label="device.label"
                :value="device.value"
              />
            </el-select>
          </el-col>
          
          <el-col :span="6">
            <el-select
              v-model="queryParams.alarmFactor"
              placeholder="选择报警因子"
              clearable
              style="width: 100%"
            >
              <el-option
                v-for="factor in factorOptions"
                :key="factor.value"
                :label="factor.label"
                :value="factor.value"
              />
            </el-select>
          </el-col>
          
          <el-col :span="8">
            <el-date-picker
              v-model="dateRange"
              type="daterange"
              range-separator="至"
              start-placeholder="开始日期"
              end-placeholder="结束日期"
              value-format="YYYY-MM-DD HH:mm:ss"
              format="YYYY-MM-DD HH:mm"
              style="width: 100%"
            />
          </el-col>
          
          <el-col :span="4" style="text-align: right">
            <el-button type="primary" @click="handleSearch" :loading="loading">
              <el-icon><Search /></el-icon>查询
            </el-button>
            <el-button @click="handleReset">
              <el-icon><Refresh /></el-icon>重置
            </el-button>
          </el-col>
        </el-row>
      </div>

      <!-- 统计信息 -->
      <el-row :gutter="20" class="stats-row">
        <el-col :span="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon" style="background: #e6f7ff;">
                <el-icon size="24" color="#1890ff"><Bell /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ stats.total || 0 }}</div>
                <div class="stat-label">总报警数</div>
              </div>
            </div>
          </el-card>
        </el-col>
        
        <el-col :span="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon" style="background: #f6ffed;">
                <el-icon size="24" color="#52c41a"><Clock /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ stats.today || 0 }}</div>
                <div class="stat-label">今日报警</div>
              </div>
            </div>
          </el-card>
        </el-col>
        
        <el-col :span="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon" style="background: #fff7e6;">
                <el-icon size="24" color="#fa8c16"><Odometer /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ stats.deviceCount || 0 }}</div>
                <div class="stat-label">涉及设备</div>
              </div>
            </div>
          </el-card>
        </el-col>
        
        <el-col :span="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon" style="background: #fff0f6;">
                <el-icon size="24" color="#eb2f96"><TrendCharts /></el-icon>
              </div>
              <div class="stat-info">
                <div class="stat-value">{{ stats.factorCount || 0 }}</div>
                <div class="stat-label">报警因子</div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <!-- 操作工具栏 -->
      <div class="toolbar-container">
        <el-button 
          type="danger" 
          plain 
          @click="handleBatchDelete"
          :disabled="selectedRows.length === 0"
        >
          <el-icon><Delete /></el-icon>批量删除
        </el-button>
        <el-button 
          type="warning" 
          plain 
          @click="handleRefresh"
          :loading="loading"
        >
          <el-icon><Refresh /></el-icon>刷新
        </el-button>
      </div>

      <!-- 数据表格 -->
      <div class="table-container">
        <el-table
          :data="tableData"
          v-loading="loading"
          stripe
          border
          style="width: 100%"
          @selection-change="handleSelectionChange"
        >
          <el-table-column type="selection" width="55" align="center" />
          
          <el-table-column
            prop="DeviceId"
            label="设备ID"
            width="120"
            sortable
          >
            <template #default="{ row }">
              <el-tag size="small" type="info">{{ row.DeviceId }}</el-tag>
            </template>
          </el-table-column>
          
          <el-table-column
            prop="DeviceName"
            label="设备名称"
            width="150"
            sortable
          >
            <template #default="{ row }">
              <div class="device-cell">
                <div style="font-weight: 500;">{{ row.DeviceName }}</div>
              </div>
            </template>
          </el-table-column>
          
          <el-table-column
            prop="AlarmTime"
            label="报警时间"
            width="180"
            sortable
          >
            <template #default="{ row }">
              <div class="time-cell">
                <div>{{ formatDateTime(row.AlarmTime) }}</div>
                <div style="color: #999; font-size: 12px;">
                  {{ formatTimeAgo(row.AlarmTime) }}
                </div>
              </div>
            </template>
          </el-table-column>
          
          <el-table-column
            prop="AlarmFactor"
            label="报警因子"
            width="120"
            sortable
          >
            <template #default="{ row }">
              <el-tag 
                :type="getFactorTagType(row.AlarmFactor)" 
                effect="light" 
                size="small"
              >
                {{ getFactorName(row.AlarmFactor) }}
              </el-tag>
            </template>
          </el-table-column>
          
          <el-table-column
            prop="FactorValue"
            label="设备数值"
            width="120"
            sortable
            align="right"
          >
            <template #default="{ row }">
              <div class="value-cell" :class="getValueClass(row)">
                <span class="value-number">{{ formatValue(row.FactorValue, row.AlarmFactor) }}</span>
                <span class="value-unit">{{ getUnit(row.AlarmFactor) }}</span>
              </div>
            </template>
          </el-table-column>
          
          <el-table-column
            prop="AlarmDescription"
            label="报警说明"
            min-width="200"
            show-overflow-tooltip
          >
            <template #default="{ row }">
              <div class="description-cell">
                {{ row.AlarmDescription || '-' }}
              </div>
            </template>
          </el-table-column>
          
          <el-table-column
            prop="CreateTime"
            label="记录时间"
            width="180"
            sortable
          >
            <template #default="{ row }">
              {{ formatDateTime(row.CreateTime) }}
            </template>
          </el-table-column>
          
          <el-table-column 
            label="操作" 
            width="150" 
            fixed="right"
            align="center"
          >
            <template #default="{ row }">
              <el-button 
                type="primary" 
                link 
                size="small"
                @click="handleViewDetail(row)"
              >
                详情
              </el-button>
              <el-button 
                type="danger" 
                link 
                size="small"
                @click="handleDelete(row)"
              >
                删除
              </el-button>
            </template>
          </el-table-column>
        </el-table>
      </div>

      <!-- 分页 -->
      <div class="pagination-container">
        <el-pagination
          v-model:current-page="queryParams.pageNumber"
          v-model:page-size="queryParams.pageSize"
          :page-sizes="[10, 20, 50, 100]"
          :total="pagination.total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handleSizeChange"
          @current-change="handleCurrentChange"
        />
      </div>
    </el-card>

    <!-- 详情对话框 -->
    <el-dialog
      v-model="detailVisible"
      title="报警详情"
      width="500px"
    >
      <el-descriptions 
        :column="1" 
        border 
        size="small"
        v-if="currentRow"
      >
        <el-descriptions-item label="报警ID">
          {{ currentRow.Id }}
        </el-descriptions-item>
        <el-descriptions-item label="设备ID">
          <el-tag size="small">{{ currentRow.DeviceId }}</el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="设备名称">
          {{ currentRow.DeviceName }}
        </el-descriptions-item>
        <el-descriptions-item label="报警时间">
          {{ formatDateTime(currentRow.AlarmTime) }}
        </el-descriptions-item>
        <el-descriptions-item label="报警因子">
          <el-tag :type="getFactorTagType(currentRow.AlarmFactor)">
            {{ getFactorName(currentRow.AlarmFactor) }}
          </el-tag>
        </el-descriptions-item>
        <el-descriptions-item label="设备数值">
          <div class="value-cell" :class="getValueClass(currentRow)">
            <span class="value-number">{{ formatValue(currentRow.FactorValue, currentRow.AlarmFactor) }}</span>
            <span class="value-unit">{{ getUnit(currentRow.AlarmFactor) }}</span>
          </div>
        </el-descriptions-item>
        <el-descriptions-item label="报警说明">
          <div style="white-space: pre-wrap;">{{ currentRow.AlarmDescription }}</div>
        </el-descriptions-item>
        <el-descriptions-item label="记录时间">
          {{ formatDateTime(currentRow.CreateTime) }}
        </el-descriptions-item>
      </el-descriptions>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="detailVisible = false">关闭</el-button>
          <el-button type="primary" @click="detailVisible = false">
            确定
          </el-button>
        </span>
      </template>
    </el-dialog>
  </div>
</template>

<script setup>
import { ref, reactive, onMounted, computed } from 'vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { 
  Search, 
  Refresh, 
  Delete, 
  Bell, 
  Clock, 
  Odometer, 
  TrendCharts 
} from '@element-plus/icons-vue'
import { getAlarmHistory, getAlarmStats, deleteAlarm, batchDeleteAlarm } from '@/api/device'

// 响应式数据
const loading = ref(false)
const tableData = ref([])
const selectedRows = ref([])
const dateRange = ref([])
const detailVisible = ref(false)
const currentRow = ref(null)

// 查询参数
const queryParams = reactive({
  deviceId: '',
  alarmFactor: '',
  startTime: '',
  endTime: '',
  orderby: 'alarm_time DESC',
  pageNumber: 1,
  pageSize: 20
})

// 分页信息
const pagination = reactive({
  total: 0,
  totalPages: 0
})

// 统计信息
const stats = reactive({
  total: 0,
  today: 0,
  deviceCount: 0,
  factorCount: 0
})

// 下拉选项
const deviceOptions = ref([
  { label: '温度传感器001', value: 'DEV001' },
  { label: '湿度控制器002', value: 'DEV002' },
  { label: '电压监测003', value: 'DEV003' },
  { label: '压力传感器004', value: 'DEV004' }
])

const factorOptions = ref([
  { label: '温度', value: 'temperature' },
  { label: '湿度', value: 'humidity' },
  { label: '电压', value: 'voltage' },
  { label: '压力', value: 'pressure' },
  { label: '流量', value: 'flow' },
  { label: '电流', value: 'current' }
])

// 计算属性
const selectedIds = computed(() => {
  return selectedRows.value.map(row => row.Id)
})

// 生命周期
onMounted(() => {
  loadAlarmHistory()
  loadAlarmStats()
})

// 加载报警历史数据
const loadAlarmHistory = async () => {
  try {
    loading.value = true
    
    // 处理时间范围
    if (dateRange.value && dateRange.value.length === 2) {
      queryParams.startTime = dateRange.value[0]
      queryParams.endTime = dateRange.value[1]
    } else {
      queryParams.startTime = ''
      queryParams.endTime = ''
    }
    
    const response = await getAlarmHistory(queryParams)
    
    if (response.success) {
      tableData.value = response.dataTable || []
      pagination.total = response.totalCount || 0
      pagination.totalPages = response.totalPages || 0
    } else {
      ElMessage.error(response.message || '获取数据失败')
    }
  } catch (error) {
    console.error('加载报警历史数据失败:', error)
    ElMessage.error('网络请求失败')
  } finally {
    loading.value = false
  }
}

// 加载统计信息
const loadAlarmStats = async () => {
  try {
    const response = await getAlarmStats()
    if (response.success) {
      Object.assign(stats, response.data || {})
    }
  } catch (error) {
    console.error('加载统计信息失败:', error)
  }
}

// 处理查询
const handleSearch = () => {
  queryParams.pageNumber = 1
  loadAlarmHistory()
}

// 处理重置
const handleReset = () => {
  queryParams.pageNumber = 1
  queryParams.deviceId = ''
  queryParams.alarmFactor = ''
  queryParams.startTime = ''
  queryParams.endTime = ''
  dateRange.value = []
  selectedRows.value = []
  loadAlarmHistory()
}

// 处理行选择
const handleSelectionChange = (rows) => {
  selectedRows.value = rows
}

// 查看详情
const handleViewDetail = (row) => {
  currentRow.value = row
  detailVisible.value = true
}

// 删除单条记录
const handleDelete = async (row) => {
  try {
    await ElMessageBox.confirm(
      `确认删除设备"${row.DeviceName}"的这条报警记录吗？`,
      '删除确认',
      {
        confirmButtonText: '确认删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    const response = await deleteAlarm(row.Id)
    
    if (response.success) {
      ElMessage.success('删除成功')
      // 重新加载数据
      loadAlarmHistory()
      loadAlarmStats()
    } else {
      ElMessage.error(response.message || '删除失败')
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除记录失败:', error)
      ElMessage.error('删除失败')
    }
  }
}

// 批量删除
const handleBatchDelete = async () => {
  if (selectedRows.value.length === 0) {
    ElMessage.warning('请先选择要删除的记录')
    return
  }
  
  try {
    await ElMessageBox.confirm(
      `确认删除选中的 ${selectedRows.value.length} 条报警记录吗？`,
      '批量删除确认',
      {
        confirmButtonText: '确认删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    const ids = selectedIds.value
    const response = await batchDeleteAlarm(ids)
    
    if (response.success) {
      ElMessage.success(response.message || `成功删除 ${selectedRows.value.length} 条记录`)
      // 清空选择
      selectedRows.value = []
      // 重新加载数据
      loadAlarmHistory()
      loadAlarmStats()
    } else {
      ElMessage.error(response.message || '批量删除失败')
    }
  } catch (error) {
    if (error !== 'cancel') {
      console.error('批量删除失败:', error)
      ElMessage.error('批量删除失败')
    }
  }
}

// 刷新数据
const handleRefresh = () => {
  loadAlarmHistory()
  loadAlarmStats()
}

// 分页处理
const handleSizeChange = (val) => {
  queryParams.pageSize = val
  loadAlarmHistory()
}

const handleCurrentChange = (val) => {
  queryParams.pageNumber = val
  loadAlarmHistory()
}

// 工具函数
const formatDateTime = (dateTime) => {
  if (!dateTime) return ''
  
  try {
    const date = new Date(dateTime)
    if (isNaN(date.getTime())) return dateTime
    
    const year = date.getFullYear()
    const month = String(date.getMonth() + 1).padStart(2, '0')
    const day = String(date.getDate()).padStart(2, '0')
    const hours = String(date.getHours()).padStart(2, '0')
    const minutes = String(date.getMinutes()).padStart(2, '0')
    const seconds = String(date.getSeconds()).padStart(2, '0')
    
    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`
  } catch (e) {
    return dateTime
  }
}

const formatTimeAgo = (dateTime) => {
  if (!dateTime) return ''
  
  try {
    const date = new Date(dateTime)
    if (isNaN(date.getTime())) return ''
    
    const now = new Date()
    const diffMs = now - date
    const diffMins = Math.floor(diffMs / 60000)
    const diffHours = Math.floor(diffMs / 3600000)
    const diffDays = Math.floor(diffMs / 86400000)
    
    if (diffMins < 1) {
      return '刚刚'
    } else if (diffMins < 60) {
      return `${diffMins}分钟前`
    } else if (diffHours < 24) {
      return `${diffHours}小时前`
    } else {
      return `${diffDays}天前`
    }
  } catch (e) {
    return ''
  }
}

const getFactorName = (factor) => {
  const factorMap = {
    'temperature': '温度',
    'humidity': '湿度',
    'voltage': '电压',
    'pressure': '压力',
    'flow': '流量',
    'current': '电流'
  }
  return factorMap[factor] || factor
}

const getFactorTagType = (factor) => {
  const typeMap = {
    'temperature': 'danger',
    'humidity': 'warning',
    'voltage': 'danger',
    'pressure': 'warning',
    'flow': 'info',
    'current': 'danger'
  }
  return typeMap[factor] || 'info'
}

const getUnit = (factor) => {
  const unitMap = {
    'temperature': '°C',
    'humidity': '%',
    'voltage': 'V',
    'pressure': 'MPa',
    'flow': 'm³/h',
    'current': 'A'
  }
  return unitMap[factor] || ''
}

const formatValue = (value, factor) => {
  if (value === null || value === undefined) return '-'
  
  try {
    const numValue = Number(value)
    if (isNaN(numValue)) return value
    
    // 根据因子类型决定小数位数
    const decimalMap = {
      'temperature': 1,
      'humidity': 1,
      'voltage': 1,
      'pressure': 2,
      'flow': 1,
      'current': 2
    }
    
    const decimals = decimalMap[factor] || 2
    return numValue.toFixed(decimals)
  } catch (e) {
    return value
  }
}

const getValueClass = (row) => {
  const value = row.FactorValue
  const factor = row.AlarmFactor
  
  if (typeof value !== 'number' || isNaN(value)) return ''
  
  // 根据因子类型判断报警级别
  if (factor === 'temperature') {
    if (value > 80) return 'value-danger'
    if (value > 60) return 'value-warning'
  } else if (factor === 'voltage') {
    if (value > 250 || value < 200) return 'value-danger'
    if (value > 240 || value < 210) return 'value-warning'
  } else if (factor === 'pressure') {
    if (value > 10) return 'value-danger'
    if (value > 8) return 'value-warning'
  } else if (factor === 'humidity') {
    if (value > 85 || value < 20) return 'value-warning'
  }
  
  return ''
}
</script>

<style scoped>
.alarm-history-container {
  padding: 20px;
}

.search-container {
  margin-bottom: 20px;
}

.stats-row {
  margin-bottom: 20px;
}

.stat-card {
  border-radius: 8px;
  border: none;
}

.stat-content {
  display: flex;
  align-items: center;
}

.stat-icon {
  width: 48px;
  height: 48px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin-right: 12px;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 24px;
  font-weight: 600;
  color: #000;
  margin-bottom: 4px;
}

.stat-label {
  font-size: 14px;
  color: #666;
}

.toolbar-container {
  margin-bottom: 20px;
  display: flex;
  gap: 10px;
  align-items: center;
}

.table-container {
  margin-top: 20px;
  min-height: 400px;
}

.device-cell {
  line-height: 1.4;
}

.time-cell {
  line-height: 1.4;
}

.value-cell {
  display: flex;
  align-items: baseline;
  justify-content: flex-end;
}

.value-number {
  font-size: 14px;
  font-weight: 500;
}

.value-unit {
  font-size: 12px;
  color: #666;
  margin-left: 4px;
}

.value-danger .value-number {
  color: #f5222d;
  font-weight: bold;
}

.value-warning .value-number {
  color: #fa8c16;
  font-weight: bold;
}

.description-cell {
  max-width: 300px;
}

.pagination-container {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.dialog-footer {
  display: flex;
  justify-content: flex-end;
  gap: 10px;
}

:deep(.el-table) {
  margin-top: 0;
}

:deep(.el-table__header-wrapper) {
  font-weight: 600;
}

:deep(.el-table__cell) {
  padding: 8px 0;
}

:deep(.el-tag) {
  font-weight: 500;
}
</style>