<template>
  <el-card>
    <el-row :gutter="20" style="margin-bottom: 20px">
      <el-col :span="6">
        <el-select
          v-model="selectedDeviceId"
          placeholder="选择设备"
          @change="handleDeviceChange"
          style="width: 100%"
        >
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
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          format="YYYY-MM-DD"
          value-format="YYYY-MM-DD"
          :disabled-date="disabledDate"
          :shortcuts="shortcuts"
          style="width: 100%"
          @change="onDateChange"
        />
      </el-col>
      <el-col :span="8" style="text-align: right">
        <el-button
          type="primary"
          @click="search"
          :loading="loading"
          :disabled="!selectedDeviceId"
        >
          查询
        </el-button>
        <el-button @click="reset">重置</el-button>
        <el-dropdown @command="handleExport" style="margin-left: 10px">
          <el-button type="success">
            导出数据<el-icon class="el-icon--right"><arrow-down /></el-icon>
          </el-button>
          <template #dropdown>
            <el-dropdown-menu>
              <el-dropdown-item command="current">导出当前页数据</el-dropdown-item>
              <el-dropdown-item command="all">导出全部数据</el-dropdown-item>
              <el-dropdown-item command="custom" divided>自定义导出</el-dropdown-item>
            </el-dropdown-menu>
          </template>
        </el-dropdown>
      </el-col>
    </el-row>

    <!-- 统计信息 -->
    <el-row :gutter="20" style="margin-bottom: 15px">
      <el-col :span="24">
        <el-alert
          v-if="selectedDevice"
          :title="`当前设备：${selectedDevice.deviceName} | 监测因子：${tableColumns.length}个 | 时间范围：${timeRange[0]} 至 ${timeRange[1]} | 数据量：${pagination.total}条`"
          type="info"
          :closable="false"
        />
      </el-col>
    </el-row>

    <!-- 数据表格 -->
    <div class="table-container">
      <el-table
        :data="list"
        v-loading="loading"
        :max-height="600"
        stripe
        border
        style="width: 100%"
        @selection-change="handleSelectionChange"
      >
        <!-- 选择列 -->
        <el-table-column type="selection" width="55" fixed="left" />

        <!-- 数据时间列（移除 fixed="left"） -->
        <!-- <el-table-column
          prop="DataTime"
          label="数据时间"
          width="180"
          sortable
        >
          <template #default="{ row }">
            {{ formatTime(row.DataTime) }}
          </template>
        </el-table-column> -->

        <!-- 设备信息列 -->
        <el-table-column
          v-if="selectedDevice"
          prop="deviceName"
          label="设备名称"
          width="150"
          fixed="left"
        >
          <template #default>
            {{ selectedDevice.deviceName }}
          </template>
        </el-table-column>

        <!-- 动态因子列 -->
        <el-table-column
          v-for="column in tableColumns"
          :key="column.fieldName"
          :prop="column.fieldName"
          :label="column.displayName"
          :width="getColumnWidth(column.fieldName)"
          min-width="130"
          sortable
          :sort-by="column.fieldName"
        >
          <template #default="{ row }">
            <div
              :class="{ 'alarm-value': getAlarmStatus(row, column.fieldName) }"
            >
              {{ formatValue(row[column.fieldName], column.fieldName) }}
            </div>
          </template>
        </el-table-column>

        <!-- 状态列 -->
        <el-table-column prop="status" label="状态" width="100" fixed="right">
          <template #default="{ row }">
            <el-tag
              :type="
                row.status === 1 || row.alarmStatus === 1 ? 'danger' : 'success'
              "
              size="small"
            >
              {{ row.status === 1 || row.alarmStatus === 1 ? "报警" : "正常" }}
            </el-tag>
          </template>
        </el-table-column>

        <!-- 操作列 -->
        <el-table-column label="操作" width="120" fixed="right">
          <template #default="{ row, $index }">
            <el-button type="primary" link @click="viewDetail(row)">
              详情
            </el-button>
            <el-button type="warning" link @click="exportRow(row, $index)">
              导出
            </el-button>
          </template>
        </el-table-column>
      </el-table>
    </div>

    <!-- 分页 -->
    <div
      class="pagination-container"
      v-if="list.length > 0"
    >
      <el-pagination
        v-model:current-page="pagination.currentPage"
        v-model:page-size="pagination.pageSize"
        :page-sizes="[10, 20, 50, 100, 200]"
        layout="total, sizes, prev, pager, next, jumper"
        :total="pagination.total"
        @size-change="handleSizeChange"
        @current-change="handleCurrentChange"
      />
    </div>

    <!-- 无数据提示 -->
    <el-empty
      v-if="!loading && list.length === 0 && hasSearched"
      description="暂无数据"
    />

    <!-- 导出对话框 -->
    <el-dialog
      v-model="exportDialogVisible"
      title="导出设置"
      width="500px"
    >
      <el-form :model="exportForm" label-width="80px">
        <el-form-item label="导出范围">
          <el-radio-group v-model="exportForm.range">
            <el-radio label="current">当前页数据（{{ list.length }}条）</el-radio>
            <el-radio label="selected">已选数据（{{ selectedRows.length }}条）</el-radio>
            <el-radio label="all">全部数据（{{ pagination.total }}条）</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="时间范围">
          <el-checkbox v-model="exportForm.includeTimeRange" :disabled="exportForm.range !== 'all'">
            使用当前查询时间范围
          </el-checkbox>
          <div v-if="exportForm.includeTimeRange" style="margin-top: 5px; color: #666;">
            {{ timeRange[0] }} 至 {{ timeRange[1] }}
          </div>
        </el-form-item>
        <el-form-item label="文件格式">
          <el-radio-group v-model="exportForm.format">
            <el-radio label="csv">CSV文件</el-radio>
            <el-radio label="excel">Excel文件</el-radio>
            <el-radio label="json">JSON文件</el-radio>
          </el-radio-group>
        </el-form-item>
        <el-form-item label="包含列">
          <el-checkbox-group v-model="exportForm.columns">
            <el-checkbox label="DataTime" disabled>数据时间</el-checkbox>
            <el-checkbox label="deviceName" disabled>设备名称</el-checkbox>
            <el-checkbox 
              v-for="col in tableColumns" 
              :key="col.fieldName" 
              :label="col.fieldName"
              :checked="true"
            >
              {{ col.displayName }}
            </el-checkbox>
          </el-checkbox-group>
        </el-form-item>
      </el-form>
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="exportDialogVisible = false">取消</el-button>
          <el-button type="primary" @click="confirmExport" :loading="exportLoading">
            开始导出
          </el-button>
        </span>
      </template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ref, onMounted, computed, watch } from "vue";
import { ElMessage, ElMessageBox } from "element-plus";
import { ArrowDown } from "@element-plus/icons-vue";
import { getDevices, getDeviceTable, getHistoryData } from "@/api/device";

// 响应式数据
const devices = ref([]);
const selectedDeviceId = ref(null);
const selectedDevice = ref(null);
const timeRange = ref([]);
const list = ref([]);
const loading = ref(false);
const tableColumns = ref([]);
const hasSearched = ref(false);
const selectedRows = ref([]); // 选择的列
const exportDialogVisible = ref(false);
const exportLoading = ref(false);

// 导出表单
const exportForm = ref({
  range: 'current',
  format: 'csv',
  columns: [],
  includeTimeRange: true
});

// 分页配置
const pagination = ref({
  currentPage: 1,
  pageSize: 20,
  total: 0,
});

// 设置默认时间为最近一年
const setDefaultTimeRange = () => {
  const end = new Date();
  const start = new Date();
  start.setFullYear(start.getFullYear() - 1); // 一年前
  
  // 格式化为 YYYY-MM-DD
  const formatDate = (date) => {
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, "0");
    const day = date.getDate().toString().padStart(2, "0");
    return `${year}-${month}-${day}`;
  };
  
  timeRange.value = [formatDate(start), formatDate(end)];
};

// 时间快捷选项（按日期）
const shortcuts = [
  {
    text: "最近7天",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setDate(start.getDate() - 7);
      return [formatDate(start), formatDate(end)];
    },
  },
  {
    text: "最近30天",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setDate(start.getDate() - 30);
      return [formatDate(start), formatDate(end)];
    },
  },
  {
    text: "最近3个月",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setMonth(start.getMonth() - 3);
      return [formatDate(start), formatDate(end)];
    },
  },
  {
    text: "最近1年",
    value: () => {
      const end = new Date();
      const start = new Date();
      start.setFullYear(start.getFullYear() - 1);
      return [formatDate(start), formatDate(end)];
    },
  },
  {
    text: "今年至今",
    value: () => {
      const end = new Date();
      const start = new Date(new Date().getFullYear(), 0, 1);
      return [formatDate(start), formatDate(end)];
    },
  },
];

// 格式化日期为 YYYY-MM-DD
const formatDate = (date) => {
  const year = date.getFullYear();
  const month = (date.getMonth() + 1).toString().padStart(2, "0");
  const day = date.getDate().toString().padStart(2, "0");
  return `${year}-${month}-${day}`;
};

// 生命周期
onMounted(async () => {
  setDefaultTimeRange();
  await loadDevices();
});

// 加载设备列表
const loadDevices = async () => {
  try {
    loading.value = true;
    const res = await getDevices();
    devices.value = res || [];

    if (devices.value.length > 0) {
      selectedDeviceId.value = devices.value[0].deviceId;
      selectedDevice.value = devices.value[0];
    }
  } catch (error) {
    console.error("加载设备列表失败:", error);
    ElMessage.error("加载设备列表失败");
  } finally {
    loading.value = false;
  }
};

// 设备变化处理
const handleDeviceChange = async (deviceId) => {
  const device = devices.value.find((d) => d.deviceId === deviceId);
  if (device) {
    selectedDevice.value = device;
    selectedDeviceId.value = deviceId;
    await loadDeviceTable(device.deviceTable);
    list.value = [];
    hasSearched.value = false;
  }
};

// 加载设备表结构
const loadDeviceTable = async (tableName) => {
  if (!tableName) return;

  try {
    loading.value = true;
    const res = await getDeviceTable(tableName);
    const columns = res || [];

    tableColumns.value = columns
      .filter((col) => col.isVisible)
      .map((col) => ({
        fieldName: col.fieldName,
        displayName: col.displayName || col.fieldName,
        sortable: true,
        unit: col.unit || "",
        minValue: col.minValue,
        maxValue: col.maxValue,
      }));
debugger
    // 初始化导出列选择，添加设备名称
    exportForm.value.columns = ['DataTime', 'deviceName', ...tableColumns.value.map(col => col.fieldName)];
    
  } catch (error) {
    console.error("加载设备表结构失败:", error);
    ElMessage.error("加载设备表结构失败");
  } finally {
    loading.value = false;
  }
};

// 日期变化处理
const onDateChange = () => {
  if (hasSearched.value) {
    search();
  }
};

// 查询历史数据
const search = async () => {
  if (!selectedDeviceId.value) {
    ElMessage.warning("请先选择设备");
    return;
  }

  if (timeRange.value.length !== 2) {
    ElMessage.warning("请选择时间范围");
    return;
  }

  loading.value = true;
  hasSearched.value = true;

  try {
    const [startStr, endStr] = timeRange.value;
    
    // 转换为完整的时间格式（添加时分秒）
    const start = new Date(startStr + ' 00:00:00');
    const end = new Date(endStr + ' 23:59:59');
    
    // 格式化SQL时间
    const formatDateForSQL = (date) => {
      const pad = (num) => num.toString().padStart(2, "0");
      return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(
        date.getDate()
      )} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(
        date.getSeconds()
      )}`;
    };

    const timeField = "DataTime";

    debugger
    // 构建查询参数
    const params = {
      tableName: selectedDevice.value?.deviceTable,
      orderby: "ORDER BY DID DESC",
      where: `WHERE ${timeField} >= '${formatDateForSQL(start)}' AND ${timeField} <= '${formatDateForSQL(end)}'`,
      pageNumber: pagination.value.currentPage,
      pageSize: pagination.value.pageSize,
    };

    console.log("查询参数:", params);

    const res = await getHistoryData(params);

    // 处理返回数据
    if (res && res.success) {
      list.value = res.dataTable || [];
      pagination.value.total = res.totalCount || 0;
      
      if (list.value.length === 0) {
        ElMessage.info("该时间范围内没有数据");
      } else {
        ElMessage.success(`查询到 ${pagination.value.total} 条记录，当前显示 ${list.value.length} 条`);
      }
    } else {
      ElMessage.error(res?.message || "查询失败");
      list.value = [];
      pagination.value.total = 0;
    }
  } catch (error) {
    console.error("查询历史数据失败:", error);
    ElMessage.error("查询历史数据失败: " + (error.message || "未知错误"));
    list.value = [];
    pagination.value.total = 0;
  } finally {
    loading.value = false;
  }
};

// 重置查询
const reset = () => {
  setDefaultTimeRange();
  list.value = [];
  selectedRows.value = [];
  pagination.value.currentPage = 1;
  hasSearched.value = false;
};

// 行选择处理
const handleSelectionChange = (rows) => {
  selectedRows.value = rows;
};

// 导出处理
const handleExport = (command) => {
  if (command === 'custom') {
    exportDialogVisible.value = true;
  } else {
    exportForm.value.range = command;
    confirmExport();
  }
};

// 确认导出
const confirmExport = async () => {
  exportLoading.value = true;
  
  try {
    let dataToExport = [];
    let filename = '';
    
    switch (exportForm.value.range) {
      case 'current':
        dataToExport = list.value;
        filename = `${selectedDevice.value.deviceName}_当前页数据`;
        break;
      case 'selected':
        if (selectedRows.value.length === 0) {
          ElMessage.warning('请先选择要导出的数据');
          exportLoading.value = false;
          return;
        }
        dataToExport = selectedRows.value;
        filename = `${selectedDevice.value.deviceName}_选中数据`;
        break;
      case 'all':
        // 这里需要调用API获取全部数据
        await exportAllData();
        exportLoading.value = false;
        exportDialogVisible.value = false;
        return;
    }
    
    if (dataToExport.length === 0) {
      ElMessage.warning('没有数据可导出');
      exportLoading.value = false;
      return;
    }
    
    // 根据格式导出
    switch (exportForm.value.format) {
      case 'csv':
        exportToCSV(dataToExport, filename);
        break;
      case 'excel':
        exportToExcel(dataToExport, filename);
        break;
      case 'json':
        exportToJSON(dataToExport, filename);
        break;
    }
    
    ElMessage.success(`成功导出 ${dataToExport.length} 条数据`);
    exportDialogVisible.value = false;
    
  } catch (error) {
    console.error('导出失败:', error);
    ElMessage.error('导出失败');
  } finally {
    exportLoading.value = false;
  }
};

// 导出全部数据
const exportAllData = async () => {
  try {
    const [startStr, endStr] = timeRange.value;
    const start = new Date(startStr + ' 00:00:00');
    const end = new Date(endStr + ' 23:59:59');
    
    const formatDateForSQL = (date) => {
      const pad = (num) => num.toString().padStart(2, "0");
      return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(
        date.getDate()
      )} ${pad(date.getHours())}:${pad(date.getMinutes())}:${pad(
        date.getSeconds()
      )}`;
    };

    const timeField = "DataTime";

    // 构建参数查询所有数据（不分页）
    const params = {
      tableName: selectedDevice.value?.deviceTable,
      orderby: "ORDER BY DID DESC",
      where: `WHERE ${timeField} >= '${formatDateForSQL(start)}' AND ${timeField} <= '${formatDateForSQL(end)}'`,
      pageNumber: 1,
      pageSize: 1000000, // 设置一个大数获取所有数据
    };

    ElMessage.info('正在获取全部数据，请稍候...');
    
    const res = await getHistoryData(params);
    
    if (res && res.success) {
      const allData =res.dataTable || [];
      
      if (allData.length === 0) {
        ElMessage.warning('没有数据可导出');
        return;
      }
      
      const filename = `${selectedDevice.value.deviceName}_全部数据_${timeRange.value[0]}_至_${timeRange.value[1]}`;
      
      switch (exportForm.value.format) {
        case 'csv':
          exportToCSV(allData, filename);
          break;
        case 'excel':
          exportToExcel(allData, filename);
          break;
        case 'json':
          exportToJSON(allData, filename);
          break;
      }
      
      ElMessage.success(`成功导出 ${allData.length} 条数据`);
    }
    
  } catch (error) {
    console.error('导出全部数据失败:', error);
    ElMessage.error('导出全部数据失败');
  }
};

// 导出为CSV
const exportToCSV = (data, filename) => {
  // 筛选要导出的列
  const columnsToExport = exportForm.value.columns || ['DataTime', 'deviceName', ...tableColumns.value.map(col => col.fieldName)];
  
  // 准备表头
  const headers = columnsToExport.map(field => {
    if (field === 'DataTime') return '数据时间';
    if (field === 'deviceName') return '设备名称';
    const col = tableColumns.value.find(c => c.fieldName === field);
    return col ? col.displayName : field;
  });
  
  // 准备数据行，添加设备名称
  const rows = data.map(item => {
    return columnsToExport.map(field => {
      let value;
      if (field === 'deviceName') {
        value = selectedDevice.value?.deviceName || '';
      } else {
        value = item[field];
      }
      
      // 处理特殊字符
      if (value === null || value === undefined) return '';
      const strValue = String(value);
      // 处理逗号和引号
      if (strValue.includes(',') || strValue.includes('"') || strValue.includes('\n')) {
        return '"' + strValue.replace(/"/g, '""') + '"';
      }
      return strValue;
    });
  });
  
  // 构建CSV内容
  const csvContent = [
    headers.join(','),
    ...rows.map(row => row.join(','))
  ].join('\n');
  
  // 创建下载链接
  const blob = new Blob(['\uFEFF' + csvContent], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = `${filename}_${new Date().getTime()}.csv`;
  link.click();
};

// 导出为Excel（使用CSV格式，可以添加.xlsx扩展名）
const exportToExcel = (data, filename) => {
  exportToCSV(data, filename); // 简单起见，先用CSV
};

// 导出为JSON
const exportToJSON = (data, filename) => {
  // 在导出数据中添加设备名称
  const dataWithDeviceName = data.map(item => ({
    ...item,
    deviceName: selectedDevice.value?.deviceName || ''
  }));
  
  const jsonContent = JSON.stringify(dataWithDeviceName, null, 2);
  const blob = new Blob([jsonContent], { type: 'application/json;charset=utf-8;' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.download = `${filename}_${new Date().getTime()}.json`;
  link.click();
};

// 导出单行数据
const exportRow = (row, index) => {
  const filename = `${selectedDevice.value.deviceName}_单条数据_${index + 1}`;
  // 在导出单行数据时也添加设备名称
  const rowWithDeviceName = {
    ...row,
    deviceName: selectedDevice.value?.deviceName || ''
  };
  exportToJSON([rowWithDeviceName], filename);
  ElMessage.success('单条数据导出成功');
};

// 查看详情
const viewDetail = (row) => {
  ElMessageBox.alert(JSON.stringify(row, null, 2), "数据详情", {
    confirmButtonText: "确定",
    customClass: "detail-message-box",
    center: true,
    width: '600px'
  });
};

// 格式化时间（只显示到年月日）
// 格式化时间（显示年月日时分秒）
const formatTime = (timeString) => {
  if (!timeString) return "-";
  
  try {
    // 首先尝试直接解析字符串
    let date = new Date(timeString);
    
    // 如果解析失败，尝试处理常见的格式
    if (isNaN(date.getTime())) {
      // 移除可能的末尾空格和Z字符
      const cleaned = timeString.trim().replace(/Z$/, '');
      date = new Date(cleaned);
    }
    
    // 如果仍然失败，尝试其他格式
    if (isNaN(date.getTime())) {
      // 尝试匹配 ISO 8601 格式
      const isoMatch = timeString.match(/^(\d{4}-\d{2}-\d{2}[T\s]\d{2}:\d{2}:\d{2})/);
      if (isoMatch) {
        date = new Date(isoMatch[1].replace(' ', 'T'));
      }
    }
    
    // 最终检查
    if (isNaN(date.getTime())) {
      console.warn(`无法解析时间字符串: ${timeString}`);
      return timeString;
    }
    
    // 格式化为年月日时分秒
    const year = date.getFullYear();
    const month = (date.getMonth() + 1).toString().padStart(2, "0");
    const day = date.getDate().toString().padStart(2, "0");
    const hours = date.getHours().toString().padStart(2, "0");
    const minutes = date.getMinutes().toString().padStart(2, "0");
    const seconds = date.getSeconds().toString().padStart(2, "0");
    
    return `${year}-${month}-${day} ${hours}:${minutes}:${seconds}`;
    
  } catch (e) {
    console.error("时间格式化错误:", e);
    return timeString;
  }
};

// 格式化值
const formatValue = (value, fieldName) => {
  if (value === null || value === undefined || value === "") return "-";

  if (
    fieldName.toLowerCase().includes("time") ||
    fieldName.toLowerCase().includes("date")
  ) {
    return formatTime(value);
  }

  if (typeof value === "number") {
    const decimalMap = {
      temperature: 1,
      humidity: 1,
      voltage: 2,
      current: 2,
      pressure: 2,
      speed: 1,
    };

    const field = fieldName.toLowerCase();
    let decimals = 2;

    for (const [key, val] of Object.entries(decimalMap)) {
      if (field.includes(key)) {
        decimals = val;
        break;
      }
    }

    return value.toFixed(decimals);
  }

  return value;
};

// 判断是否为报警值
const getAlarmStatus = (row, fieldName) => {
  const value = row[fieldName];
  if (typeof value !== "number") return false;

  const column = tableColumns.value.find((col) => col.fieldName === fieldName);
  if (!column) return false;

  if (column.minValue !== undefined && value < column.minValue) return true;
  if (column.maxValue !== undefined && value > column.maxValue) return true;

  return false;
};

// 获取列宽
const getColumnWidth = (fieldName) => {
  const field = fieldName.toLowerCase();
  const widthMap = {
    time: 120,
    status: 100,
    temperature: 120,
    humidity: 120,
    voltage: 120,
    current: 120,
    pressure: 120,
  };

  for (const [key, width] of Object.entries(widthMap)) {
    if (field.includes(key)) return width;
  }

  return 130;
};

// 禁用未来日期
const disabledDate = (time) => {
  return time.getTime() > Date.now();
};

// 分页处理
const handleSizeChange = (size) => {
  pagination.value.pageSize = size;
  pagination.value.currentPage = 1;
  if (hasSearched.value) {
    search();
  }
};

const handleCurrentChange = (page) => {
  pagination.value.currentPage = page;
  if (hasSearched.value) {
    search();
  }
};

// 监听设备选择
watch(selectedDeviceId, (newVal) => {
  if (newVal) {
    const device = devices.value.find((d) => d.deviceId === newVal);
    if (device) {
      loadDeviceTable(device.deviceTable);
    }
  }
});

// 初始化加载设备表结构
watch(
  () => devices.value,
  (newVal) => {
    if (newVal.length > 0 && !selectedDeviceId.value) {
      selectedDeviceId.value = newVal[0].deviceId;
      selectedDevice.value = newVal[0];
      loadDeviceTable(newVal[0].deviceTable);
    }
  },
  { immediate: true }
);
</script>

<style scoped>
.table-container {
  margin-top: 20px;
  min-height: 300px;
}

.pagination-container {
  margin-top: 20px;
  display: flex;
  justify-content: flex-end;
}

.alarm-value {
  color: #f56c6c;
  font-weight: bold;
}

:deep(.el-table__cell) {
  padding: 8px 0;
}

:deep(.el-tag) {
  font-weight: bold;
}

:deep(.detail-message-box) {
  min-width: 400px;
  max-width: 80vw;
}

:deep(.el-alert) {
  margin-bottom: 15px;
}
</style>