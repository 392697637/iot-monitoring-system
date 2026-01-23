<template>
  <el-card>
    <h2 style="margin-bottom: 20px">设备管理</h2>

    <div class="device-management-container">
      <!-- 左侧设备列表 -->
      <div class="device-list-section">
        <div class="device-list-header">
          <h3>设备列表</h3>
          <div class="device-actions">
            <el-button type="primary" @click="openAddDeviceDialog" size="small">
              <el-icon><Plus /></el-icon>
              添加设备
            </el-button>
            <el-button @click="fetchDevices" size="small">
              <el-icon><Refresh /></el-icon>
              刷新
            </el-button>
          </div>
        </div>

        <div class="device-list">
          <div
            v-for="device in devices"
            :key="device.deviceId"
            class="device-item"
            :class="{ active: activeDeviceId === device.deviceId }"
            @click="selectDevice(device)"
          >
            <div class="device-info">
              <div class="device-name">{{ device.deviceName }}</div>
              <div class="device-type">{{ device.deviceType }}</div>
              <div class="device-table">表名: {{ device.deviceTable }}</div>
            </div>
            <div class="device-actions">
              <el-button
                type="primary"
                size="small"
                @click.stop="openEditDeviceDialog(device)"
                title="编辑设备"
              >
                <el-icon><Edit /></el-icon>
              </el-button>
              <el-button
                type="danger"
                size="small"
                @click.stop="deleteDeviceAction(device)"
                title="删除设备"
              >
                <el-icon><Delete /></el-icon>
              </el-button>
            </div>
          </div>
        </div>
      </div>

      <!-- 右侧因子管理 -->
      <div class="factor-management-section">
        <div class="factor-management-header" v-if="activeDevice">
          <h3>{{ activeDevice.deviceName }} - 因子管理</h3>
          <div class="factor-actions">
            <el-button
              type="primary"
              @click="openAddFactorDialog"
              :disabled="!activeDevice"
            >
              <el-icon><Plus /></el-icon>
              添加因子
            </el-button>
          </div>
        </div>

        <!-- 因子表格 -->
        <div v-if="activeDevice" class="factor-table-container">
          <el-table
            :data="pagedFactors"
            style="width: 100%"
            v-loading="factorLoading"
            stripe
            border
            height="calc(100vh - 280px)"
          >
            <el-table-column prop="fieldName" label="字段名" width="120" />
            <el-table-column prop="fieldType" label="数据类型" width="100">
              <template #default="{ row }">
                {{
                  getDataTypeDisplay(
                    row.fieldType,
                    row.FieldLength,
                    row.DecimalPlaces
                  )
                }}
              </template>
            </el-table-column>
            <el-table-column prop="displayName" label="显示名称" width="120" />
            <el-table-column prop="displayUnit" label="显示单位" width="100" />
            <el-table-column
              prop="sortOrder"
              label="排序"
              width="80"
              align="center"
            />
            <el-table-column label="预警值范围" width="150">
              <template #default="{ row }">
                <div v-if="row.configMinValue || row.configMaxValue">
                  {{ row.configMinValue || "" }} ~
                  {{ row.configMaxValue || "" }}
                </div>
                <div v-else style="color: #ccc">未设置</div>
              </template>
            </el-table-column>
            <el-table-column
              prop="isVisible"
              label="是否显示"
              width="100"
              align="center"
            >
              <template #default="{ row }">
                <el-switch
                  v-model="row.isVisible"
                  :active-value="true"
                  :inactive-value="false"
                  @change="updateFactorVisibility(row)"
                />
              </template>
            </el-table-column>
            <el-table-column
              prop="isAlarm"
              label="是否预警"
              width="100"
              align="center"
            >
              <template #default="{ row }">
                <el-switch
                  v-model="row.isAlarm"
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
                <el-button
                  type="danger"
                  size="small"
                  @click="deleteFactor(row)"
                >
                  删除
                </el-button>
              </template>
            </el-table-column>
          </el-table>

          <!-- 因子分页 -->
          <el-pagination
            v-if="activeDeviceFactors.length > 0"
            v-model:current-page="currentFactorPage"
            v-model:page-size="factorPageSize"
            :total="activeDeviceFactors.length"
            :page-sizes="[10, 20, 50, 100]"
            layout="total, sizes, prev, pager, next, jumper"
            @size-change="handleFactorSizeChange"
            @current-change="handleFactorCurrentChange"
            style="margin-top: 20px; justify-content: flex-end"
          />
        </div>

        <!-- 空状态 -->
        <div v-else class="empty-state">
          <el-empty description="请从左侧选择一个设备查看其因子" />
        </div>
      </div>
    </div>

    <!-- 添加/编辑设备对话框 -->
    <el-dialog
      v-model="deviceDialogVisible"
      :title="deviceDialogTitle"
      width="500px"
      @closed="handleDeviceDialogClosed"
    >
      <el-form
        ref="deviceFormRef"
        :model="deviceForm"
        :rules="deviceRules"
        label-width="120px"
        status-icon
      >
        <el-form-item label="设备名称" prop="deviceName">
          <el-input
            v-model="deviceForm.deviceName"
            placeholder="请输入设备名称"
            clearable
          />
        </el-form-item>

        <el-form-item label="设备类型" prop="deviceType">
          <el-select
            v-model="deviceForm.deviceType"
            placeholder="请选择设备类型"
            style="width: 100%"
            @change="handleDeviceTypeChange"
          >
            <el-option label="Access" value="Access" />
            <el-option label="OpcUA" value="OpcUA" />
          </el-select>
        </el-form-item>

        <el-form-item label="数据表名" prop="deviceTable">
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

        <el-form-item label="设备表主键" prop="deviceTableID">
          <el-input
            v-model="deviceForm.deviceTableID"
            placeholder="请输入设备表主键"
            clearable
          />
        </el-form-item>

        <!-- Access相关字段 -->
        <template v-if="deviceForm.deviceType === 'Access'">
          <el-form-item label="Access服务地址" prop="accessUrl">
            <el-input
              v-model="deviceForm.accessUrl"
              placeholder="例如：https://api.example.com/access"
              clearable
            />
          </el-form-item>
        </template>

        <!-- OpcUA相关字段 -->
        <template v-if="deviceForm.deviceType === 'OpcUA'">
          <el-form-item label="OpcUa服务地址" prop="opcUaUrl">
            <el-input
              v-model="deviceForm.opcUaUrl"
              placeholder="例如：opc.tcp://192.168.1.100:4840"
              clearable
            />
          </el-form-item>
          <el-form-item label="OpcUa用户名" prop="opcUaUser">
            <el-input
              v-model="deviceForm.opcUaUser"
              placeholder="请输入OpcUa用户名"
              clearable
            />
          </el-form-item>
          <el-form-item label="OpcUa密码" prop="opcUaPass">
            <el-input
              v-model="deviceForm.opcUaPass"
              type="password"
              placeholder="请输入OpcUa密码"
              clearable
              show-password
            />
          </el-form-item>
        </template>

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
            自动创建数据表（只创建主键字段）
          </span>
        </el-form-item>
      </el-form>

      <template #footer>
        <span class="dialog-footer">
          <el-button @click="deviceDialogVisible = false">取消</el-button>
          <el-button
            type="primary"
            @click="submitDeviceForm"
            :loading="submitting"
          >
            {{ isEditMode ? "保存" : "添加" }}
          </el-button>
        </span>
      </template>
    </el-dialog>

    <!-- 添加/编辑因子对话框 -->
    <el-dialog
      v-model="factorDialogVisible"
      :title="factorDialogTitle"
      width="700px"
      @closed="handleFactorDialogClosed"
    >
      <el-form
        :model="factorForm"
        label-width="100px"
        :rules="factorRules"
        ref="factorFormRef"
      >
        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="字段名" prop="fieldName" required>
              <el-input
                v-model="factorForm.fieldName"
                placeholder="字段名（英文）"
                :disabled="isEditFactorMode"
              />
              <span
                v-if="isEditFactorMode"
                style="font-size: 12px; color: #999"
              >
                字段名创建后不可修改
              </span>
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="显示名称" prop="displayName" required>
              <el-input
                v-model="factorForm.displayName"
                placeholder="显示名称"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="显示单位" prop="displayUnit">
              <el-input
                v-model="factorForm.displayUnit"
                placeholder="显示单位"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label=" 阈值比较类型 " prop="configType">
              <el-input
                v-model="factorForm.configType"
                placeholder=" 阈值比较类型 "
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="数据类型" prop="fieldType">
              <el-select
                v-model="factorForm.fieldType"
                placeholder="请选择数据类型"
                style="width: 100%"
              >
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
          <el-col :span="6" v-if="showFieldLength">
            <el-form-item label="字段长度" prop="FieldLength">
              <el-input-number
                v-model="factorForm.FieldLength"
                :min="1"
                :max="4000"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="6" v-if="showDecimalPlaces">
            <el-form-item label="小数位数" prop="DecimalPlaces">
              <el-input-number
                v-model="factorForm.DecimalPlaces"
                :min="0"
                :max="10"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="排序" prop="sortOrder">
              <el-input-number
                v-model="factorForm.sortOrder"
                :min="0"
                :max="999"
                style="width: 100%"
              />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="说明" prop="remarks">
              <el-input v-model="factorForm.remarks" placeholder="说明" />
            </el-form-item>
          </el-col>
        </el-row>

        <el-row :gutter="20">
          <el-col :span="12">
            <el-form-item label="预警值设置">
              <el-row :gutter="10">
                <el-col :span="11">
                  <el-input
                    v-model="factorForm.configMinValue"
                    placeholder="最小值"
                  />
                </el-col>
                <el-col :span="2" style="text-align: center">~</el-col>
                <el-col :span="11">
                  <el-input
                    v-model="factorForm.configMaxValue"
                    placeholder="最大值"
                  />
                </el-col>
              </el-row>
              <span style="font-size: 12px; color: #999"
                >留空表示不设置预警</span
              >
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="是否显示">
              <el-switch
                v-model="factorForm.isVisible"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
          </el-col>
          <el-col :span="6">
            <el-form-item label="是否预警">
              <el-switch
                v-model="factorForm.isAlarm"
                :active-value="true"
                :inactive-value="false"
              />
            </el-form-item>
          </el-col>
        </el-row>
      </el-form>

      <template #footer>
        <span class="dialog-footer">
          <el-button @click="factorDialogVisible = false">取消</el-button>
          <el-button
            type="primary"
            @click="submitFactorForm"
            :loading="factorSubmitting"
          >
            {{ isEditFactorMode ? "保存" : "添加" }}
          </el-button>
        </span>
      </template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ref, reactive, onMounted, computed, watch } from "vue";
import { Search, Plus, Refresh, Edit, Delete } from "@element-plus/icons-vue";
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
const activeDeviceId = ref(null);

// 设备对话框相关
const deviceDialogVisible = ref(false);
const deviceFormRef = ref();
const deviceForm = reactive({
  deviceName: "",
  deviceType: "",
  deviceTable: "",
  deviceTableID: "",
  accessUrl: "", // Access服务地址
  opcUaUrl: "",
  opcUaUser: "",
  opcUaPass: "",
  description: "",
  autoCreateTable: false, // 默认为不创建表
});
const isEditMode = ref(false);
const submitting = ref(false);
const currentDeviceId = ref(null);

// 因子相关
const activeDevice = ref(null);
const activeDeviceFactors = ref([]);
const factorLoading = ref(false);
const currentFactorPage = ref(1);
const factorPageSize = ref(10);

// 因子对话框
const factorDialogVisible = ref(false);
const factorFormRef = ref();
const factorForm = reactive({
  fieldName: "",
  fieldType: "INT",
  FieldLength: 11,
  DecimalPlaces: 2,
  displayName: "",
  displayUnit: "",
  configType: "",
  sortOrder: 0,
  isVisible: true,
  isAlarm: false,
  configMinValue: "",
  configMaxValue: "",
  remarks: "",
});
const isEditFactorMode = ref(false);
const factorSubmitting = ref(false);
const editingFactorId = ref(null);

// 验证规则 - 动态验证规则函数
const getDeviceRules = (deviceType) => {
  const rules = {
    deviceName: [
      { required: true, message: "设备名称不能为空", trigger: "blur" },
      { min: 2, max: 50, message: "长度在 2 到 50 个字符", trigger: "blur" },
    ],
    deviceType: [
      { required: true, message: "设备类型不能为空", trigger: "change" },
    ],
    deviceTable: [
      { required: true, message: "数据表名不能为空", trigger: "blur" },
      {
        pattern: /^[a-zA-Z_][a-zA-Z0-9_]*$/,
        message: "只能包含字母、数字和下划线，且以字母或下划线开头",
        trigger: "blur",
      },
    ],
    deviceTableID: [
      {
        pattern: /^[a-zA-Z0-9_]*$/,
        message: "只能包含字母、数字和下划线",
        trigger: "blur",
      },
    ],
  };

  // 如果设备类型是 Access，添加相关验证规则
  if (deviceType === "Access") {
    rules.accessUrl = [
      {
        required: true,
        message: "Access服务地址不能为空",
        trigger: "blur",
      },
      {
        validator: (rule, value, callback) => {
          if (value) {
            // Access地址验证（HTTP/HTTPS）
            if (!value.startsWith("http://") && !value.startsWith("https://")) {
              callback(new Error("Access地址应以 http:// 或 https:// 开头"));
            } else {
              // 验证URL格式
              try {
                new URL(value);
                callback();
              } catch (error) {
                callback(new Error("请输入有效的URL地址"));
              }
            }
          } else {
            callback();
          }
        },
        trigger: "blur",
      },
    ];
  }

  // 如果设备类型是 OpcUA，添加相关验证规则
  if (deviceType === "OpcUA") {
    rules.opcUaUrl = [
      {
        required: true,
        message: "OpcUa服务地址不能为空",
        trigger: "blur",
      },
      {
        validator: (rule, value, callback) => {
          if (value) {
            // OpcUa地址验证
            if (!value.startsWith("opc.tcp://")) {
              callback(new Error("OpcUa地址应以 opc.tcp:// 开头"));
            } else {
              // 验证IP和端口格式
              const urlRegex =
                /^opc\.tcp:\/\/(([0-9]{1,3}\.){3}[0-9]{1,3}|localhost)(:[0-9]{1,5})?(\/.*)?$/;
              if (!urlRegex.test(value)) {
                callback(
                  new Error(
                    "请输入有效的OpcUa地址，例如：opc.tcp://192.168.1.100:4840"
                  )
                );
              } else {
                callback();
              }
            }
          } else {
            callback();
          }
        },
        trigger: "blur",
      },
    ];

    rules.opcUaUser = [
      {
        required: true,
        message: "OpcUa用户名不能为空",
        trigger: "blur",
      },
      {
        min: 1,
        max: 50,
        message: "用户名长度在 1 到 50 个字符",
        trigger: "blur",
      },
    ];

    rules.opcUaPass = [
      {
        required: true,
        message: "OpcUa密码不能为空",
        trigger: "blur",
      },
      {
        min: 1,
        max: 100,
        message: "密码长度在 1 到 100 个字符",
        trigger: "blur",
      },
    ];
  }

  return rules;
};

// 设备验证规则（响应式）
const deviceRules = ref(getDeviceRules(""));

const factorRules = {
  fieldName: [
    { required: true, message: "字段名不能为空", trigger: "blur" },
    {
      pattern: /^[a-zA-Z_][a-zA-Z0-9_]*$/,
      message: "只能包含字母、数字和下划线，且以字母或下划线开头",
      trigger: "blur",
    },
  ],
  displayName: [
    { required: true, message: "显示名称不能为空", trigger: "blur" },
  ],
  fieldType: [
    { required: true, message: "数据类型不能为空", trigger: "change" },
  ],
};

// 计算属性
const pagedFactors = computed(() => {
  if (!activeDeviceFactors.value) return [];

  const start = (currentFactorPage.value - 1) * factorPageSize.value;
  const end = start + factorPageSize.value;
  return activeDeviceFactors.value.slice(start, end);
});

const deviceDialogTitle = computed(() => {
  return isEditMode.value ? "编辑设备" : "添加设备";
});

const factorDialogTitle = computed(() => {
  if (!activeDevice.value) return "";
  return isEditFactorMode.value
    ? `编辑因子 - ${activeDevice.value.deviceName}`
    : `添加因子 - ${activeDevice.value.deviceName}`;
});

// 显示字段长度的数据类型
const showFieldLength = computed(() => {
  return ["VARCHAR", "CHAR", "DECIMAL"].includes(factorForm.fieldType);
});

// 显示小数位数的数据类型
const showDecimalPlaces = computed(() => {
  return ["FLOAT", "DOUBLE", "DECIMAL"].includes(factorForm.fieldType);
});

// 监听设备类型变化，更新验证规则
watch(
  () => deviceForm.deviceType,
  (newVal) => {
    // 根据设备类型动态更新验证规则
    deviceRules.value = getDeviceRules(newVal);
  },
  { immediate: true }
);

// 设备类型变化处理
const handleDeviceTypeChange = (value) => {
  // 清除相关字段的值
  if (value === "Access") {
    deviceForm.opcUaUrl = "";
    deviceForm.opcUaUser = "";
    deviceForm.opcUaPass = "";
  } else if (value === "OpcUA") {
    deviceForm.accessUrl = "";
  }
};

// 选择设备
const selectDevice = async (device) => {
  activeDeviceId.value = device.deviceId;
  activeDevice.value = device;

  try {
    factorLoading.value = true;
    const res = await getDeviceTable(device.deviceTable);

    if (!res || !Array.isArray(res)) {
      activeDeviceFactors.value = [];
    } else {
      // 处理布尔值转换
      activeDeviceFactors.value = res.map((factor) => ({
        ...factor,
        isVisible:
          factor.isVisible === true ||
          factor.isVisible === 1 ||
          factor.isVisible === "true",
        isAlarm:
          factor.isAlarm === true ||
          factor.isAlarm === 1 ||
          factor.isAlarm === "true",
        sortOrder: factor.sortOrder || 0,
        FieldLength: factor.FieldLength || null,
        DecimalPlaces: factor.DecimalPlaces || null,
        configMinValue: factor.configMinValue || "",
        configMaxValue: factor.configMaxValue || "",
      }));
    }
    currentFactorPage.value = 1;
  } catch (error) {
    console.error("加载因子数据失败:", error);
    ElMessage.error("加载因子数据失败: " + (error.message || error));
    activeDeviceFactors.value = [];
  } finally {
    factorLoading.value = false;
  }
};

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
    accessUrl: device.accessUrl || "",
    opcUaUrl: device.opcUaUrl || "",
    opcUaUser: device.opcUaUser || "",
    opcUaPass: device.opcUaPass || "",
    description: device.description || "",
    autoCreateTable: false,
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
    accessUrl: "",
    opcUaUrl: "",
    opcUaUser: "",
    opcUaPass: "",
    description: "",
    autoCreateTable: false,
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
        deviceId: currentDeviceId.value,
      };
      // 根据设备类型清理不相关的字段
      if (updateData.deviceType === "Access") {
        delete updateData.opcUaUrl;
        delete updateData.opcUaUser;
        delete updateData.opcUaPass;
      } else if (updateData.deviceType === "OpcUA") {
        delete updateData.accessUrl;
      }
      
      await updateDevice(updateData);
      ElMessage.success("设备更新成功");
    } else {
      // 添加设备
      const addData = {
        ...deviceForm,
      };
      // 根据设备类型清理不相关的字段
      if (addData.deviceType === "Access") {
        delete addData.opcUaUrl;
        delete addData.opcUaUser;
        delete addData.opcUaPass;
      } else if (addData.deviceType === "OpcUA") {
        delete addData.accessUrl;
      }
      
      await addDevice(addData);
      ElMessage.success("设备添加成功");
    }

    deviceDialogVisible.value = false;
    fetchDevices();
  } catch (error) {
    if (error && error.errors) {
      return;
    }
    console.error("操作失败:", error);
    ElMessage.error(isEditMode.value ? "更新设备失败" : "添加设备失败");
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

    // 如果有设备但未选中，默认选中第一个
    if (devices.value.length > 0 && !activeDeviceId.value) {
      selectDevice(devices.value[0]);
    }
  } catch (error) {
    console.error("加载设备列表失败:", error);
    ElMessage.error("加载设备列表失败");
  } finally {
    loading.value = false;
  }
};

// 删除设备
const deleteDeviceAction = async (device) => {
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

    // 如果删除的是当前选中的设备，清空右侧
    if (activeDeviceId.value === device.deviceId) {
      activeDeviceId.value = null;
      activeDevice.value = null;
      activeDeviceFactors.value = [];
    }

    fetchDevices();
  } catch (error) {
    if (error !== "cancel") {
      console.error("删除设备失败:", error);
      ElMessage.error("删除设备失败");
    }
  }
};

// 打开添加因子对话框
const openAddFactorDialog = () => {
  resetFactorForm();
  isEditFactorMode.value = false;
  factorDialogVisible.value = true;
};

// 编辑因子
const editFactor = (factor) => {
  resetFactorForm();
  Object.assign(factorForm, { ...factor });
  editingFactorId.value = factor.id || factor.fieldName;
  isEditFactorMode.value = true;
  factorDialogVisible.value = true;
};

// 重置因子表单
const resetFactorForm = () => {
  factorFormRef.value?.resetFields();
  Object.assign(factorForm, {
    fieldName: "",
    fieldType: "INT",
    FieldLength: 11,
    DecimalPlaces: 2,
    displayName: "",
    displayUnit: "",
    configType: "",
    sortOrder: 0,
    isVisible: true,
    isAlarm: false,
    configMinValue: "",
    configMaxValue: "",
    remarks: "",
  });
  editingFactorId.value = null;
};

// 提交因子表单
const submitFactorForm = async () => {
  if (!factorFormRef.value) return;

  try {
    await factorFormRef.value.validate();
    factorSubmitting.value = true;

    const factorData = {
      id: factorForm.id,
      tableName: activeDevice.value.deviceTable,
      fieldName: factorForm.fieldName,
      fieldType: factorForm.fieldType,
      displayName: factorForm.displayName,
      displayUnit: factorForm.displayUnit || "",
      configType: factorForm.configType || "",
      sortOrder: factorForm.sortOrder || 0,
      isVisible: Boolean(factorForm.isVisible),
      isAlarm: Boolean(factorForm.isAlarm),
      configMinValue: factorForm.configMinValue || "",
      configMaxValue: factorForm.configMaxValue || "",
      remarks: factorForm.remarks || "",
    };

    // 根据数据类型添加额外字段
    if (showFieldLength.value && factorForm.FieldLength) {
      factorData.FieldLength = factorForm.FieldLength;
    }

    if (showDecimalPlaces.value && factorForm.DecimalPlaces !== undefined) {
      factorData.DecimalPlaces = factorForm.DecimalPlaces;
    }
    if (isEditFactorMode.value) {
      // 编辑因子
      await updateFactor(factorData);
      ElMessage.success("因子更新成功");
    } else {
      // 添加因子
      const addData = {
        ...factorData,
      };
      await addFactor(addData);
      ElMessage.success("因子添加成功");
    }

    factorDialogVisible.value = false;

    // 刷新当前设备的因子列表
    if (activeDevice.value) {
      await selectDevice(activeDevice.value);
    }
  } catch (error) {
    console.error("操作失败:", error);
    ElMessage.error(isEditFactorMode.value ? "更新因子失败" : "添加因子失败");
  } finally {
    factorSubmitting.value = false;
  }
};

// 处理因子对话框关闭
const handleFactorDialogClosed = () => {
  resetFactorForm();
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
      id: factor.id,
      tableName: activeDevice.value.deviceTable,
    });

    ElMessage.success("因子删除成功");
    await selectDevice(activeDevice.value);
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
      tableName: activeDevice.value.deviceTable,
      isVisible: factor.isVisible,
    };

    await updateFactor(updateData);
    ElMessage.success("显示状态已更新");
  } catch (error) {
    console.error("更新显示状态失败:", error);
    ElMessage.error("更新显示状态失败");
    // 恢复原来的状态
    factor.isVisible = !factor.isVisible;
  }
};

// 更新因子预警状态
const updateFactorThreshold = async (factor) => {
  try {
    const updateData = {
      ...factor,
      tableName: activeDevice.value.deviceTable,
      isAlarm: factor.isAlarm,
    };

    await updateFactor(updateData);
    ElMessage.success("预警状态已更新");
  } catch (error) {
    console.error("更新预警状态失败:", error);
    ElMessage.error("更新预警状态失败");
    // 恢复原来的状态
    factor.isAlarm = !factor.isAlarm;
  }
};

// 因子分页处理
const handleFactorSizeChange = (size) => {
  factorPageSize.value = size;
  currentFactorPage.value = 1;
};

const handleFactorCurrentChange = (page) => {
  currentFactorPage.value = page;
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
  if (!fieldType) return "";

  switch (fieldType) {
    case "VARCHAR":
      return fieldLength ? `${fieldType}(${fieldLength})` : fieldType;
    case "DECIMAL":
      if (fieldLength && decimalPlaces !== undefined) {
        return `${fieldType}(${fieldLength},${decimalPlaces})`;
      }
      return fieldType;
    default:
      return fieldType;
  }
};

// 监听数据类型变化
watch(
  () => factorForm.fieldType,
  (newVal) => {
    // 根据数据类型设置默认值
    switch (newVal) {
      case "INT":
        factorForm.FieldLength = 11;
        break;
      case "VARCHAR":
        factorForm.FieldLength = 255;
        break;
      case "DECIMAL":
        factorForm.FieldLength = 10;
        factorForm.DecimalPlaces = 2;
        break;
      case "FLOAT":
      case "DOUBLE":
        factorForm.DecimalPlaces = 2;
        break;
    }
  }
);

// 初始化
onMounted(() => {
  fetchDevices();
});
</script>

<style scoped>
.device-management-container {
  display: flex;
  gap: 20px;
}

.device-list-section {
  width: 300px;
  border-right: 1px solid #e4e7ed;
  display: flex;
  flex-direction: column;
}

.device-list-header {
  padding: 10px 0;
  border-bottom: 1px solid #e4e7ed;
  margin-bottom: 10px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.device-list-header h3 {
  margin: 0;
  font-size: 16px;
  color: #303133;
}

.device-actions {
  display: flex;
  gap: 8px;
}

.device-list {
  flex: 1;
  overflow-y: auto;
  padding-right: 10px;
}

.device-item {
  padding: 12px;
  border: 1px solid #e4e7ed;
  border-radius: 4px;
  margin-bottom: 10px;
  cursor: pointer;
  transition: all 0.3s;
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
}

.device-item:hover {
  border-color: #409eff;
  background-color: #f5f7fa;
}

.device-item.active {
  border-color: #409eff;
  background-color: #ecf5ff;
}

.device-info {
  flex: 1;
}

.device-name {
  font-weight: bold;
  color: #303133;
  margin-bottom: 4px;
  font-size: 14px;
}

.device-type {
  color: #606266;
  font-size: 12px;
  margin-bottom: 2px;
}

.device-table {
  color: #909399;
  font-size: 12px;
}

.device-item .device-actions {
  display: flex;
  gap: 4px;
  opacity: 0;
  transition: opacity 0.3s;
}

.device-item:hover .device-actions {
  opacity: 1;
}

.factor-management-section {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.factor-management-header {
  padding: 10px 0;
  border-bottom: 1px solid #e4e7ed;
  margin-bottom: 10px;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.factor-management-header h3 {
  margin: 0;
  font-size: 16px;
  color: #303133;
}

.factor-table-container {
  flex: 1;
  display: flex;
  flex-direction: column;
}

.empty-state {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
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
</style>