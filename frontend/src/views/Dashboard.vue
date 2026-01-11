<template>
  <div class="dashboard-container">
    <!-- 页面标题 -->
    <div class="page-header">
      <h1 class="page-title">
        <i class="el-icon-monitor"></i>
        实时监控
      </h1>
      <div class="page-actions">
        <el-button
          type="primary"
          icon="el-icon-refresh"
          @click="refreshData"
          :loading="isRefreshing"
        >
          刷新
        </el-button>
        <el-button
          :type="autoRefresh ? 'success' : 'info'"
          :icon="autoRefresh ? 'el-icon-video-pause' : 'el-icon-video-play'"
          @click="toggleAutoRefresh"
        >
          {{ autoRefresh ? '暂停刷新' : '开始刷新' }}
        </el-button>
        <el-button
          type="warning"
          icon="el-icon-setting"
          @click="showSettings"
        >
          监控设置
        </el-button>
      </div>
    </div>

    <!-- 设备选择 -->
    <div class="device-selector-section">
      <el-card shadow="never">
        <div slot="header" class="card-header">
          <span>设备选择</span>
          <el-tag type="info">{{ devices.length }} 台设备</el-tag>
        </div>
        <div class="device-list">
          <el-radio-group v-model="selectedDeviceId" @change="handleDeviceChange">
            <el-radio-button
              v-for="device in activeDevices"
              :key="device.id"
              :label="device.id"
            >
              <div class="device-item">
                <div class="device-info">
                  <div class="device-name">{{ device.deviceName }}</div>
                  <div class="device-code">{{ device.deviceCode }}</div>
                </div>
                <div class="device-status">
                  <el-tag
                    :type="getDeviceStatusType(device)"
                    size="small"
                  >
                    {{ getDeviceStatusText(device) }}
                  </el-tag>
                </div>
              </div>
            </el-radio-button>
          </el-radio-group>
        </div>
      </el-card>
    </div>

    <!-- 实时数据仪表盘 -->
    <div class="dashboard-content">
      <!-- 实时数据卡片 -->
      <div class="real-time-cards">
        <el-row :gutter="20">
          <el-col :xs="24" :sm="12" :md="6" :lg="6">
            <el-card class="data-card" shadow="hover">
              <div class="card-content">
                <div class="card-icon temperature">
                  <i class="el-icon-thermometer"></i>
                </div>
                <div class="card-info">
                  <div class="card-label">温度</div>
                  <div class="card-value">
                    {{ currentData.temperature || '--' }}
                    <span class="card-unit">°C</span>
                  </div>
                  <div class="card-trend">
                    <trend-indicator :value="temperatureTrend" />
                  </div>
                </div>
              </div>
              <div class="card-footer">
                <threshold-indicator
                  :value="currentData.temperature"
                  :lower="temperatureThreshold?.lower"
                  :upper="temperatureThreshold?.upper"
                />
              </div>
            </el-card>
          </el-col>

          <el-col :xs="24" :sm="12" :md="6" :lg="6">
            <el-card class="data-card" shadow="hover">
              <div class="card-content">
                <div class="card-icon humidity">
                  <i class="el-icon-watermelon"></i>
                </div>
                <div class="card-info">
                  <div class="card-label">湿度</div>
                  <div class="card-value">
                    {{ currentData.humidity || '--' }}
                    <span class="card-unit">%</span>
                  </div>
                  <div class="card-trend">
                    <trend-indicator :value="humidityTrend" />
                  </div>
                </div>
              </div>
              <div class="card-footer">
                <threshold-indicator
                  :value="currentData.humidity"
                  :lower="humidityThreshold?.lower"
                  :upper="humidityThreshold?.upper"
                />
              </div>
            </el-card>
          </el-col>

          <el-col :xs="24" :sm="12" :md="6" :lg="6">
            <el-card class="data-card" shadow="hover">
              <div class="card-content">
                <div class="card-icon current">
                  <i class="el-icon-lightning"></i>
                </div>
                <div class="card-info">
                  <div class="card-label">电流</div>
                  <div class="card-value">
                    {{ currentData.current || '--' }}
                    <span class="card-unit">A</span>
                  </div>
                  <div class="card-trend">
                    <trend-indicator :value="currentTrend" />
                  </div>
                </div>
              </div>
              <div class="card-footer">
                <threshold-indicator
                  :value="currentData.current"
                  :lower="currentThreshold?.lower"
                  :upper="currentThreshold?.upper"
                />
              </div>
            </el-card>
          </el-col>

          <el-col :xs="24" :sm="12" :md="6" :lg="6">
            <el-card class="data-card" shadow="hover">
              <div class="card-content">
                <div class="card-icon voltage">
                  <i class="el-icon-flash"></i>
                </div>
                <div class="card-info">
                  <div class="card-label">电压</div>
                  <div class="card-value">
                    {{ currentData.voltage || '--' }}
                    <span class="card-unit">V</span>
                  </div>
                  <div class="card-trend">
                    <trend-indicator :value="voltageTrend" />
                  </div>
                </div>
              </div>
              <div class="card-footer">
                <threshold-indicator
                  :value="currentData.voltage"
                  :lower="voltageThreshold?.lower"
                  :upper="voltageThreshold?.upper"
                />
              </div>
            </el-card>
          </el-col>
        </el-row>
      </div>

      <!-- 实时图表 -->
      <div class="real-time-charts">
        <el-row :gutter="20">
          <el-col :xs="24" :lg="12">
            <el-card class="chart-card" shadow="never">
              <div slot="header" class="chart-header">
                <span>温度变化趋势</span>
                <div class="chart-controls">
                  <el-select
                    v-model="chartTimeRange"
                    size="small"
                    style="width: 120px;"
                  >
                    <el-option label="最近15分钟" value="15"></el-option>
                    <el-option label="最近30分钟" value="30"></el-option>
                    <el-option label="最近1小时" value="60"></el-option>
                    <el-option label="最近3小时" value="180"></el-option>
                  </el-select>
                </div>
              </div>
              <div class="chart-container">
                <v-chart
                  v-if="temperatureChartData.length > 0"
                  :option="temperatureChartOption"
                  :autoresize="true"
                  style="height: 300px;"
                />
                <empty-state v-else message="暂无温度数据" />
              </div>
            </el-card>
          </el-col>

          <el-col :xs="24" :lg="12">
            <el-card class="chart-card" shadow="never">
              <div slot="header" class="chart-header">
                <span>湿度变化趋势</span>
                <div class="chart-controls">
                  <el-select
                    v-model="chartTimeRange"
                    size="small"
                    style="width: 120px;"
                  >
                    <el-option label="最近15分钟" value="15"></el-option>
                    <el-option label="最近30分钟" value="30"></el-option>
                    <el-option label="最近1小时" value="60"></el-option>
                    <el-option label="最近3小时" value="180"></el-option>
                  </el-select>
                </div>
              </div>
              <div class="chart-container">
                <v-chart
                  v-if="humidityChartData.length > 0"
                  :option="humidityChartOption"
                  :autoresize="true"
                  style="height: 300px;"
                />
                <empty-state v-else message="暂无湿度数据" />
              </div>
            </el-card>
          </el-col>

          <el-col :xs="24" :lg="12">
            <el-card class="chart-card" shadow="never">
              <div slot="header" class="chart-header">
                <span>电流电压趋势</span>
                <div class="chart-controls">
                  <el-select
                    v-model="chartTimeRange"
                    size="small"
                    style="width: 120px;"
                  >
                    <el-option label="最近15分钟" value="15"></el-option>
                    <el-option label="最近30分钟" value="30"></el-option>
                    <el-option label="最近1小时" value="60"></el-option>
                    <el-option label="最近3小时" value="180"></el-option>
                  </el-select>
                </div>
              </div>
              <div class="chart-container">
                <v-chart
                  v-if="currentVoltageChartData.length > 0"
                  :option="currentVoltageChartOption"
                  :autoresize="true"
                  style="height: 300px;"
                />
                <empty-state v-else message="暂无电流电压数据" />
              </div>
            </el-card>
          </el-col>

          <el-col :xs="24" :lg="12">
            <el-card class="chart-card" shadow="never">
              <div slot="header" class="chart-header">
                <span>设备状态分布</span>
                <div class="chart-controls">
                  <el-select
                    v-model="chartTimeRange"
                    size="small"
                    style="width: 120px;"
                  >
                    <el-option label="最近15分钟" value="15"></el-option>
                    <el-option label="最近30分钟" value="30"></el-option>
                    <el-option label="最近1小时" value="60"></el-option>
                    <el-option label="最近3小时" value="180"></el-option>
                  </el-select>
                </div>
              </div>
              <div class="chart-container">
                <v-chart
                  v-if="statusChartData.length > 0"
                  :option="statusChartOption"
                  :autoresize="true"
                  style="height: 300px;"
                />
                <empty-state v-else message="暂无状态数据" />
              </div>
            </el-card>
          </el-col>
        </el-row>
      </div>

      <!-- 实时报警面板 -->
      <div class="alarm-panel">
        <el-card shadow="never">
          <div slot="header" class="card-header">
            <span>实时报警</span>
            <div class="header-actions">
              <el-tag type="danger" v-if="activeAlarms.length > 0">
                {{ activeAlarms.length }} 条报警
              </el-tag>
              <el-button
                type="text"
                @click="muteAllAlarms"
                v-if="activeAlarms.length > 0"
              >
                {{ isAlarmMuted ? '启用报警音' : '静音所有' }}
              </el-button>
              <el-button
                type="text"
                @click="acknowledgeAllAlarms"
                v-if="activeAlarms.length > 0"
              >
                全部确认
              </el-button>
              <el-button
                type="text"
                @click="$router.push('/alarms')"
              >
                查看所有报警
              </el-button>
            </div>
          </div>
          <div class="alarm-list">
            <div v-if="activeAlarms.length === 0" class="no-alarms">
              <empty-state message="当前无报警" />
            </div>
            <div v-else class="alarms-container">
              <div
                v-for="alarm in activeAlarms"
                :key="alarm.id"
                class="alarm-item"
                :class="getAlarmSeverityClass(alarm)"
              >
                <div class="alarm-content">
                  <div class="alarm-header">
                    <div class="alarm-title">
                      <i class="el-icon-warning"></i>
                      {{ alarm.deviceName }} - {{ alarm.factorName }}
                    </div>
                    <div class="alarm-time">
                      {{ formatTime(alarm.timestamp) }}
                    </div>
                  </div>
                  <div class="alarm-body">
                    <div class="alarm-message">{{ alarm.message }}</div>
                    <div class="alarm-value">
                      <el-tag
                        size="small"
                        :type="alarm.limitType === 'Upper' ? 'danger' : 'warning'"
                      >
                        {{ alarm.limitType === 'Upper' ? '超过上限' : '低于下限' }}:
                        {{ alarm.value }}
                      </el-tag>
                    </div>
                  </div>
                  <div class="alarm-footer">
                    <div class="alarm-duration">
                      持续: {{ formatDuration(alarm.duration) }}
                    </div>
                    <div class="alarm-actions">
                      <el-button
                        size="mini"
                        type="primary"
                        @click="acknowledgeAlarm(alarm.id)"
                      >
                        确认
                      </el-button>
                      <el-button
                        size="mini"
                        @click="viewAlarmDetails(alarm)"
                      >
                        详情
                      </el-button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </el-card>
      </div>
    </div>

    <!-- 监控设置对话框 -->
    <el-dialog
      title="监控设置"
      :visible.sync="settingsDialogVisible"
      width="500px"
    >
      <el-form :model="settingsForm" label-width="120px">
        <el-form-item label="自动刷新">
          <el-switch
            v-model="settingsForm.autoRefresh"
            active-text="开启"
            inactive-text="关闭"
          ></el-switch>
        </el-form-item>
        <el-form-item label="刷新间隔" v-if="settingsForm.autoRefresh">
          <el-select v-model="settingsForm.refreshInterval">
            <el-option label="3秒" :value="3000"></el-option>
            <el-option label="5秒" :value="5000"></el-option>
            <el-option label="10秒" :value="10000"></el-option>
            <el-option label="30秒" :value="30000"></el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="报警音效">
          <el-switch
            v-model="settingsForm.alarmSound"
            active-text="开启"
            inactive-text="关闭"
          ></el-switch>
        </el-form-item>
        <el-form-item label="图表时间范围">
          <el-select v-model="settingsForm.chartTimeRange">
            <el-option label="15分钟" value="15"></el-option>
            <el-option label="30分钟" value="30"></el-option>
            <el-option label="1小时" value="60"></el-option>
            <el-option label="3小时" value="180"></el-option>
          </el-select>
        </el-form-item>
        <el-form-item label="数据保留">
          <el-select v-model="settingsForm.dataRetention">
            <el-option label="1天" value="1"></el-option>
            <el-option label="7天" value="7"></el-option>
            <el-option label="30天" value="30"></el-option>
            <el-option label="90天" value="90"></el-option>
          </el-select>
        </el-form-item>
      </el-form>
      <span slot="footer" class="dialog-footer">
        <el-button @click="settingsDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveSettings">保存</el-button>
      </span>
    </el-dialog>

    <!-- 报警详情对话框 -->
    <el-dialog
      :title="currentAlarmDetails ? currentAlarmDetails.deviceName + '报警详情' : '报警详情'"
      :visible.sync="alarmDetailsDialogVisible"
      width="600px"
    >
      <div v-if="currentAlarmDetails" class="alarm-details">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="设备名称">
            {{ currentAlarmDetails.deviceName }}
          </el-descriptions-item>
          <el-descriptions-item label="监测因子">
            {{ currentAlarmDetails.factorName }}
          </el-descriptions-item>
          <el-descriptions-item label="报警类型">
            <el-tag :type="currentAlarmDetails.limitType === 'Upper' ? 'danger' : 'warning'">
              {{ currentAlarmDetails.limitType === 'Upper' ? '超过上限' : '低于下限' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="当前值">
            {{ currentAlarmDetails.value }}
          </el-descriptions-item>
          <el-descriptions-item label="阈值上限">
            {{ getThresholdUpper(currentAlarmDetails.factorType) }}
          </el-descriptions-item>
          <el-descriptions-item label="阈值下限">
            {{ getThresholdLower(currentAlarmDetails.factorType) }}
          </el-descriptions-item>
          <el-descriptions-item label="发生时间">
            {{ formatTime(currentAlarmDetails.timestamp) }}
          </el-descriptions-item>
          <el-descriptions-item label="持续时间">
            {{ formatDuration(currentAlarmDetails.duration) }}
          </el-descriptions-item>
          <el-descriptions-item label="报警信息" :span="2">
            {{ currentAlarmDetails.message }}
          </el-descriptions-item>
        </el-descriptions>
        
        <div class="alarm-history" v-if="alarmHistory.length > 0">
          <h4>历史报警记录</h4>
          <el-table :data="alarmHistory" size="small">
            <el-table-column prop="timestamp" label="时间" width="180">
              <template slot-scope="{row}">
                {{ formatTime(row.timestamp) }}
              </template>
            </el-table-column>
            <el-table-column prop="value" label="值" width="100" />
            <el-table-column prop="limitType" label="类型" width="100">
              <template slot-scope="{row}">
                <el-tag :type="row.limitType === 'Upper' ? 'danger' : 'warning'" size="small">
                  {{ row.limitType === 'Upper' ? '超上限' : '低下限' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="message" label="信息" />
          </el-table>
        </div>
      </div>
      <span slot="footer" class="dialog-footer">
        <el-button @click="alarmDetailsDialogVisible = false">关闭</el-button>
        <el-button
          type="primary"
          @click="acknowledgeCurrentAlarm"
          v-if="currentAlarmDetails && !currentAlarmDetails.isAcknowledged"
        >
          确认报警
        </el-button>
      </span>
    </el-dialog>
  </div>
</template>

<script>
import { mapState, mapGetters, mapActions } from 'vuex'
import TrendIndicator from '@/components/charts/TrendIndicator.vue'
import ThresholdIndicator from '@/components/charts/ThresholdIndicator.vue'
import EmptyState from '@/components/common/EmptyState.vue'

export default {
  name: 'Dashboard',
  components: {
    TrendIndicator,
    ThresholdIndicator,
    EmptyState
  },
  data() {
    return {
      selectedDeviceId: null,
      chartTimeRange: '30',
      isRefreshing: false,
      refreshTimer: null,
      settingsDialogVisible: false,
      alarmDetailsDialogVisible: false,
      currentAlarmDetails: null,
      alarmHistory: [],
      
      settingsForm: {
        autoRefresh: true,
        refreshInterval: 5000,
        alarmSound: true,
        chartTimeRange: '30',
        dataRetention: '7'
      },
      
      // 图表配置
      temperatureChartOption: {},
      humidityChartOption: {},
      currentVoltageChartOption: {},
      statusChartOption: {}
    }
  },
  computed: {
    ...mapState({
      devices: state => state.devices.devices,
      deviceData: state => state.devices.deviceData,
      realTimeData: state => state.devices.realTimeData,
      activeAlarms: state => state.alarms.activeAlarms,
      thresholds: state => state.thresholds.thresholds,
      connection: state => state.connection
    }),
    
    ...mapGetters('devices', [
      'activeDevices',
      'selectedDevice',
      'selectedDeviceData',
      'selectedDeviceRealtimeData'
    ]),
    
    ...mapGetters('connection', ['isConnected', 'autoRefresh', 'isAlarmMuted']),
    
    currentData() {
      return this.selectedDeviceRealtimeData || {}
    },
    
    temperatureThreshold() {
      return this.getThresholdByFactor('Temperature')
    },
    
    humidityThreshold() {
      return this.getThresholdByFactor('Humidity')
    },
    
    currentThreshold() {
      return this.getThresholdByFactor('Current')
    },
    
    voltageThreshold() {
      return this.getThresholdByFactor('Voltage')
    },
    
    // 趋势计算
    temperatureTrend() {
      return this.calculateTrend('temperature')
    },
    
    humidityTrend() {
      return this.calculateTrend('humidity')
    },
    
    currentTrend() {
      return this.calculateTrend('current')
    },
    
    voltageTrend() {
      return this.calculateTrend('voltage')
    },
    
    // 图表数据
    temperatureChartData() {
      return this.formatChartData('temperature')
    },
    
    humidityChartData() {
      return this.formatChartData('humidity')
    },
    
    currentVoltageChartData() {
      const currentData = this.formatChartData('current')
      const voltageData = this.formatChartData('voltage')
      
      return {
        current: currentData,
        voltage: voltageData
      }
    },
    
    statusChartData() {
      return this.formatStatusData()
    }
  },
  watch: {
    selectedDeviceId(newVal) {
      if (newVal) {
        this.loadDeviceData()
        this.subscribeToDevice()
      }
    },
    
    chartTimeRange(newVal) {
      this.loadDeviceData(parseInt(newVal))
    },
    
    autoRefresh(enabled) {
      if (enabled) {
        this.startAutoRefresh()
      } else {
        this.stopAutoRefresh()
      }
    },
    
    // 监听实时数据变化
    selectedDeviceRealtimeData: {
      handler(newData) {
        if (newData) {
          this.updateCharts()
        }
      },
      deep: true
    },
    
    // 监听设备数据变化
    selectedDeviceData: {
      handler() {
        this.updateCharts()
      },
      deep: true
    }
  },
  created() {
    this.initDashboard()
  },
  mounted() {
    this.startAutoRefresh()
    
    // 监听窗口激活事件
    document.addEventListener('visibilitychange', this.handleVisibilityChange)
  },
  beforeDestroy() {
    this.stopAutoRefresh()
    document.removeEventListener('visibilitychange', this.handleVisibilityChange)
    
    // 取消订阅当前设备
    if (this.selectedDeviceId) {
      this.unsubscribeFromDevice(this.selectedDeviceId)
    }
  },
  methods: {
    ...mapActions('devices', [
      'fetchDevices',
      'fetchDeviceData',
      'fetchLatestData',
      'selectDevice'
    ]),
    
    ...mapActions('connection', [
      'toggleAlarmMute',
      'toggleAutoRefresh',
      'subscribeToDevice',
      'unsubscribeFromDevice'
    ]),
    
    ...mapActions('alarms', [
      'acknowledgeAlarm',
      'acknowledgeAllAlarms',
      'getAlarmHistory'
    ]),
    
    async initDashboard() {
      try {
        // 加载设备列表
        await this.fetchDevices()
        
        // 如果没有选中设备，使用store中的默认值
        if (!this.selectedDeviceId && this.activeDevices.length > 0) {
          this.selectedDeviceId = this.activeDevices[0].id
        }
        
        // 加载设备数据
        if (this.selectedDeviceId) {
          await this.loadDeviceData()
        }
        
        // 初始化图表
        this.initCharts()
        
      } catch (error) {
        console.error('初始化仪表盘失败:', error)
        this.$message.error('初始化仪表盘失败')
      }
    },
    
    async loadDeviceData(minutes = null) {
      if (!this.selectedDeviceId) return
      
      try {
        const loadMinutes = minutes || parseInt(this.chartTimeRange)
        await this.fetchDeviceData({
          deviceId: this.selectedDeviceId,
          minutes: loadMinutes
        })
        
        // 加载最新数据
        await this.fetchLatestData(this.selectedDeviceId)
        
      } catch (error) {
        console.error('加载设备数据失败:', error)
      }
    },
    
    async refreshData() {
      this.isRefreshing = true
      try {
        await this.loadDeviceData()
        this.$message.success('数据刷新成功')
      } catch (error) {
        console.error('刷新数据失败:', error)
      } finally {
        this.isRefreshing = false
      }
    },
    
    handleDeviceChange(deviceId) {
      this.selectDevice(deviceId)
    },
    
    async subscribeToDevice() {
      if (this.selectedDeviceId && this.isConnected) {
        await this.subscribeToDevice(this.selectedDeviceId)
      }
    },
    
    unsubscribeFromDevice(deviceId) {
      this.unsubscribeFromDevice(deviceId)
    },
    
    startAutoRefresh() {
      if (this.autoRefresh && !this.refreshTimer) {
        this.refreshTimer = setInterval(() => {
          if (this.selectedDeviceId && document.visibilityState === 'visible') {
            this.loadDeviceData()
          }
        }, this.connection.refreshInterval)
      }
    },
    
    stopAutoRefresh() {
      if (this.refreshTimer) {
        clearInterval(this.refreshTimer)
        this.refreshTimer = null
      }
    },
    
    toggleAutoRefresh() {
      this.toggleAutoRefresh()
    },
    
    muteAllAlarms() {
      this.toggleAlarmMute()
      const message = this.isAlarmMuted ? '报警音已开启' : '报警音已关闭'
      this.$message.success(message)
    },
    
    async acknowledgeAllAlarms() {
      try {
        await this.acknowledgeAllAlarms()
        this.$message.success('所有报警已确认')
      } catch (error) {
        console.error('确认所有报警失败:', error)
      }
    },
    
    async acknowledgeAlarm(alarmId) {
      try {
        await this.acknowledgeAlarm(alarmId)
        this.$message.success('报警已确认')
      } catch (error) {
        console.error('确认报警失败:', error)
      }
    },
    
    async viewAlarmDetails(alarm) {
      this.currentAlarmDetails = alarm
      this.alarmDetailsDialogVisible = true
      
      // 加载历史报警记录
      try {
        const history = await this.getAlarmHistory({
          deviceId: alarm.deviceId,
          factorType: alarm.factorType
        })
        this.alarmHistory = history
      } catch (error) {
        console.error('加载报警历史失败:', error)
      }
    },
    
    async acknowledgeCurrentAlarm() {
      if (this.currentAlarmDetails) {
        await this.acknowledgeAlarm(this.currentAlarmDetails.id)
        this.alarmDetailsDialogVisible = false
      }
    },
    
    showSettings() {
      this.settingsForm = {
        autoRefresh: this.autoRefresh,
        refreshInterval: this.connection.refreshInterval,
        alarmSound: !this.isAlarmMuted,
        chartTimeRange: this.chartTimeRange,
        dataRetention: '7'
      }
      this.settingsDialogVisible = true
    },
    
    saveSettings() {
      // 保存设置到store
      if (this.settingsForm.autoRefresh !== this.autoRefresh) {
        this.toggleAutoRefresh()
      }
      
      if (this.settingsForm.refreshInterval !== this.connection.refreshInterval) {
        this.$store.dispatch('connection/setRefreshInterval', this.settingsForm.refreshInterval)
      }
      
      if (this.settingsForm.alarmSound === this.isAlarmMuted) {
        this.toggleAlarmMute()
      }
      
      this.chartTimeRange = this.settingsForm.chartTimeRange
      
      this.settingsDialogVisible = false
      this.$message.success('设置已保存')
    },
    
    // 工具方法
    getDeviceStatusType(device) {
      const latestData = this.realTimeData[device.id]
      if (!latestData) return 'info'
      
      const status = latestData.status
      switch (status) {
        case 'Normal': return 'success'
        case 'Warning': return 'warning'
        case 'Fault': return 'danger'
        case 'Offline': return 'info'
        default: return 'info'
      }
    },
    
    getDeviceStatusText(device) {
      const latestData = this.realTimeData[device.id]
      if (!latestData) return '未知'
      
      const statusMap = {
        'Normal': '正常',
        'Warning': '警告',
        'Fault': '故障',
        'Offline': '离线'
      }
      return statusMap[latestData.status] || latestData.status
    },
    
    getAlarmSeverityClass(alarm) {
      if (alarm.limitType === 'Upper') {
        return 'alarm-severe'
      } else {
        return 'alarm-moderate'
      }
    },
    
    getThresholdByFactor(factorType) {
      if (!this.selectedDeviceId || !this.thresholds[this.selectedDeviceId]) {
        return null
      }
      
      return this.thresholds[this.selectedDeviceId].find(
        t => t.factorType === factorType
      )
    },
    
    getThresholdUpper(factorType) {
      const threshold = this.getThresholdByFactor(factorType)
      return threshold ? threshold.upperLimit : '--'
    },
    
    getThresholdLower(factorType) {
      const threshold = this.getThresholdByFactor(factorType)
      return threshold ? threshold.lowerLimit : '--'
    },
    
    calculateTrend(field) {
      const data = this.selectedDeviceData
      if (!data || data.length < 2) return 0
      
      const recentData = data.slice(-10) // 最近10个数据点
      const values = recentData.map(item => item[field]).filter(v => v != null)
      
      if (values.length < 2) return 0
      
      const first = values[0]
      const last = values[values.length - 1]
      
      return ((last - first) / first) * 100
    },
    
    formatChartData(field) {
      const data = this.selectedDeviceData
      if (!data || data.length === 0) return []
      
      return data
        .filter(item => item[field] != null)
        .map(item => ({
          time: new Date(item.timestamp),
          value: item[field]
        }))
        .slice(-100) // 最多显示100个点
    },
    
    formatStatusData() {
      const data = this.selectedDeviceData
      if (!data || data.length === 0) return []
      
      const statusCount = {
        Normal: 0,
        Warning: 0,
        Fault: 0,
        Offline: 0
      }
      
      data.forEach(item => {
        if (item.status && statusCount[item.status] !== undefined) {
          statusCount[item.status]++
        }
      })
      
      return Object.entries(statusCount)
        .filter(([_, count]) => count > 0)
        .map(([status, count]) => ({
          name: this.getDeviceStatusText({ status }),
          value: count
        }))
    },
    
    formatTime(timestamp) {
      if (!timestamp) return '--'
      return new Date(timestamp).toLocaleString()
    },
    
    formatDuration(duration) {
      if (!duration) return '--'
      
      const seconds = Math.floor(duration / 1000)
      const minutes = Math.floor(seconds / 60)
      const hours = Math.floor(minutes / 60)
      
      if (hours > 0) {
        return `${hours}小时${minutes % 60}分钟`
      } else if (minutes > 0) {
        return `${minutes}分钟${seconds % 60}秒`
      } else {
        return `${seconds}秒`
      }
    },
    
    handleVisibilityChange() {
      if (document.visibilityState === 'visible') {
        // 页面重新激活，重新加载数据
        this.loadDeviceData()
      }
    },
    
    // 图表相关方法
    initCharts() {
      this.temperatureChartOption = this.createLineChartOption('温度', '°C', '#ff6b6b')
      this.humidityChartOption = this.createLineChartOption('湿度', '%', '#4ecdc4')
      this.currentVoltageChartOption = this.createDualLineChartOption()
      this.statusChartOption = this.createPieChartOption()
    },
    
    updateCharts() {
      this.updateLineChart('temperature')
      this.updateLineChart('humidity')
      this.updateDualLineChart()
      this.updatePieChart()
    },
    
    createLineChartOption(title, unit, color) {
      return {
        title: {
          text: title,
          left: 'center',
          textStyle: {
            fontSize: 14,
            fontWeight: 'normal'
          }
        },
        tooltip: {
          trigger: 'axis',
          formatter: (params) => {
            const date = new Date(params[0].data[0])
            const time = date.toLocaleTimeString()
            const value = params[0].data[1]
            return `${time}<br/>${title}: ${value}${unit}`
          }
        },
        xAxis: {
          type: 'time',
          axisLabel: {
            formatter: (value) => {
              const date = new Date(value)
              return date.toLocaleTimeString()
            }
          }
        },
        yAxis: {
          type: 'value',
          name: unit,
          nameTextStyle: {
            padding: [0, 0, 0, 10]
          }
        },
        series: [{
          name: title,
          type: 'line',
          smooth: true,
          symbol: 'circle',
          symbolSize: 4,
          lineStyle: {
            width: 2
          },
          itemStyle: {
            color: color
          },
          areaStyle: {
            color: {
              type: 'linear',
              x: 0,
              y: 0,
              x2: 0,
              y2: 1,
              colorStops: [{
                offset: 0, color: color + '40'
              }, {
                offset: 1, color: color + '00'
              }]
            }
          },
          data: []
        }],
        grid: {
          left: '3%',
          right: '4%',
          bottom: '3%',
          top: '15%',
          containLabel: true
        }
      }
    },
    
    createDualLineChartOption() {
      return {
        title: {
          text: '电流电压趋势',
          left: 'center',
          textStyle: {
            fontSize: 14,
            fontWeight: 'normal'
          }
        },
        tooltip: {
          trigger: 'axis',
          axisPointer: {
            type: 'cross'
          }
        },
        legend: {
          data: ['电流', '电压'],
          top: 10
        },
        xAxis: {
          type: 'time',
          axisLabel: {
            formatter: (value) => {
              const date = new Date(value)
              return date.toLocaleTimeString()
            }
          }
        },
        yAxis: [
          {
            type: 'value',
            name: '电流(A)',
            position: 'left'
          },
          {
            type: 'value',
            name: '电压(V)',
            position: 'right'
          }
        ],
        series: [
          {
            name: '电流',
            type: 'line',
            yAxisIndex: 0,
            smooth: true,
            symbol: 'circle',
            symbolSize: 4,
            lineStyle: {
              width: 2
            },
            itemStyle: {
              color: '#36a2eb'
            },
            data: []
          },
          {
            name: '电压',
            type: 'line',
            yAxisIndex: 1,
            smooth: true,
            symbol: 'circle',
            symbolSize: 4,
            lineStyle: {
              width: 2
            },
            itemStyle: {
              color: '#ff6384'
            },
            data: []
          }
        ],
        grid: {
          left: '3%',
          right: '4%',
          bottom: '3%',
          top: '20%',
          containLabel: true
        }
      }
    },
    
    createPieChartOption() {
      return {
        title: {
          text: '设备状态分布',
          left: 'center',
          textStyle: {
            fontSize: 14,
            fontWeight: 'normal'
          }
        },
        tooltip: {
          trigger: 'item',
          formatter: '{a} <br/>{b}: {c} ({d}%)'
        },
        legend: {
          orient: 'vertical',
          left: 'left',
          top: 'center'
        },
        series: [
          {
            name: '状态分布',
            type: 'pie',
            radius: ['40%', '70%'],
            center: ['60%', '50%'],
            avoidLabelOverlap: false,
            itemStyle: {
              borderRadius: 10,
              borderColor: '#fff',
              borderWidth: 2
            },
            label: {
              show: false,
              position: 'center'
            },
            emphasis: {
              label: {
                show: true,
                fontSize: '16',
                fontWeight: 'bold'
              }
            },
            labelLine: {
              show: false
            },
            data: []
          }
        ]
      }
    },
    
    updateLineChart(field) {
      const chartData = this[`${field}ChartData`]
      if (!chartData || chartData.length === 0) return
      
      const option = this[`${field}ChartOption`]
      option.series[0].data = chartData.map(item => [item.time, item.value])
      
      // 重新设置图表
      this[`${field}ChartOption`] = { ...option }
    },
    
    updateDualLineChart() {
      const data = this.currentVoltageChartData
      if (!data.current || data.current.length === 0) return
      
      const option = this.currentVoltageChartOption
      option.series[0].data = data.current.map(item => [item.time, item.value])
      option.series[1].data = data.voltage.map(item => [item.time, item.value])
      
      this.currentVoltageChartOption = { ...option }
    },
    
    updatePieChart() {
      const data = this.statusChartData
      if (data.length === 0) return
      
      const option = this.statusChartOption
      option.series[0].data = data
      
      // 设置颜色
      const colorMap = {
        '正常': '#67c23a',
        '警告': '#e6a23c',
        '故障': '#f56c6c',
        '离线': '#909399'
      }
      
      option.series[0].data.forEach(item => {
        item.itemStyle = {
          color: colorMap[item.name] || '#909399'
        }
      })
      
      this.statusChartOption = { ...option }
    }
  }
}
</script>

<style lang="scss" scoped>
.dashboard-container {
  height: 100%;
  display: flex;
  flex-direction: column;
  
  .page-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 20px;
    
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
    
    .page-actions {
      display: flex;
      gap: 10px;
    }
  }
  
  .device-selector-section {
    margin-bottom: 20px;
    
    .card-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      
      span {
        font-weight: 600;
        font-size: 16px;
      }
    }
    
    .device-list {
      ::v-deep .el-radio-group {
        display: flex;
        flex-wrap: wrap;
        gap: 10px;
        width: 100%;
        
        .el-radio-button {
          flex: 1;
          min-width: 200px;
          
          .el-radio-button__inner {
            width: 100%;
            padding: 12px 20px;
            border-radius: 4px;
          }
        }
      }
      
      .device-item {
        display: flex;
        align-items: center;
        justify-content: space-between;
        width: 100%;
        
        .device-info {
          .device-name {
            font-weight: 600;
            margin-bottom: 4px;
          }
          
          .device-code {
            font-size: 12px;
            color: var(--text-color-secondary);
          }
        }
      }
    }
  }
  
  .dashboard-content {
    flex: 1;
    display: flex;
    flex-direction: column;
    gap: 20px;
    overflow: auto;
    
    .real-time-cards {
      .data-card {
        border-radius: 8px;
        border: 1px solid var(--border-color);
        transition: all 0.3s;
        
        &:hover {
          transform: translateY(-2px);
          box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important;
        }
        
        .card-content {
          display: flex;
          align-items: center;
          gap: 15px;
          padding: 20px 0;
          
          .card-icon {
            width: 60px;
            height: 60px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 28px;
            
            &.temperature {
              background: linear-gradient(135deg, #ff6b6b20, #ff6b6b40);
              color: #ff6b6b;
            }
            
            &.humidity {
              background: linear-gradient(135deg, #4ecdc420, #4ecdc440);
              color: #4ecdc4;
            }
            
            &.current {
              background: linear-gradient(135deg, #36a2eb20, #36a2eb40);
              color: #36a2eb;
            }
            
            &.voltage {
              background: linear-gradient(135deg, #ff638420, #ff638440);
              color: #ff6384;
            }
          }
          
          .card-info {
            flex: 1;
            
            .card-label {
              font-size: 14px;
              color: var(--text-color-secondary);
              margin-bottom: 5px;
            }
            
            .card-value {
              font-size: 28px;
              font-weight: 700;
              color: var(--text-color-primary);
              margin-bottom: 5px;
              
              .card-unit {
                font-size: 14px;
                color: var(--text-color-secondary);
                margin-left: 2px;
              }
            }
          }
        }
        
        .card-footer {
          padding-top: 10px;
          border-top: 1px solid var(--border-color);
        }
      }
    }
    
    .real-time-charts {
      .chart-card {
        border-radius: 8px;
        border: 1px solid var(--border-color);
        
        .chart-header {
          display: flex;
          align-items: center;
          justify-content: space-between;
          
          span {
            font-weight: 600;
            font-size: 16px;
          }
        }
        
        .chart-container {
          height: 300px;
        }
      }
    }
    
    .alarm-panel {
      .card-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        
        span {
          font-weight: 600;
          font-size: 16px;
        }
        
        .header-actions {
          display: flex;
          align-items: center;
          gap: 10px;
        }
      }
      
      .alarm-list {
        .no-alarms {
          padding: 40px 0;
          text-align: center;
          color: var(--text-color-secondary);
        }
        
        .alarms-container {
          display: flex;
          flex-direction: column;
          gap: 10px;
          max-height: 400px;
          overflow-y: auto;
          
          .alarm-item {
            border-radius: 6px;
            padding: 15px;
            border-left: 4px solid;
            background: var(--bg-color-primary);
            transition: all 0.3s;
            
            &:hover {
              transform: translateX(5px);
              box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
            }
            
            &.alarm-severe {
              border-left-color: #f56c6c;
              background: linear-gradient(90deg, #f56c6c10, transparent);
            }
            
            &.alarm-moderate {
              border-left-color: #e6a23c;
              background: linear-gradient(90deg, #e6a23c10, transparent);
            }
            
            .alarm-content {
              .alarm-header {
                display: flex;
                align-items: center;
                justify-content: space-between;
                margin-bottom: 10px;
                
                .alarm-title {
                  font-weight: 600;
                  font-size: 14px;
                  display: flex;
                  align-items: center;
                  gap: 8px;
                  
                  i {
                    font-size: 16px;
                  }
                }
                
                .alarm-time {
                  font-size: 12px;
                  color: var(--text-color-secondary);
                }
              }
              
              .alarm-body {
                display: flex;
                align-items: center;
                justify-content: space-between;
                margin-bottom: 10px;
                
                .alarm-message {
                  flex: 1;
                  font-size: 13px;
                  color: var(--text-color-primary);
                }
                
                .alarm-value {
                  margin-left: 10px;
                }
              }
              
              .alarm-footer {
                display: flex;
                align-items: center;
                justify-content: space-between;
                
                .alarm-duration {
                  font-size: 12px;
                  color: var(--text-color-secondary);
                }
                
                .alarm-actions {
                  display: flex;
                  gap: 5px;
                }
              }
            }
          }
        }
      }
    }
  }
  
  .alarm-details {
    .alarm-history {
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
  .dashboard-container {
    .page-header {
      flex-direction: column;
      align-items: stretch;
      gap: 10px;
      
      .page-actions {
        justify-content: flex-start;
      }
    }
    
    .device-selector-section {
      .device-list {
        ::v-deep .el-radio-group {
          .el-radio-button {
            min-width: 100%;
          }
        }
      }
    }
    
    .dashboard-content {
      .real-time-cards {
        .data-card {
          .card-content {
            flex-direction: column;
            text-align: center;
            gap: 10px;
          }
        }
      }
      
      .alarm-panel {
        .alarm-list {
          .alarms-container {
            .alarm-item {
              .alarm-body {
                flex-direction: column;
                align-items: flex-start;
                gap: 10px;
                
                .alarm-value {
                  margin-left: 0;
                }
              }
              
              .alarm-footer {
                flex-direction: column;
                align-items: flex-start;
                gap: 10px;
                
                .alarm-actions {
                  width: 100%;
                  justify-content: flex-end;
                }
              }
            }
          }
        }
      }
    }
  }
}
</style>