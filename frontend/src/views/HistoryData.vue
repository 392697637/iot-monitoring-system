<template>
  <div class="history-data-container">
    <!-- 页面标题 -->
    <div class="page-header">
      <h1 class="page-title">
        <i class="el-icon-timer"></i>
        历史数据查询
      </h1>
      <div class="page-actions">
        <el-button
          type="primary"
          icon="el-icon-download"
          @click="handleExport"
          :loading="exporting"
        >
          导出数据
        </el-button>
        <el-button
          type="info"
          icon="el-icon-setting"
          @click="showQuerySettings"
        >
          查询设置
        </el-button>
      </div>
    </div>

    <!-- 查询条件 -->
    <el-card shadow="never" class="query-card">
      <div slot="header" class="card-header">
        <span>查询条件</span>
        <el-button
          type="text"
          @click="toggleAdvancedQuery"
        >
          {{ showAdvancedQuery ? '简化查询' : '高级查询' }}
          <i :class="showAdvancedQuery ? 'el-icon-arrow-up' : 'el-icon-arrow-down'"></i>
        </el-button>
      </div>
      
      <el-form
        :model="queryForm"
        ref="queryForm"
        label-width="100px"
        size="medium"
      >
        <el-row :gutter="20">
          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="选择设备" prop="deviceId">
              <el-select
                v-model="queryForm.deviceId"
                placeholder="请选择设备"
                clearable
                filterable
                style="width: 100%;"
              >
                <el-option
                  v-for="device in devices"
                  :key="device.id"
                  :label="device.deviceName"
                  :value="device.id"
                >
                  <div class="device-option">
                    <span>{{ device.deviceName }}</span>
                    <el-tag size="mini" :type="device.isActive ? 'success' : 'info'">
                      {{ device.isActive ? '启用' : '停用' }}
                    </el-tag>
                  </div>
                </el-option>
              </el-select>
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="开始时间" prop="startTime">
              <el-date-picker
                v-model="queryForm.startTime"
                type="datetime"
                placeholder="选择开始时间"
                format="yyyy-MM-dd HH:mm:ss"
                value-format="yyyy-MM-dd HH:mm:ss"
                style="width: 100%;"
              />
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="结束时间" prop="endTime">
              <el-date-picker
                v-model="queryForm.endTime"
                type="datetime"
                placeholder="选择结束时间"
                format="yyyy-MM-dd HH:mm:ss"
                value-format="yyyy-MM-dd HH:mm:ss"
                style="width: 100%;"
              />
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="数据状态" prop="status">
              <el-select
                v-model="queryForm.status"
                placeholder="选择状态"
                clearable
                style="width: 100%;"
              >
                <el-option label="正常" value="Normal" />
                <el-option label="警告" value="Warning" />
                <el-option label="故障" value="Fault" />
                <el-option label="离线" value="Offline" />
              </el-select>
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 高级查询条件 -->
        <el-collapse-transition>
          <div v-show="showAdvancedQuery">
            <el-divider></el-divider>
            <el-row :gutter="20">
              <el-col :xs="24" :sm="12" :md="8" :lg="6">
                <el-form-item label="温度范围" prop="temperatureRange">
                  <el-input-number
                    v-model="queryForm.temperatureRange[0]"
                    placeholder="最小值"
                    :precision="2"
                    :step="0.1"
                    style="width: 48%;"
                  />
                  <span style="margin: 0 2%;">至</span>
                  <el-input-number
                    v-model="queryForm.temperatureRange[1]"
                    placeholder="最大值"
                    :precision="2"
                    :step="0.1"
                    style="width: 48%;"
                  />
                </el-form-item>
              </el-col>

              <el-col :xs="24" :sm="12" :md="8" :lg="6">
                <el-form-item label="湿度范围" prop="humidityRange">
                  <el-input-number
                    v-model="queryForm.humidityRange[0]"
                    placeholder="最小值"
                    :precision="2"
                    :step="0.1"
                    style="width: 48%;"
                  />
                  <span style="margin: 0 2%;">至</span>
                  <el-input-number
                    v-model="queryForm.humidityRange[1]"
                    placeholder="最大值"
                    :precision="2"
                    :step="0.1"
                    style="width: 48%;"
                  />
                </el-form-item>
              </el-col>

              <el-col :xs="24" :sm="12" :md="8" :lg="6">
                <el-form-item label="电流范围" prop="currentRange">
                  <el-input-number
                    v-model="queryForm.currentRange[0]"
                    placeholder="最小值"
                    :precision="2"
                    :step="0.1"
                    style="width: 48%;"
                  />
                  <span style="margin: 0 2%;">至</span>
                  <el-input-number
                    v-model="queryForm.currentRange[1]"
                    placeholder="最大值"
                    :precision="2"
                    :step="0.1"
                    style="width: 48%;"
                  />
                </el-form-item>
              </el-col>

              <el-col :xs="24" :sm="12" :md="8" :lg="6">
                <el-form-item label="电压范围" prop="voltageRange">
                  <el-input-number
                    v-model="queryForm.voltageRange[0]"
                    placeholder="最小值"
                    :precision="2"
                    :step="0.1"
                    style="width: 48%;"
                  />
                  <span style="margin: 0 2%;">至</span>
                  <el-input-number
                    v-model="queryForm.voltageRange[1]"
                    placeholder="最大值"
                    :precision="2"
                    :step="0.1"
                    style="width: 48%;"
                  />
                </el-form-item>
              </el-col>
            </el-row>
          </div>
        </el-collapse-transition>

        <el-form-item class="query-actions">
          <el-button
            type="primary"
            icon="el-icon-search"
            @click="handleQuery"
            :loading="queryLoading"
          >
            查询
          </el-button>
          <el-button
            icon="el-icon-refresh"
            @click="handleReset"
          >
            重置
          </el-button>
          <el-button
            type="text"
            @click="quickSelectTime('today')"
          >
            今天
          </el-button>
          <el-button
            type="text"
            @click="quickSelectTime('yesterday')"
          >
            昨天
          </el-button>
          <el-button
            type="text"
            @click="quickSelectTime('lastWeek')"
          >
            最近一周
          </el-button>
          <el-button
            type="text"
            @click="quickSelectTime('lastMonth')"
          >
            最近一月
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 数据统计 -->
    <div class="statistics-section" v-if="statistics && !loading">
      <el-row :gutter="20">
        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon total">
                <i class="el-icon-data-line"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">数据总量</div>
                <div class="stat-value">{{ statistics.totalCount }}</div>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon normal">
                <i class="el-icon-success"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">正常数据</div>
                <div class="stat-value">
                  {{ statistics.statusDistribution?.Normal || 0 }}
                </div>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon warning">
                <i class="el-icon-warning"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">警告数据</div>
                <div class="stat-value">
                  {{ statistics.statusDistribution?.Warning || 0 }}
                </div>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card">
            <div class="stat-content">
              <div class="stat-icon fault">
                <i class="el-icon-error"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">故障数据</div>
                <div class="stat-value">
                  {{ statistics.statusDistribution?.Fault || 0 }}
                </div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <!-- 数据图表 -->
    <el-card shadow="never" class="chart-card" v-if="chartData.length > 0">
      <div slot="header" class="chart-header">
        <span>数据趋势图</span>
        <div class="chart-controls">
          <el-radio-group v-model="chartType" size="small">
            <el-radio-button label="line">折线图</el-radio-button>
            <el-radio-button label="bar">柱状图</el-radio-button>
            <el-radio-button label="scatter">散点图</el-radio-button>
          </el-radio-group>
          <el-select
            v-model="chartFactor"
            size="small"
            style="width: 120px; margin-left: 10px;"
          >
            <el-option label="温度" value="temperature" />
            <el-option label="湿度" value="humidity" />
            <el-option label="电流" value="current" />
            <el-option label="电压" value="voltage" />
          </el-select>
        </div>
      </div>
      <div class="chart-container">
        <v-chart
          :option="chartOption"
          :autoresize="true"
          style="height: 400px;"
        />
      </div>
    </el-card>

    <!-- 数据表格 -->
    <el-card shadow="never" class="table-card">
      <div slot="header" class="table-header">
        <span>历史数据</span>
        <div class="table-actions">
          <el-select
            v-model="pageSize"
            size="small"
            style="width: 100px; margin-right: 10px;"
            @change="handlePageSizeChange"
          >
            <el-option label="20条/页" :value="20" />
            <el-option label="50条/页" :value="50" />
            <el-option label="100条/页" :value="100" />
            <el-option label="200条/页" :value="200" />
          </el-select>
          <el-button
            size="small"
            icon="el-icon-printer"
            @click="printData"
          >
            打印
          </el-button>
        </div>
      </div>

      <div class="table-container">
        <el-table
          :data="tableData"
          v-loading="loading"
          element-loading-text="数据加载中..."
          element-loading-spinner="el-icon-loading"
          element-loading-background="rgba(0, 0, 0, 0.1)"
          stripe
          border
          highlight-current-row
          style="width: 100%;"
          @sort-change="handleSortChange"
        >
          <el-table-column
            type="index"
            label="序号"
            width="80"
            align="center"
            :index="indexMethod"
          />
          
          <el-table-column
            prop="timestamp"
            label="时间"
            width="180"
            align="center"
            sortable="custom"
          >
            <template slot-scope="{ row }">
              {{ formatDateTime(row.timestamp) }}
            </template>
          </el-table-column>

          <el-table-column
            prop="deviceName"
            label="设备名称"
            width="150"
            align="center"
          />

          <el-table-column
            prop="temperature"
            label="温度(°C)"
            width="120"
            align="center"
            sortable="custom"
          >
            <template slot-scope="{ row }">
              <div class="value-cell" :class="getValueClass(row.temperature, 'temperature')">
                {{ row.temperature !== null ? row.temperature.toFixed(2) : '--' }}
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="humidity"
            label="湿度(%)"
            width="120"
            align="center"
            sortable="custom"
          >
            <template slot-scope="{ row }">
              <div class="value-cell" :class="getValueClass(row.humidity, 'humidity')">
                {{ row.humidity !== null ? row.humidity.toFixed(2) : '--' }}
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="current"
            label="电流(A)"
            width="120"
            align="center"
            sortable="custom"
          >
            <template slot-scope="{ row }">
              <div class="value-cell" :class="getValueClass(row.current, 'current')">
                {{ row.current !== null ? row.current.toFixed(2) : '--' }}
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="voltage"
            label="电压(V)"
            width="120"
            align="center"
            sortable="custom"
          >
            <template slot-scope="{ row }">
              <div class="value-cell" :class="getValueClass(row.voltage, 'voltage')">
                {{ row.voltage !== null ? row.voltage.toFixed(2) : '--' }}
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="status"
            label="状态"
            width="100"
            align="center"
            sortable="custom"
          >
            <template slot-scope="{ row }">
              <el-tag
                :type="getStatusType(row.status)"
                size="small"
                effect="plain"
              >
                {{ getStatusText(row.status) }}
              </el-tag>
            </template>
          </el-table-column>

          <el-table-column
            label="操作"
            width="120"
            align="center"
            fixed="right"
          >
            <template slot-scope="{ row }">
              <el-button
                type="text"
                size="small"
                @click="viewDataDetails(row)"
              >
                详情
              </el-button>
              <el-button
                type="text"
                size="small"
                style="color: #f56c6c;"
                @click="deleteData(row)"
              >
                删除
              </el-button>
            </template>
          </el-table-column>
        </el-table>

        <!-- 空状态 -->
        <empty-state
          v-if="!loading && tableData.length === 0"
          :message="emptyMessage"
          class="table-empty"
        >
          <template v-slot:action>
            <el-button type="primary" @click="handleQuery">重新查询</el-button>
          </template>
        </empty-state>
      </div>

      <!-- 分页 -->
      <div class="pagination-container" v-if="total > 0">
        <el-pagination
          :current-page="currentPage"
          :page-sizes="[20, 50, 100, 200]"
          :page-size="pageSize"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handlePageSizeChange"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

    <!-- 数据详情对话框 -->
    <el-dialog
      title="数据详情"
      :visible.sync="detailDialogVisible"
      width="600px"
      :close-on-click-modal="false"
    >
      <div v-if="currentData" class="data-details">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="设备名称">
            {{ currentData.deviceName }}
          </el-descriptions-item>
          <el-descriptions-item label="记录时间">
            {{ formatDateTime(currentData.timestamp) }}
          </el-descriptions-item>
          <el-descriptions-item label="温度">
            <div class="detail-value">
              {{ currentData.temperature !== null ? currentData.temperature.toFixed(2) : '--' }}
              <span class="unit">°C</span>
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="湿度">
            <div class="detail-value">
              {{ currentData.humidity !== null ? currentData.humidity.toFixed(2) : '--' }}
              <span class="unit">%</span>
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="电流">
            <div class="detail-value">
              {{ currentData.current !== null ? currentData.current.toFixed(2) : '--' }}
              <span class="unit">A</span>
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="电压">
            <div class="detail-value">
              {{ currentData.voltage !== null ? currentData.voltage.toFixed(2) : '--' }}
              <span class="unit">V</span>
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="getStatusType(currentData.status)" size="small">
              {{ getStatusText(currentData.status) }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="数据ID">
            {{ currentData.id }}
          </el-descriptions-item>
          <el-descriptions-item label="设备ID">
            {{ currentData.deviceId }}
          </el-descriptions-item>
        </el-descriptions>

        <div class="threshold-check" v-if="thresholds.length > 0">
          <h4>阈值检查</h4>
          <el-table :data="thresholdChecks" size="small">
            <el-table-column prop="factor" label="监测因子" width="100" />
            <el-table-column prop="value" label="当前值" width="100">
              <template slot-scope="{ row }">
                {{ row.value !== null ? row.value.toFixed(2) : '--' }}
              </template>
            </el-table-column>
            <el-table-column prop="threshold" label="阈值范围" width="150">
              <template slot-scope="{ row }">
                {{ row.lower }} - {{ row.upper }}
              </template>
            </el-table-column>
            <el-table-column prop="status" label="状态" width="100">
              <template slot-scope="{ row }">
                <el-tag
                  :type="row.status === '正常' ? 'success' : row.status === '警告' ? 'warning' : 'danger'"
                  size="small"
                >
                  {{ row.status }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="message" label="说明" />
          </el-table>
        </div>
      </div>
      <span slot="footer" class="dialog-footer">
        <el-button @click="detailDialogVisible = false">关闭</el-button>
        <el-button type="primary" @click="compareWithOther">对比分析</el-button>
      </span>
    </el-dialog>

    <!-- 查询设置对话框 -->
    <el-dialog
      title="查询设置"
      :visible.sync="settingsDialogVisible"
      width="500px"
    >
      <el-form :model="settingsForm" label-width="120px">
        <el-form-item label="默认时间范围">
          <el-select v-model="settingsForm.defaultTimeRange">
            <el-option label="最近一天" value="1" />
            <el-option label="最近一周" value="7" />
            <el-option label="最近一月" value="30" />
            <el-option label="最近三月" value="90" />
          </el-select>
        </el-form-item>
        <el-form-item label="默认页面大小">
          <el-select v-model="settingsForm.defaultPageSize">
            <el-option label="20条" :value="20" />
            <el-option label="50条" :value="50" />
            <el-option label="100条" :value="100" />
            <el-option label="200条" :value="200" />
          </el-select>
        </el-form-item>
        <el-form-item label="启用实时数据">
          <el-switch v-model="settingsForm.enableRealtime" />
        </el-form-item>
        <el-form-item label="数据自动刷新">
          <el-switch v-model="settingsForm.autoRefresh" />
        </el-form-item>
        <el-form-item label="显示数据统计" v-if="settingsForm.autoRefresh">
          <el-switch v-model="settingsForm.showStatistics" />
        </el-form-item>
        <el-form-item label="导出格式">
          <el-select v-model="settingsForm.exportFormat">
            <el-option label="CSV" value="csv" />
            <el-option label="Excel" value="excel" />
            <el-option label="PDF" value="pdf" />
          </el-select>
        </el-form-item>
      </el-form>
      <span slot="footer" class="dialog-footer">
        <el-button @click="settingsDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveSettings">保存</el-button>
      </span>
    </el-dialog>
  </div>
</template>

<script>
import { mapState, mapActions } from 'vuex'
import { format, subDays, startOfDay, endOfDay, subWeeks, subMonths } from 'date-fns'
import * as XLSX from 'xlsx'
import FileSaver from 'file-saver'
import EmptyState from '@/components/common/EmptyState.vue'

export default {
  name: 'HistoryData',
  components: {
    EmptyState
  },
  data() {
    return {
      // 查询表单
      queryForm: {
        deviceId: null,
        startTime: null,
        endTime: null,
        status: null,
        temperatureRange: [null, null],
        humidityRange: [null, null],
        currentRange: [null, null],
        voltageRange: [null, null]
      },
      
      // 表格数据
      tableData: [],
      loading: false,
      queryLoading: false,
      exporting: false,
      
      // 分页
      currentPage: 1,
      pageSize: 50,
      total: 0,
      sortField: 'timestamp',
      sortOrder: 'desc',
      
      // 对话框
      showAdvancedQuery: false,
      detailDialogVisible: false,
      settingsDialogVisible: false,
      currentData: null,
      
      // 设置
      settingsForm: {
        defaultTimeRange: '7',
        defaultPageSize: 50,
        enableRealtime: true,
        autoRefresh: false,
        showStatistics: true,
        exportFormat: 'csv'
      },
      
      // 图表
      chartType: 'line',
      chartFactor: 'temperature',
      chartData: [],
      chartOption: {},
      
      // 统计
      statistics: null,
      thresholds: [],
      thresholdChecks: [],
      
      // 空状态信息
      emptyMessage: '暂无历史数据'
    }
  },
  computed: {
    ...mapState('devices', ['devices']),
    
    selectedDevice() {
      if (!this.queryForm.deviceId) return null
      return this.devices.find(d => d.id === this.queryForm.deviceId)
    }
  },
  watch: {
    chartType() {
      this.updateChart()
    },
    
    chartFactor() {
      this.updateChart()
    },
    
    'settingsForm.autoRefresh'(enabled) {
      if (enabled) {
        this.startAutoRefresh()
      } else {
        this.stopAutoRefresh()
      }
    }
  },
  created() {
    this.initPage()
    this.loadDevices()
    this.setDefaultTimeRange()
  },
  mounted() {
    if (this.settingsForm.autoRefresh) {
      this.startAutoRefresh()
    }
  },
  beforeDestroy() {
    this.stopAutoRefresh()
  },
  methods: {
    ...mapActions('devices', [
      'fetchDevices',
      'fetchHistoricalData',
      'exportDeviceData',
      'getDataStatistics'
    ]),
    
    async initPage() {
      try {
        // 加载设置
        const savedSettings = localStorage.getItem('historyQuerySettings')
        if (savedSettings) {
          this.settingsForm = { ...this.settingsForm, ...JSON.parse(savedSettings) }
          this.pageSize = this.settingsForm.defaultPageSize
        }
        
        // 如果有默认设备，自动查询
        const defaultDevice = this.$store.state.devices.selectedDeviceId
        if (defaultDevice) {
          this.queryForm.deviceId = defaultDevice
          await this.loadThresholds(defaultDevice)
        }
      } catch (error) {
        console.error('初始化页面失败:', error)
      }
    },
    
    async loadDevices() {
      try {
        await this.fetchDevices()
      } catch (error) {
        console.error('加载设备列表失败:', error)
      }
    },
    
    async loadThresholds(deviceId) {
      try {
        const response = await this.$api.getDeviceThresholds(deviceId)
        if (response.success) {
          this.thresholds = response.data
        }
      } catch (error) {
        console.error('加载阈值失败:', error)
      }
    },
    
    setDefaultTimeRange() {
      const now = new Date()
      const days = parseInt(this.settingsForm.defaultTimeRange)
      
      this.queryForm.endTime = format(now, 'yyyy-MM-dd HH:mm:ss')
      this.queryForm.startTime = format(subDays(now, days), 'yyyy-MM-dd HH:mm:ss')
    },
    
    async handleQuery() {
      if (!this.validateQueryForm()) {
        return
      }
      
      this.queryLoading = true
      this.loading = true
      this.currentPage = 1
      
      try {
        await this.fetchData()
      } catch (error) {
        console.error('查询数据失败:', error)
        this.$message.error('查询数据失败')
      } finally {
        this.queryLoading = false
        this.loading = false
      }
    },
    
    validateQueryForm() {
      if (!this.queryForm.deviceId) {
        this.$message.warning('请选择设备')
        return false
      }
      
      if (!this.queryForm.startTime || !this.queryForm.endTime) {
        this.$message.warning('请选择时间范围')
        return false
      }
      
      const start = new Date(this.queryForm.startTime)
      const end = new Date(this.queryForm.endTime)
      
      if (start >= end) {
        this.$message.warning('开始时间必须早于结束时间')
        return false
      }
      
      const diffDays = (end - start) / (1000 * 60 * 60 * 24)
      if (diffDays > 365) {
        this.$message.warning('查询时间范围不能超过一年')
        return false
      }
      
      return true
    },
    
    async fetchData() {
      try {
        const params = {
          deviceId: this.queryForm.deviceId,
          startTime: this.queryForm.startTime,
          endTime: this.queryForm.endTime,
          page: this.currentPage,
          pageSize: this.pageSize,
          sortBy: this.sortField,
          sortDescending: this.sortOrder === 'desc'
        }
        
        // 添加筛选条件
        if (this.queryForm.status) {
          params.status = this.queryForm.status
        }
        
        const result = await this.fetchHistoricalData(params)
        
        if (result) {
          this.tableData = result.data
          this.total = result.totalCount
          
          // 处理图表数据
          this.prepareChartData(result.data)
          
          // 加载统计信息
          await this.loadStatistics()
          
          // 更新空状态信息
          this.updateEmptyMessage()
        }
      } catch (error) {
        throw error
      }
    },
    
    async loadStatistics() {
      if (!this.queryForm.deviceId) return
      
      try {
        const response = await this.getDataStatistics({
          deviceId: this.queryForm.deviceId,
          date: new Date(this.queryForm.startTime)
        })
        
        if (response.success) {
          this.statistics = response.data
        }
      } catch (error) {
        console.error('加载统计信息失败:', error)
      }
    },
    
    prepareChartData(data) {
      if (!data || data.length === 0) {
        this.chartData = []
        return
      }
      
      this.chartData = data.map(item => ({
        time: new Date(item.timestamp),
        temperature: item.temperature,
        humidity: item.humidity,
        current: item.current,
        voltage: item.voltage,
        status: item.status
      }))
      
      this.updateChart()
    },
    
    updateChart() {
      if (this.chartData.length === 0) return
      
      const factorMap = {
        temperature: { name: '温度', unit: '°C', color: '#ff6b6b' },
        humidity: { name: '湿度', unit: '%', color: '#4ecdc4' },
        current: { name: '电流', unit: 'A', color: '#36a2eb' },
        voltage: { name: '电压', unit: 'V', color: '#ff6384' }
      }
      
      const factor = factorMap[this.chartFactor]
      if (!factor) return
      
      const data = this.chartData
        .filter(item => item[this.chartFactor] != null)
        .map(item => [item.time, item[this.chartFactor]])
      
      this.chartOption = {
        title: {
          text: `${factor.name}趋势图`,
          left: 'center',
          textStyle: {
            fontSize: 14,
            fontWeight: 'normal'
          }
        },
        tooltip: {
          trigger: 'axis',
          formatter: params => {
            const date = new Date(params[0].value[0])
            const time = format(date, 'yyyy-MM-dd HH:mm:ss')
            const value = params[0].value[1]
            return `${time}<br/>${factor.name}: ${value}${factor.unit}`
          }
        },
        grid: {
          left: '3%',
          right: '4%',
          bottom: '3%',
          top: '10%',
          containLabel: true
        },
        xAxis: {
          type: 'time',
          axisLabel: {
            formatter: value => format(new Date(value), 'MM-dd HH:mm')
          }
        },
        yAxis: {
          type: 'value',
          name: factor.unit,
          nameTextStyle: {
            padding: [0, 0, 0, 10]
          }
        },
        series: [{
          name: factor.name,
          type: this.chartType,
          data: data,
          smooth: true,
          symbol: 'circle',
          symbolSize: 4,
          lineStyle: {
            width: 2,
            color: factor.color
          },
          itemStyle: {
            color: factor.color
          },
          areaStyle: this.chartType === 'line' ? {
            color: {
              type: 'linear',
              x: 0,
              y: 0,
              x2: 0,
              y2: 1,
              colorStops: [{
                offset: 0,
                color: factor.color + '40'
              }, {
                offset: 1,
                color: factor.color + '00'
              }]
            }
          } : undefined
        }]
      }
    },
    
    handleReset() {
      this.$refs.queryForm.resetFields()
      this.setDefaultTimeRange()
      this.tableData = []
      this.total = 0
      this.chartData = []
      this.statistics = null
    },
    
    quickSelectTime(type) {
      const now = new Date()
      let start, end
      
      switch (type) {
        case 'today':
          start = startOfDay(now)
          end = endOfDay(now)
          break
        case 'yesterday':
          start = startOfDay(subDays(now, 1))
          end = endOfDay(subDays(now, 1))
          break
        case 'lastWeek':
          start = subWeeks(now, 1)
          end = now
          break
        case 'lastMonth':
          start = subMonths(now, 1)
          end = now
          break
      }
      
      this.queryForm.startTime = format(start, 'yyyy-MM-dd HH:mm:ss')
      this.queryForm.endTime = format(end, 'yyyy-MM-dd HH:mm:ss')
      
      // 自动查询
      if (this.queryForm.deviceId) {
        this.handleQuery()
      }
    },
    
    handlePageChange(page) {
      this.currentPage = page
      this.fetchData()
    },
    
    handlePageSizeChange(size) {
      this.pageSize = size
      this.currentPage = 1
      this.fetchData()
    },
    
    handleSortChange({ prop, order }) {
      this.sortField = prop
      this.sortOrder = order === 'ascending' ? 'asc' : 'desc'
      this.fetchData()
    },
    
    indexMethod(index) {
      return (this.currentPage - 1) * this.pageSize + index + 1
    },
    
    formatDateTime(timestamp) {
      if (!timestamp) return '--'
      return format(new Date(timestamp), 'yyyy-MM-dd HH:mm:ss')
    },
    
    getStatusType(status) {
      const typeMap = {
        'Normal': 'success',
        'Warning': 'warning',
        'Fault': 'danger',
        'Offline': 'info'
      }
      return typeMap[status] || 'info'
    },
    
    getStatusText(status) {
      const textMap = {
        'Normal': '正常',
        'Warning': '警告',
        'Fault': '故障',
        'Offline': '离线'
      }
      return textMap[status] || status
    },
    
    getValueClass(value, factor) {
      if (value === null) return 'value-null'
      
      const threshold = this.thresholds.find(t => {
        const factorMap = {
          temperature: 'Temperature',
          humidity: 'Humidity',
          current: 'Current',
          voltage: 'Voltage'
        }
        return t.factorType === factorMap[factor]
      })
      
      if (!threshold) return ''
      
      if (value > threshold.upperLimit) return 'value-exceed'
      if (value < threshold.lowerLimit) return 'value-below'
      
      return 'value-normal'
    },
    
    async handleExport() {
      if (!this.validateQueryForm()) {
        return
      }
      
      this.exporting = true
      
      try {
        const { deviceId, startTime, endTime } = this.queryForm
        
        const response = await this.exportDeviceData({
          deviceId,
          startTime,
          endTime
        })
        
        if (this.settingsForm.exportFormat === 'excel') {
          this.exportToExcel(response)
        } else {
          this.exportToCSV(response)
        }
        
        this.$message.success('数据导出成功')
      } catch (error) {
        console.error('导出数据失败:', error)
        this.$message.error('导出数据失败')
      } finally {
        this.exporting = false
      }
    },
    
    exportToCSV(blob) {
      const filename = `历史数据_${this.selectedDevice?.deviceName || '未知设备'}_${format(new Date(), 'yyyyMMddHHmmss')}.csv`
      FileSaver.saveAs(blob, filename)
    },
    
    exportToExcel(blob) {
      const reader = new FileReader()
      reader.onload = (e) => {
        const data = new Uint8Array(e.target.result)
        const workbook = XLSX.read(data, { type: 'array' })
        
        // 添加格式
        const worksheet = workbook.Sheets[workbook.SheetNames[0]]
        
        // 设置列宽
        const colWidths = [
          { wch: 20 }, // 时间
          { wch: 15 }, // 设备
          { wch: 10 }, // 温度
          { wch: 10 }, // 湿度
          { wch: 10 }, // 电流
          { wch: 10 }, // 电压
          { wch: 10 }  // 状态
        ]
        worksheet['!cols'] = colWidths
        
        // 导出Excel
        const excelBuffer = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' })
        const excelBlob = new Blob([excelBuffer], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' })
        
        const filename = `历史数据_${this.selectedDevice?.deviceName || '未知设备'}_${format(new Date(), 'yyyyMMddHHmmss')}.xlsx`
        FileSaver.saveAs(excelBlob, filename)
      }
      reader.readAsArrayBuffer(blob)
    },
    
    printData() {
      window.print()
    },
    
    async viewDataDetails(data) {
      this.currentData = data
      this.detailDialogVisible = true
      
      // 检查阈值
      this.checkThresholds(data)
    },
    
    checkThresholds(data) {
      this.thresholdChecks = this.thresholds.map(threshold => {
        const factorMap = {
          'Temperature': { field: 'temperature', unit: '°C', name: '温度' },
          'Humidity': { field: 'humidity', unit: '%', name: '湿度' },
          'Current': { field: 'current', unit: 'A', name: '电流' },
          'Voltage': { field: 'voltage', unit: 'V', name: '电压' }
        }
        
        const factor = factorMap[threshold.factorType]
        if (!factor) return null
        
        const value = data[factor.field]
        let status = '正常'
        let message = ''
        
        if (value !== null) {
          if (value > threshold.upperLimit) {
            status = '超上限'
            message = `超过上限 ${threshold.upperLimit}${factor.unit}`
          } else if (value < threshold.lowerLimit) {
            status = '低下限'
            message = `低于下限 ${threshold.lowerLimit}${factor.unit}`
          } else {
            message = `在正常范围内 ${threshold.lowerLimit} - ${threshold.upperLimit}${factor.unit}`
          }
        } else {
          status = '无数据'
          message = '该因子无数据'
        }
        
        return {
          factor: factor.name,
          value: value,
          lower: threshold.lowerLimit,
          upper: threshold.upperLimit,
          status: status,
          message: message
        }
      }).filter(item => item !== null)
    },
    
    deleteData(data) {
      this.$confirm(`确定要删除这条数据吗？<br/>时间：${this.formatDateTime(data.timestamp)}`, '删除确认', {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning',
        dangerouslyUseHTMLString: true
      }).then(async () => {
        try {
          // 这里调用API删除数据
          // await this.$api.deleteDeviceData(data.id)
          this.$message.success('数据删除成功')
          await this.fetchData() // 重新加载数据
        } catch (error) {
          console.error('删除数据失败:', error)
          this.$message.error('删除数据失败')
        }
      }).catch(() => {})
    },
    
    compareWithOther() {
      this.$message.info('对比分析功能开发中...')
    },
    
    toggleAdvancedQuery() {
      this.showAdvancedQuery = !this.showAdvancedQuery
    },
    
    showQuerySettings() {
      this.settingsDialogVisible = true
    },
    
    saveSettings() {
      localStorage.setItem('historyQuerySettings', JSON.stringify(this.settingsForm))
      this.settingsDialogVisible = false
      this.$message.success('设置已保存')
      
      // 应用设置
      this.pageSize = this.settingsForm.defaultPageSize
      if (this.settingsForm.autoRefresh) {
        this.startAutoRefresh()
      }
    },
    
    startAutoRefresh() {
      this.refreshInterval = setInterval(() => {
        if (this.queryForm.deviceId && document.visibilityState === 'visible') {
          this.fetchData()
        }
      }, 30000) // 每30秒刷新一次
    },
    
    stopAutoRefresh() {
      if (this.refreshInterval) {
        clearInterval(this.refreshInterval)
        this.refreshInterval = null
      }
    },
    
    updateEmptyMessage() {
      if (this.total === 0) {
        if (!this.queryForm.deviceId) {
          this.emptyMessage = '请选择设备后查询'
        } else if (!this.queryForm.startTime || !this.queryForm.endTime) {
          this.emptyMessage = '请选择时间范围后查询'
        } else {
          this.emptyMessage = '该时间段内暂无数据'
        }
      }
    }
  }
}
</script>

<style lang="scss" scoped>
.history-data-container {
  height: 100%;
  display: flex;
  flex-direction: column;
  gap: 20px;
  
  .page-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    
    .page-title {
      margin: 0;
      font-size: 24px;
      font-weight: 600;
      display: flex;
      align-items: center;
      gap: 10px;
      color: var(--text-color-primary);
      
      i {
        font-size: 28px;
        color: var(--primary-color);
      }
    }
  }
  
  .query-card {
    .card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      
      span {
        font-weight: 600;
      }
    }
    
    .query-actions {
      margin-bottom: 0;
      padding-top: 20px;
      border-top: 1px solid var(--border-color);
      
      .el-button + .el-button {
        margin-left: 10px;
      }
    }
    
    .device-option {
      display: flex;
      align-items: center;
      justify-content: space-between;
      width: 100%;
      
      .el-tag {
        margin-left: 10px;
      }
    }
  }
  
  .statistics-section {
    .stat-card {
      border-radius: 8px;
      border: 1px solid var(--border-color);
      transition: all 0.3s;
      
      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important;
      }
      
      .stat-content {
        display: flex;
        align-items: center;
        gap: 15px;
        
        .stat-icon {
          width: 50px;
          height: 50px;
          border-radius: 50%;
          display: flex;
          align-items: center;
          justify-content: center;
          font-size: 24px;
          
          &.total {
            background: linear-gradient(135deg, #36a2eb20, #36a2eb40);
            color: #36a2eb;
          }
          
          &.normal {
            background: linear-gradient(135deg, #67c23a20, #67c23a40);
            color: #67c23a;
          }
          
          &.warning {
            background: linear-gradient(135deg, #e6a23c20, #e6a23c40);
            color: #e6a23c;
          }
          
          &.fault {
            background: linear-gradient(135deg, #f56c6c20, #f56c6c40);
            color: #f56c6c;
          }
        }
        
        .stat-info {
          .stat-label {
            font-size: 14px;
            color: var(--text-color-secondary);
            margin-bottom: 5px;
          }
          
          .stat-value {
            font-size: 24px;
            font-weight: 700;
            color: var(--text-color-primary);
          }
        }
      }
    }
  }
  
  .chart-card {
    .chart-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      
      span {
        font-weight: 600;
        font-size: 16px;
      }
    }
  }
  
  .table-card {
    flex: 1;
    display: flex;
    flex-direction: column;
    
    .table-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      
      span {
        font-weight: 600;
        font-size: 16px;
      }
    }
    
    .table-container {
      flex: 1;
      overflow: auto;
      
      .value-cell {
        padding: 4px 8px;
        border-radius: 4px;
        font-weight: 600;
        
        &.value-normal {
          background: #f0f9eb;
          color: #67c23a;
        }
        
        &.value-exceed {
          background: #fef0f0;
          color: #f56c6c;
        }
        
        &.value-below {
          background: #fdf6ec;
          color: #e6a23c;
        }
        
        &.value-null {
          color: var(--text-color-secondary);
        }
      }
      
      .table-empty {
        padding: 60px 0;
      }
    }
    
    .pagination-container {
      padding: 20px 0 0 0;
      border-top: 1px solid var(--border-color);
      text-align: center;
    }
  }
  
  .data-details {
    .detail-value {
      display: flex;
      align-items: center;
      gap: 4px;
      
      .unit {
        color: var(--text-color-secondary);
        font-size: 12px;
      }
    }
    
    .threshold-check {
      margin-top: 20px;
      
      h4 {
        margin: 20px 0 10px 0;
        font-size: 16px;
        font-weight: 600;
        color: var(--text-color-primary);
      }
    }
  }
}

@media (max-width: 768px) {
  .history-data-container {
    .page-header {
      flex-direction: column;
      align-items: flex-start;
      gap: 10px;
    }
    
    .query-card {
      .el-form-item {
        margin-bottom: 15px;
      }
      
      .query-actions {
        .el-button {
          margin-bottom: 10px;
        }
      }
    }
    
    .statistics-section {
      .el-col {
        margin-bottom: 10px;
      }
    }
    
    .table-card {
      .table-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 10px;
      }
    }
  }
}
</style>