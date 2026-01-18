<template>
  <el-card>
    <h2 style="margin-bottom: 20px">设备管理</h2>

    <!-- 搜索和操作区域 -->
    <el-row :gutter="20" style="margin-bottom: 20px">
      <el-col :span="8">
        <el-input
          v-model="searchQuery"
          placeholder="按设备名称或类型搜索"
          clearable
          @input="handleSearch"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-input>
      </el-col>
      <el-col :span="16" style="text-align: right">
        <el-button type="primary" @click="openAddDeviceDialog">
          <el-icon><Plus /></el-icon>
          添加设备
        </el-button>
        <el-button @click="fetchDevices">
          <el-icon><Refresh /></el-icon>
          刷新
        </el-button>
      </el-col>
    </el-row>

    <!-- 设备表格 -->
    <el-table :data="pagedDevices" style="width: 100%" v-loading="loading" stripe>
      <el-table-column prop="deviceId" label="ID" width="80" />
      <el-table-column prop="deviceName" label="设备名称" />
      <el-table-column prop="deviceType" label="设备类型" />
      <el-table-column prop="deviceTable" label="数据表名" />
      <el-table-column prop="deviceTableID" label="设备表主键" />
      <el-table-column prop="description" label="说明" />
      <el-table-column prop="createdAt" label="创建时间" width="180">
        <template #default="{ row }">
          {{ formatTime(row.createdAt) }}
        </template>
      </el-table-column>
      <el-table-column label="操作" width="250" fixed="right">
        <template #default="{ row }">
          <el-button type="primary" size="small" @click="showFactorDialog(row)">
            因子管理
          </el-button>
          <el-button type="success" size="small" @click="openEditDeviceDialog(row)">
            编辑
          </el-button>
          <el-button type="warning" size="small" @click="deleteRow(row)">
            删除
          </el-button>
        </template>
      </el-table-column>
    </el-table>

    <!-- 分页 -->
    <el-pagination
      v-model:current-page="currentPage"
      v-model:page-size="pageSize"
      :total="filteredDevices.length"
      :page-sizes="[10, 20, 50, 100]"
      layout="total, sizes, prev, pager, next, jumper"
      @size-change="handleSizeChange"
      @current-change="handleCurrentChange"
      style="margin-top: 20px; justify-content: flex-end"
    />

    <!-- 添加/编辑设备对话框 -->
    <el-dialog
      v-model="deviceDialogVisible"
      :title="deviceDialogTitle"
      width="700px"
      @closed="handleDeviceDialogClosed"
    >
      <el-form 
        ref="deviceFormRef"
        :model="deviceForm" 
        :rules="deviceRules" 
        label-width="120px"
        status-icon
      >
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="设备名称" prop="deviceName">
              <el-input 
                v-model="deviceForm.deviceName" 
                placeholder="请输入设备名称" 
                clearable
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="设备类型" prop="deviceType">
              <el-input 
                v-model="deviceForm.deviceType" 
                placeholder="请输入设备类型" 
                clearable
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="数据表名" prop="deviceTable" :disabled="isEditMode">
              <el-input 
                v-model="deviceForm.deviceTable" 
                placeholder="请输入数据表名（英文）" 
                clearable
                :disabled="isEditMode"
              />
              <span v-if="isEditMode" style="font-size: 12px; color: #999">
                数据表名创建后不可修改
              </span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="设备表主键" prop="deviceTableID">
              <el-input 
                v-model="deviceForm.deviceTableID" 
                placeholder="请输入设备表主键" 
                clearable
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-form-item label="说明" prop="description">
          <el-input 
            v-model="deviceForm.description" 
            type="textarea" 
            :rows="3" 
            placeholder="请输入设备说明"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>

        <el-form-item label="自动创建数据表" v-if="!isEditMode">
          <el-switch v-model="deviceForm.autoCreateTable" />
          <span style="margin-left: 10px; font-size: 12px; color: #999">
            自动在数据库中创建对应的数据表和默认因子
          </span>
        </el-form-item>

        <!-- 当开启自动创建数据表时，显示默认因子配置 -->
        <div v-if="!isEditMode && deviceForm.autoCreateTable" style="margin-top: 20px; padding: 15px; background-color: #f8f9fa; border-radius: 4px;">
          <h4 style="margin-bottom: 15px; color: #333;">默认因子配置</h4>
          <el-form-item label="添加时间戳字段">
            <el-switch v-model="deviceForm.addTimestampField" />
            <span style="margin-left: 10px; font-size: 12px; color: #666">添加创建时间字段</span>
          </el-form-item>
          <el-form-item label="添加设备ID字段">
            <el-switch v-model="deviceForm.addDeviceIdField" />
            <span style="margin-left: 10px; font-size: 12px; color: #666">添加设备标识字段</span>
          </el-form-item>
          <el-form-item label="添加状态字段">
            <el-switch v-model="deviceForm.addStatusField" />
            <span style="margin-left: 10px; font-size: 12px; color: #666">添加设备状态字段</span>
          </el-form-item>
        </div>
      </el-form>

      <template #footer>
        <span class="dialog-footer">
          <el-button @click="deviceDialogVisible = false">取消</el-button>
          <el-button type="primary" @click="submitDeviceForm" :loading="submitting">
            {{ isEditMode ? '保存' : '添加' }}
          </el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 因子管理对话框 -->
    <el-dialog
      v-model="factorDialogVisible"
      :title="`${currentDevice.deviceName} - 因子管理`"
      width="1000px"
      @closed="handleFactorDialogClosed"
    >
      <!-- 添加因子 -->
      <el-form 
        :model="newFactor" 
        label-width="100px" 
        style="margin-bottom: 20px" 
        :rules="factorRules"
        ref="factorFormRef"
      >
        <el-row :gutter="20">
          <el-col :span="6">
            <el-form-item label="字段名" prop="fieldName" required>
              <el-input v-model="newFactor.fieldName" placeholder="字段名（英文）" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="显示名称" prop="displayName" required>
              <el-input v-model="newFactor.displayName" placeholder="显示名称" />
            </el-form-item>
          </el-col>
          <el-col :span="4">
            <el-form-item label="显示单位" prop="displayUnit">
              <el-input v-model="newFactor.displayUnit" placeholder="显示单位" />
            </el-form-item>
          </el-col>
          <el-col :span="4">
            <el-form-item label="数据单位" prop="dataUnit">
              <el-input v-model="newFactor.dataUnit" placeholder="数据单位" />
            </el-form-item>
          </el-col>
          <el-col :span="4" style="padding-top: 32px">
            <el-button type="primary" @click="addFactorAction">添加因子</el-button>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="6">
            <el-form-item label="数据类型" prop="fieldType">
              <el-select v-model="newFactor.fieldType" placeholder="请选择数据类型" style="width: 100%">
                <el-option label="整数(INT)" value="INT" />
                <el-option label="浮点数(FLOAT)" value="FLOAT" />
                <el-option label="双精度(DOUBLE)" value="DOUBLE" />
                <el-option label="小数(DECIMAL)" value="DECIMAL" />
                <el-option label="字符串(VARCHAR)" value="VARCHAR" />
                <el-option label="文本(TEXT)" value="TEXT" />
                <el-option label="布尔值(BIT)" value="BIT" />
                <el-option label="日期时间(DATETIME)" value="DATETIME" />
                <el-option label="时间戳(TIMESTAMP)" value="TIMESTAMP" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="3">
            <el-form-item label="字段长度" prop="FieldLength" v-if="showFieldLength">
              <el-input-number v-model="newFactor.FieldLength" :min="1" :max="4000" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="3">
            <el-form-item label="小数位数" prop="DecimalPlaces" v-if="showDecimalPlaces">
              <el-input-number v-model="newFactor.DecimalPlaces" :min="0" :max="10" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="排序" prop="sortOrder">
              <el-input-number v-model="newFactor.sortOrder" :min="0" :max="999" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="6">
            <el-form-item label="最小值" prop="minValue">
              <el-input v-model="newFactor.minValue" placeholder="最小阈值" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="最大值" prop="maxValue">
              <el-input v-model="newFactor.maxValue" placeholder="最大阈值" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="是否显示">
              <el-switch v-model="newFactor.isVisible" :active-value="true" :inactive-value="false" />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="是否预警">
              <el-switch v-model="newFactor.isThreshold" :active-value="true" :inactive-value="false" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="24">
            <el-form-item label="说明" prop="remarks">
              <el-input v-model="newFactor.remarks" placeholder="说明" />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <!-- 因子表格 -->
      <el-table :data="currentDeviceFactors" style="width: 100%" stripe border v-loading="factorLoading">
        <el-table-column prop="fieldName" label="字段名" width="120" />
        <el-table-column prop="fieldType" label="数据类型" width="100">
          <template #default="{ row }">
            {{ getDataTypeDisplay(row.fieldType, row.FieldLength, row.DecimalPlaces) }}
          </template>
        </el-table-column>
        <el-table-column prop="displayName" label="显示名称" width="120" />
        <el-table-column prop="displayUnit" label="显示单位" width="100" />
        <el-table-column prop="dataUnit" label="数据单位" width="100" />
        <el-table-column prop="sortOrder" label="排序" width="80" align="center" />
        <el-table-column label="预警值范围" width="150">
          <template #default="{ row }">
            <div v-if="row.minValue || row.maxValue">
              {{ row.minValue || '' }} ~ {{ row.maxValue || '' }}
            </div>
            <div v-else style="color: #ccc">未设置</div>
          </template>
        </el-table-column>
        <el-table-column prop="isVisible" label="是否显示" width="100" align="center">
          <template #default="{ row }">
            <el-switch
              v-model="row.isVisible"
              :active-value="true"
              :inactive-value="false"
              @change="updateFactorVisibility(row)"
            />
          </template>
        </el-table-column>
        <el-table-column prop="isThreshold" label="是否预警" width="100" align="center">
          <template #default="{ row }">
            <el-switch
              v-model="row.isThreshold"
              :active-value="true"
              :inactive-value="false"
              @change="updateFactorThreshold(row)"
            />
          </template>
        </el-table-column>
        <el-table-column prop="remarks" label="说明" width="150" />
        <el-table-column prop="CreatedTime" label="创建时间" width="150">
          <template #default="{ row }">
            {{ formatTime(row.CreatedTime) }}
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
      width="700px"
    >
      <el-form :model="editingFactor" label-width="100px" :rules="factorRules">
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="字段名">
              <el-input v-model="editingFactor.fieldName" disabled />
              <span style="font-size: 12px; color: #999">字段名创建后不可修改</span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="显示名称" prop="displayName" required>
              <el-input v-model="editingFactor.displayName" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="显示单位">
              <el-input v-model="editingFactor.displayUnit" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="数据单位">
              <el-input v-model="editingFactor.dataUnit" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="数据类型" prop="fieldType">
              <el-select v-model="editingFactor.fieldType" placeholder="请选择" style="width: 100%">
                <el-option label="整数(INT)" value="INT" />
                <el-option label="浮点数(FLOAT)" value="FLOAT" />
                <el-option label="双精度(DOUBLE)" value="DOUBLE" />
                <el-option label="小数(DECIMAL)" value="DECIMAL" />
                <el-option label="字符串(VARCHAR)" value="VARCHAR" />
                <el-option label="文本(TEXT)" value="TEXT" />
                <el-option label="布尔值(BIT)" value="BIT" />
                <el-option label="日期时间(DATETIME)" value="DATETIME" />
                <el-option label="时间戳(TIMESTAMP)" value="TIMESTAMP" />
              </el-select>
            </el-form-item>
          </el-col>
          <el-col :span="6" v-if="showEditFieldLength">
            <el-form-item label="字段长度" prop="FieldLength">
              <el-input-number v-model="editingFactor.FieldLength" :min="1" :max="4000" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="6" v-if="showEditDecimalPlaces">
            <el-form-item label="小数位数" prop="DecimalPlaces">
              <el-input-number v-model="editingFactor.DecimalPlaces" :min="0" :max="10" style="width: 100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="排序" prop="sortOrder">
              <el-input-number v-model="editingFactor.sortOrder" :min="0" :max="999" style="width: 100%" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="说明">
              <el-input v-model="editingFactor.remarks" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="预警值设置">
              <el-row :gutter="10">
                <el-col :span="11">
                  <el-input v-model="editingFactor.minValue" placeholder="最小值" />
                </el-col>
                <el-col :span="2" style="text-align: center">~</el-col>
                <el-col :span="11">
                  <el-input v-model="editingFactor.maxValue" placeholder="最大值" />
                </el-col>
              </el-row>
              <span style="font-size: 12px; color: #999">留空表示不设置预警</span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="是否显示">
              <el-switch v-model="editingFactor.isVisible" :active-value="true" :inactive-value="false" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="是否预警">
              <el-switch v-model="editingFactor.isThreshold" :active-value="true" :inactive-value="false" />
            </el-form-item>
          </el-col>
        </el-row>
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
import { ref, reactive, onMounted, computed, watch } from "vue";
import { Search, Plus, Refresh } from '@element-plus/icons-vue'
import {
  getDevices,
  addDevice,
  updateDevice,
  deleteDevice,
  getDeviceTable,
  addFactor,
  updateFactor,
  deleteFactor as deleteFactorApi,
} from "@/api/device";
import { ElMessage, ElMessageBox } from "element-plus";

// 设备数据
const devices = ref([]);
const loading = ref(false);
const searchQuery = ref('');
const currentPage = ref(1);
const pageSize = ref(10);

// 设备对话框相关
const deviceDialogVisible = ref(false);
const deviceFormRef = ref();
const deviceForm = reactive({
  deviceName: "",
  deviceType: "",
  deviceTable: "",
  deviceTableID: "",
  description: "",
  autoCreateTable: true,
  addTimestampField: true,
  addDeviceIdField: true,
  addStatusField: true
});
const isEditMode = ref(false);
const submitting = ref(false);
const currentDeviceId = ref(null);

// 因子对话框
const factorDialogVisible = ref(false);
const factorLoading = ref(false);
const currentDevice = ref({});
const currentDeviceFactors = ref([]);
const factorFormRef = ref();

// 新因子表单
const newFactor = reactive({
  fieldName: "",
  fieldType: "INT",
  FieldLength: 11,
  DecimalPlaces: 2,
  displayName: "",
  displayUnit: "",
  dataUnit: "",
  sortOrder: 0,
  isVisible: true,
  isThreshold: false,
  minValue: "",
  maxValue: "",
  remarks: "",
});

// 编辑因子对话框
const editFactorDialogVisible = ref(false);
const editingFactor = reactive({});

// 验证规则
const deviceRules = {
  deviceName: [
    { required: true, message: '设备名称不能为空', trigger: 'blur' },
    { min: 2, max: 50, message: '长度在 2 到 50 个字符', trigger: 'blur' }
  ],
  deviceTable: [
    { required: true, message: '数据表名不能为空', trigger: 'blur' },
    { pattern: /^[a-zA-Z_][a-zA-Z0-9_]*$/, message: '只能包含字母、数字和下划线，且以字母或下划线开头', trigger: 'blur' }
  ],
  deviceTableID: [
    { pattern: /^[a-zA-Z0-9_]*$/, message: '只能包含字母、数字和下划线', trigger: 'blur' }
  ]
};

const factorRules = {
  fieldName: [
    { required: true, message: '字段名不能为空', trigger: 'blur' },
    { pattern: /^[a-zA-Z_][a-zA-Z0-9_]*$/, message: '只能包含字母、数字和下划线，且以字母或下划线开头', trigger: 'blur' }
  ],
  displayName: [
    { required: true, message: '显示名称不能为空', trigger: 'blur' }
  ],
  fieldType: [
    { required: true, message: '数据类型不能为空', trigger: 'change' }
  ],
  FieldLength: [
    { required: true, message: '字段长度不能为空', trigger: 'blur' }
  ],
  DecimalPlaces: [
    { required: true, message: '小数位数不能为空', trigger: 'blur' }
  ]
};

// 计算属性
const filteredDevices = computed(() => {
  if (!searchQuery.value) return devices.value;
  
  const query = searchQuery.value.toLowerCase();
  return devices.value.filter(device => 
    device.deviceName?.toLowerCase().includes(query) ||
    device.deviceType?.toLowerCase().includes(query) ||
    device.description?.toLowerCase().includes(query)
  );
});

const pagedDevices = computed(() => {
  const start = (currentPage.value - 1) * pageSize.value;
  const end = start + pageSize.value;
  return filteredDevices.value.slice(start, end);
});

const deviceDialogTitle = computed(() => {
  return isEditMode.value ? '编辑设备' : '添加设备';
});

// 显示字段长度的数据类型
const showFieldLength = computed(() => {
  return ['VARCHAR', 'CHAR', 'DECIMAL'].includes(newFactor.fieldType);
});

// 显示小数位数的数据类型
const showDecimalPlaces = computed(() => {
  return ['FLOAT', 'DOUBLE', 'DECIMAL'].includes(newFactor.fieldType);
});

// 编辑时显示字段长度
const showEditFieldLength = computed(() => {
  return ['VARCHAR', 'CHAR', 'DECIMAL'].includes(editingFactor.fieldType);
});

// 编辑时显示小数位数
const showEditDecimalPlaces = computed(() => {
  return ['FLOAT', 'DOUBLE', 'DECIMAL'].includes(editingFactor.fieldType);
});

// 打开添加设备对话框
const openAddDeviceDialog = () => {
  resetDeviceForm();
  isEditMode.value = false;
  deviceDialogVisible.value = true;
};

// 打开编辑设备对话框
const openEditDeviceDialog = (device) => {
  resetDeviceForm();
  Object.assign(deviceForm, {
    deviceName: device.deviceName,
    deviceType: device.deviceType,
    deviceTable: device.deviceTable,
    deviceTableID: device.deviceTableID,
    description: device.description || '',
    autoCreateTable: false
  });
  currentDeviceId.value = device.deviceId;
  isEditMode.value = true;
  deviceDialogVisible.value = true;
};

// 重置设备表单
const resetDeviceForm = () => {
  deviceFormRef.value?.resetFields();
  Object.assign(deviceForm, {
    deviceName: "",
    deviceType: "",
    deviceTable: "",
    deviceTableID: "",
    description: "",
    autoCreateTable: true,
    addTimestampField: true,
    addDeviceIdField: true,
    addStatusField: true
  });
  currentDeviceId.value = null;
};

// 提交设备表单
const submitDeviceForm = async () => {
  if (!deviceFormRef.value) return;
  
  try {
    await deviceFormRef.value.validate();
    submitting.value = true;
    
    if (isEditMode.value) {
      // 编辑设备
      const updateData = {
        ...deviceForm,
        deviceId: currentDeviceId.value
      };
      await updateDevice(updateData);
      ElMessage.success('设备更新成功');
    } else {
      // 添加设备
      const addData = {
        ...deviceForm
      };
      await addDevice(addData);
      ElMessage.success('设备添加成功');
    }
    
    deviceDialogVisible.value = false;
    fetchDevices();
  } catch (error) {
    if (error && error.errors) {
      // 表单验证失败
      return;
    }
    console.error("操作失败:", error);
    ElMessage.error(isEditMode.value ? '更新设备失败' : '添加设备失败');
  } finally {
    submitting.value = false;
  }
};

// 处理设备对话框关闭
const handleDeviceDialogClosed = () => {
  resetDeviceForm();
};

// 加载设备列表
const fetchDevices = async () => {
  try {
    loading.value = true;
    const res = await getDevices();
    devices.value = res || [];
  } catch (error) {
    console.error("加载设备列表失败:", error);
    ElMessage.error("加载设备列表失败");
  } finally {
    loading.value = false;
  }
};

// 搜索处理
const handleSearch = () => {
  currentPage.value = 1;
};

// 分页处理
const handleSizeChange = (size) => {
  pageSize.value = size;
  currentPage.value = 1;
};

const handleCurrentChange = (page) => {
  currentPage.value = page;
};

// 删除设备
const deleteRow = async (device) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除设备 "${device.deviceName}" 吗？此操作会删除该设备的所有因子数据和相关表。`,
      "删除确认",
      {
        confirmButtonText: "确定",
        cancelButtonText: "取消",
        type: "warning",
      }
    );
    await deleteDevice(device.deviceId);
    ElMessage.success("设备删除成功");
    fetchDevices();
  } catch (error) {
    if (error !== "cancel") {
      console.error("删除设备失败:", error);
      ElMessage.error("删除设备失败");
    }
  }
};

// 显示因子管理对话框
const showFactorDialog = async (device) => {
  currentDevice.value = device;

  try {
    factorLoading.value = true;
    const res = await getDeviceTable(device.deviceTable);
    
    if (!res || !Array.isArray(res)) {
      currentDeviceFactors.value = [];
    } else {
      // 处理布尔值转换
      currentDeviceFactors.value = res.map(factor => ({
        ...factor,
        isVisible: factor.isVisible === true || factor.isVisible === 1 || factor.isVisible === 'true',
        isThreshold: factor.isThreshold === true || factor.isThreshold === 1 || factor.isThreshold === 'true',
        sortOrder: factor.sortOrder || 0,
        FieldLength: factor.FieldLength || null,
        DecimalPlaces: factor.DecimalPlaces || null,
        minValue: factor.minValue || '',
        maxValue: factor.maxValue || '',
      }));
    }
    
    factorDialogVisible.value = true;
  } catch (error) {
    console.error("加载因子数据失败:", error);
    ElMessage.error("加载因子数据失败: " + (error.message || error));
  } finally {
    factorLoading.value = false;
  }
};

// 添加因子
const addFactorAction = async () => {
  try {
    if (!newFactor.fieldName) {
      ElMessage.warning("请输入字段名");
      return;
    }

    if (!newFactor.displayName) {
      ElMessage.warning("请输入显示名称");
      return;
    }

    if (!newFactor.fieldType) {
      ElMessage.warning("请选择数据类型");
      return;
    }

    // 构建因子数据
    const factorData = {
      tableName: currentDevice.value.deviceTable,
      fieldName: newFactor.fieldName,
      fieldType: newFactor.fieldType,
      displayName: newFactor.displayName,
      displayUnit: newFactor.displayUnit || '',
      dataUnit: newFactor.dataUnit || '',
      sortOrder: newFactor.sortOrder || 0,
      isVisible: Boolean(newFactor.isVisible),
      isThreshold: Boolean(newFactor.isThreshold),
      minValue: newFactor.minValue || '',
      maxValue: newFactor.maxValue || '',
      remarks: newFactor.remarks || '',
    };

    // 根据数据类型添加额外字段
    if (showFieldLength.value && newFactor.FieldLength) {
      factorData.FieldLength = newFactor.FieldLength;
    }

    if (showDecimalPlaces.value && newFactor.DecimalPlaces !== undefined) {
      factorData.DecimalPlaces = newFactor.DecimalPlaces;
    }

    await addFactor(factorData);
    ElMessage.success("因子添加成功");

    // 重置表单
    Object.keys(newFactor).forEach(key => {
      if (key === 'fieldType') {
        newFactor[key] = 'INT';
      } else if (key === 'FieldLength') {
        newFactor[key] = 11;
      } else if (key === 'DecimalPlaces') {
        newFactor[key] = 2;
      } else if (key === 'isVisible') {
        newFactor[key] = true;
      } else if (key === 'isThreshold') {
        newFactor[key] = false;
      } else if (key === 'sortOrder') {
        newFactor[key] = 0;
      } else {
        newFactor[key] = '';
      }
    });

    // 刷新因子列表
    await showFactorDialog(currentDevice.value);
  } catch (error) {
    console.error("添加因子失败:", error);
    ElMessage.error("添加因子失败: " + (error.message || error));
  }
};

// 编辑因子
const editFactor = (factor) => {
  Object.keys(editingFactor).forEach(key => delete editingFactor[key]);
  Object.assign(editingFactor, { ...factor });
  editFactorDialogVisible.value = true;
};

// 保存因子编辑
const saveFactorEdit = async () => {
  try {
    const saveData = {
      ...editingFactor,
      tableName: currentDevice.value.deviceTable,
      // 确保布尔值正确传递
      isVisible: Boolean(editingFactor.isVisible),
      isThreshold: Boolean(editingFactor.isThreshold),
    };

    await updateFactor(saveData);
    ElMessage.success("因子更新成功");
    editFactorDialogVisible.value = false;
    await showFactorDialog(currentDevice.value);
  } catch (error) {
    console.error("更新因子失败:", error);
    ElMessage.error("更新因子失败: " + (error.message || error));
  }
};

// 删除因子
const deleteFactor = async (factor) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除因子 "${factor.displayName || factor.fieldName}" 吗？`,
      "删除确认",
      {
        confirmButtonText: "确定",
        cancelButtonText: "取消",
        type: "warning",
      }
    );

    await deleteFactorApi({
      fieldName: factor.fieldName,
      tableName: currentDevice.value.deviceTable,
    });

    ElMessage.success("因子删除成功");
    await showFactorDialog(currentDevice.value);
  } catch (error) {
    if (error !== "cancel") {
      console.error("删除因子失败:", error);
      ElMessage.error("删除因子失败");
    }
  }
};

// 更新因子显示状态
const updateFactorVisibility = async (factor) => {
  try {
    const updateData = {
      ...factor,
      tableName: currentDevice.value.deviceTable,
      isVisible: factor.isVisible,
    };

    await updateFactor(updateData);
    ElMessage.success("显示状态已更新");
  } catch (error) {
    console.error("更新显示状态失败:", error);
    ElMessage.error("更新显示状态失败");
  }
};

// 更新因子预警状态
const updateFactorThreshold = async (factor) => {
  try {
    const updateData = {
      ...factor,
      tableName: currentDevice.value.deviceTable,
      isThreshold: factor.isThreshold,
    };

    await updateFactor(updateData);
    ElMessage.success("预警状态已更新");
  } catch (error) {
    console.error("更新预警状态失败:", error);
    ElMessage.error("更新预警状态失败");
  }
};

// 处理因子对话框关闭
const handleFactorDialogClosed = () => {
  currentDevice.value = {};
  currentDeviceFactors.value = [];
};

// 格式化时间
const formatTime = (timeString) => {
  if (!timeString) return "-";

  try {
    const date = new Date(timeString);
    if (isNaN(date.getTime())) return timeString;

    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, "0");
    const day = date.getDate().toString().padStart(2, "0");
    const hours = date.getHours().toString().padStart(2, "0");
    const minutes = date.getMinutes().toString().padStart(2, "0");

    return `${year}-${month}-${day} ${hours}:${minutes}`;
  } catch (e) {
    return timeString;
  }
};

// 获取数据类型的显示文本
const getDataTypeDisplay = (fieldType, fieldLength, decimalPlaces) => {
  if (!fieldType) return '';
  
  switch (fieldType) {
    case 'VARCHAR':
      return fieldLength ? `${fieldType}(${fieldLength})` : fieldType;
    case 'DECIMAL':
      if (fieldLength && decimalPlaces !== undefined) {
        return `${fieldType}(${fieldLength},${decimalPlaces})`;
      }
      return fieldType;
    default:
      return fieldType;
  }
};

// 监听数据类型变化
watch(() => newFactor.fieldType, (newVal) => {
  // 根据数据类型设置默认值
  switch (newVal) {
    case 'INT':
      newFactor.FieldLength = 11;
      break;
    case 'VARCHAR':
      newFactor.FieldLength = 255;
      break;
    case 'DECIMAL':
      newFactor.FieldLength = 10;
      newFactor.DecimalPlaces = 2;
      break;
    case 'FLOAT':
    case 'DOUBLE':
      newFactor.DecimalPlaces = 2;
      break;
  }
});

// 初始化
onMounted(() => {
  fetchDevices();
});
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

:deep(.el-form-item) {
  margin-bottom: 18px;
}

.el-button + .el-button {
  margin-left: 8px;
}

.operation-buttons {
  display: flex;
  gap: 8px;
}
</style>