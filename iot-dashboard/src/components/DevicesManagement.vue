<template>
  <el-card>
    <h2 style="margin-bottom: 20px;">设备管理</h2>
    
    <!-- 添加设备区域 -->
    <el-row :gutter="20" style="margin-bottom: 20px;">
      <el-col :span="6">
        <el-input 
          v-model="newDevice.deviceName" 
          placeholder="设备名称"
          clearable
        ></el-input>
      </el-col>
      <el-col :span="6">
        <el-input 
          v-model="newDevice.location" 
          placeholder="设备位置"
          clearable
        ></el-input>
      </el-col>
      <el-col :span="6">
        <el-input 
          v-model="newDevice.deviceTable" 
          placeholder="数据表名（英文）"
          clearable
        ></el-input>
      </el-col>
      <el-col :span="6">
        <el-button type="primary" @click="addDeviceAction">添加设备</el-button>
      </el-col>
    </el-row>

    <!-- 设备表格 -->
    <el-table 
      :data="devices" 
      style="width: 100%"
      v-loading="loading"
      stripe
    >
      <el-table-column prop="deviceId" label="ID" width="80" />
      <el-table-column prop="deviceName" label="设备名称" />
      <el-table-column prop="location" label="位置" />
      <el-table-column prop="deviceTable" label="数据表" />
      <el-table-column prop="createdAt" label="创建时间" width="180">
        <template #default="{ row }">
          {{ formatTime(row.createdAt) }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="200" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" size="small" @click="showFactorDialog(row)">
            因子管理
          </el-button>
          <el-button type="warning" size="small" @click="deleteDevice(row)">
            删除
          </el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 因子管理对话框 -->
    <el-dialog
      v-model="factorDialogVisible"
      :title="`${currentDevice.deviceName} - 因子管理`"
      width="900px"
    >
      <!-- 添加因子 -->
      <div style="margin-bottom: 20px;">
        <el-row :gutter="20">
          <el-col :span="6">
            <el-input 
              v-model="newFactor.fieldName" 
              placeholder="字段名（英文）"
              clearable
            ></el-input>
          </el-col>
          <el-col :span="6">
            <el-input 
              v-model="newFactor.displayName" 
              placeholder="显示名称"
              clearable
            ></el-input>
          </el-col>
          <el-col :span="4">
            <el-input 
              v-model="newFactor.unit" 
              placeholder="单位"
              clearable
            ></el-input>
          </el-col>
          <el-col :span="4">
            <el-input 
              v-model="newFactor.minValue" 
              placeholder="最小值"
              type="number"
              clearable
            ></el-input>
          </el-col>
          <el-col :span="4">
            <el-button type="primary" @click="addFactorAction">添加因子</el-button>
          </el-col>
        </el-row>
      </div>

      <!-- 因子表格 -->
      <el-table 
        :data="currentDeviceFactors" 
        style="width: 100%"
        stripe
        border
      >
        <el-table-column prop="fieldName" label="字段名" width="150" />
        <el-table-column prop="displayName" label="显示名称" width="150" />
        <el-table-column prop="unit" label="单位" width="100" />
        <el-table-column label="预警值范围" width="200">
          <template #default="{ row }">
            <div v-if="row.minValue !== null && row.maxValue !== null">
              {{ row.minValue }} ~ {{ row.maxValue }}
            </div>
            <div v-else style="color: #ccc;">未设置</div>
          </template>
        </el-table-column>
        <el-table-column prop="isVisible" label="是否显示" width="100">
          <template #default="{ row }">
            <el-switch
              v-model="row.isVisible"
              @change="updateFactorVisibility(row)"
            />
          </template>
        </el-table-column>
        <el-table-column label="操作" width="150" fixed="right">
          <template #default="{ row }">
            <el-button type="warning" size="small" @click="editFactor(row)">
              编辑
            </el-button>
            <el-button type="danger" size="small" @click="deleteFactor(row)">
              删除
            </el-button>
          </template>
        </el-table-column>
      </el-table>

      <template #footer>
        <span class="dialog-footer">
          <el-button @click="factorDialogVisible = false">关闭</el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 编辑因子对话框 -->
    <el-dialog
      v-model="editFactorDialogVisible"
      :title="`编辑因子 - ${editingFactor.displayName || editingFactor.fieldName}`"
      width="600px"
    >
      <el-form :model="editingFactor" label-width="100px">
        <el-form-item label="字段名">
          <el-input v-model="editingFactor.fieldName" disabled></el-input>
          <span style="font-size: 12px; color: #999;">字段名创建后不可修改</span>
        </el-form-item>
        <el-form-item label="显示名称">
          <el-input v-model="editingFactor.displayName"></el-input>
        </el-form-item>
        <el-form-item label="单位">
          <el-input v-model="editingFactor.unit"></el-input>
        </el-form-item>
        <el-form-item label="预警值设置">
          <el-row :gutter="10">
            <el-col :span="11">
              <el-input 
                v-model="editingFactor.minValue" 
                placeholder="最小值"
                type="number"
              ></el-input>
            </el-col>
            <el-col :span="2" style="text-align: center">~</el-col>
            <el-col :span="11">
              <el-input 
                v-model="editingFactor.maxValue" 
                placeholder="最大值"
                type="number"
              ></el-input>
            </el-col>
          </el-row>
          <span style="font-size: 12px; color: #999;">留空表示不设置预警</span>
        </el-form-item>
        <el-form-item label="是否显示">
          <el-switch v-model="editingFactor.isVisible" />
        </el-form-item>
        <el-form-item label="数据类型">
          <el-select v-model="editingFactor.dataType" placeholder="请选择">
            <el-option label="数字" value="number" />
            <el-option label="文本" value="string" />
            <el-option label="布尔值" value="boolean" />
            <el-option label="日期时间" value="datetime" />
          </el-select>
        </el-form-item>
        <el-form-item label="小数位数">
          <el-input-number 
            v-model="editingFactor.decimalPlaces" 
            :min="0" 
            :max="6"
          ></el-input-number>
        </el-form-item>
      </el-form>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="editFactorDialogVisible = false">取消</el-button>
          <el-button type="primary" @click="saveFactorEdit">保存</el-button>
        </span>
      </template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import { 
  getDevices, 
  addDevice, 
  deleteDevice as deleteDeviceApi,
  getDeviceTable,
  addFactor,
  updateFactor,
  deleteFactor as deleteFactorApi
} from '@/api/device'
import { ElMessage, ElMessageBox } from 'element-plus'

// 设备数据
const devices = ref([])
const loading = ref(false)

// 新设备表单
const newDevice = ref({
  deviceName: '',
  location: '',
  deviceTable: ''
})

// 因子对话框
const factorDialogVisible = ref(false)
const currentDevice = ref({})
const currentDeviceFactors = ref([])

// 新因子表单
const newFactor = ref({
  fieldName: '',
  displayName: '',
  unit: '',
  minValue: null,
  maxValue: null,
  isVisible: true,
  dataType: 'number',
  decimalPlaces: 2
})

// 编辑因子对话框
const editFactorDialogVisible = ref(false)
const editingFactor = ref({})

// 加载设备列表
const fetchDevices = async () => {
  try {
    loading.value = true
    const res = await getDevices()
    devices.value = res.data || []
  } catch (error) {
    console.error('加载设备列表失败:', error)
    ElMessage.error('加载设备列表失败')
  } finally {
    loading.value = false
  }
}

// 添加设备
const addDeviceAction = async () => {
  if (!newDevice.value.deviceName) {
    ElMessage.warning('请输入设备名称')
    return
  }
  
  if (!newDevice.value.deviceTable) {
    ElMessage.warning('请输入数据表名')
    return
  }

  try {
    await addDevice(newDevice.value)
    ElMessage.success('设备添加成功')
    
    // 重置表单
    newDevice.value = {
      deviceName: '',
      location: '',
      deviceTable: ''
    }
    
    // 刷新列表
    fetchDevices()
  } catch (error) {
    console.error('添加设备失败:', error)
    ElMessage.error('添加设备失败')
  }
}

// 删除设备
const deleteDevice = async (device) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除设备 "${device.deviceName}" 吗？此操作会删除该设备的所有因子数据。`,
      '删除确认',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    await deleteDeviceApi(device.deviceId)
    ElMessage.success('设备删除成功')
    fetchDevices()
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除设备失败:', error)
      ElMessage.error('删除设备失败')
    }
  }
}

// 显示因子管理对话框
const showFactorDialog = async (device) => {
  currentDevice.value = device
  
  try {
    loading.value = true
    const res = await getDeviceTable(device.deviceTable)
    currentDeviceFactors.value = res.data || []
    factorDialogVisible.value = true
  } catch (error) {
    console.error('加载因子数据失败:', error)
    ElMessage.error('加载因子数据失败')
  } finally {
    loading.value = false
  }
}

// 添加因子
const addFactorAction = async () => {
  if (!newFactor.value.fieldName) {
    ElMessage.warning('请输入字段名')
    return
  }

  if (!newFactor.value.displayName) {
    ElMessage.warning('请输入显示名称')
    return
  }

  try {
    const factorData = {
      ...newFactor.value,
      tableName: currentDevice.value.deviceTable,
      deviceId: currentDevice.value.deviceId
    }
    
    await addFactor(factorData)
    ElMessage.success('因子添加成功')
    
    // 重置表单
    newFactor.value = {
      fieldName: '',
      displayName: '',
      unit: '',
      minValue: null,
      maxValue: null,
      isVisible: true,
      dataType: 'number',
      decimalPlaces: 2
    }
    
    // 刷新因子列表
    await showFactorDialog(currentDevice.value)
  } catch (error) {
    console.error('添加因子失败:', error)
    ElMessage.error('添加因子失败')
  }
}

// 编辑因子
const editFactor = (factor) => {
  editingFactor.value = { ...factor }
  editFactorDialogVisible.value = true
}

// 保存因子编辑
const saveFactorEdit = async () => {
  try {
    await updateFactor({
      ...editingFactor.value,
      tableName: currentDevice.value.deviceTable
    })
    
    ElMessage.success('因子更新成功')
    editFactorDialogVisible.value = false
    
    // 刷新因子列表
    await showFactorDialog(currentDevice.value)
  } catch (error) {
    console.error('更新因子失败:', error)
    ElMessage.error('更新因子失败')
  }
}

// 删除因子
const deleteFactor = async (factor) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除因子 "${factor.displayName || factor.fieldName}" 吗？`,
      '删除确认',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    
    await deleteFactorApi({
      fieldName: factor.fieldName,
      tableName: currentDevice.value.deviceTable
    })
    
    ElMessage.success('因子删除成功')
    
    // 刷新因子列表
    await showFactorDialog(currentDevice.value)
  } catch (error) {
    if (error !== 'cancel') {
      console.error('删除因子失败:', error)
      ElMessage.error('删除因子失败')
    }
  }
}

// 更新因子显示状态
const updateFactorVisibility = async (factor) => {
  try {
    await updateFactor({
      ...factor,
      tableName: currentDevice.value.deviceTable
    })
    
    ElMessage.success('显示状态已更新')
  } catch (error) {
    console.error('更新显示状态失败:', error)
    ElMessage.error('更新显示状态失败')
  }
}

// 格式化时间
const formatTime = (timeString) => {
  if (!timeString) return '-'
  
  try {
    const date = new Date(timeString)
    if (isNaN(date.getTime())) return timeString
    
    const year = date.getFullYear()
    const month = (date.getMonth() + 1).toString().padStart(2, '0')
    const day = date.getDate().toString().padStart(2, '0')
    const hours = date.getHours().toString().padStart(2, '0')
    const minutes = date.getMinutes().toString().padStart(2, '0')
    
    return `${year}-${month}-${day} ${hours}:${minutes}`
  } catch (e) {
    return timeString
  }
}

// 初始化
onMounted(() => {
  fetchDevices()
})
</script>

<style scoped>
.el-row {
  margin-bottom: 15px;
}

.el-dialog .el-row {
  margin-bottom: 10px;
}

.el-table {
  margin-top: 20px;
}

:deep(.el-dialog__body) {
  padding: 20px;
}
</style>