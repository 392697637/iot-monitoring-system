<template>
  <el-card>
    <div style="display: flex; justify-content: space-between; align-items: center;">
      <h2 style="margin: 0;">实时监控</h2>
      <div style="display: flex; gap: 10px; align-items: center;">
        <!-- 刷新频率选择 -->
        <el-select v-model="refreshInterval" style="width: 120px;" placeholder="刷新频率">
          <el-option label="1分钟" :value="60000" />
          <el-option label="5分钟" :value="300000" />
          <el-option label="10分钟" :value="600000" />
          <el-option label="30分钟" :value="1800000" />
          <el-option label="1小时" :value="3600000" />
          <el-option label="手动刷新" :value="0" />
        </el-select>
        
        <!-- 手动刷新按钮 -->
        <el-button 
          type="primary" 
          :icon="Refresh" 
          :loading="isRefreshing"
          @click="manualRefresh"
        >
          刷新
        </el-button>
        
        <!-- 自动刷新开关 -->
        <el-switch
          v-model="autoRefreshEnabled"
          active-text="自动"
          inactive-text="手动"
          @change="toggleAutoRefresh"
        />
        
        <!-- 告警音开关 -->
        <el-switch
          v-model="alarmSoundEnabled"
          active-text="告警音"
          inactive-text="静音"
          style="margin-left: 10px;"
        />
      </div>
    </div>

    <!-- 设备选择 -->
    <div style="margin-top: 20px;">
      <el-select v-model="deviceId" placeholder="选择设备" @change="onDeviceChange" style="width: 300px;">
        <el-option
          v-for="d in devices"
          :key="d.deviceId"
          :label="d.deviceName"
          :value="d.deviceId"
        />
      </el-select>
      
      <!-- 刷新状态显示 -->
      <span style="margin-left: 20px; color: #999; font-size: 14px;">
        <el-icon v-if="lastRefreshTime" style="vertical-align: middle;">
          <Clock />
        </el-icon>
        {{ lastRefreshTime ? `最后更新: ${formatTime(lastRefreshTime)}` : '未更新' }}
        <span v-if="showCountdown" style="margin-left: 10px; color: #409EFF; font-weight: 500;">
          ({{ countdownDisplay }})
        </span>
      </span>
    </div>

    <!-- 监控指标卡片 -->
    <el-row :gutter="20" style="margin-top: 20px">
      <el-col :span="6" v-for="metric in visibleMetrics" :key="metric.label">
        <el-card 
          :class="{ 
            'alarm-high': metric.alarmType === 'high', 
            'alarm-low': metric.alarmType === 'low',
            'alarm-range': metric.alarmType === 'range',
            'alarm-critical': metric.alarmType && metric.isAlarm // 只有启用了告警且触发告警时才添加这个类
          }" 
          shadow="hover" 
          class="metric-card"
          @click="showMetricDetail(metric)"
        >
          <!-- 正常显示模式 -->
          <div class="normal-mode">
            <div style="font-weight: bold; margin-bottom: 8px; display: flex; align-items: center; justify-content: space-between;">
              <span>{{ metric.label }}</span>
              <div style="display: flex; align-items: center;">
                <el-tooltip v-if="metric.isAlarm" :content="getAlarmConfigTip(metric.config)" placement="top">
                  <el-icon style="margin-left: 5px; color: #f56c6c;">
                    <Warning v-if="metric.alarmType" />
                    <InfoFilled v-else />
                  </el-icon>
                </el-tooltip>
                <!-- 当前告警状态指示器（只在启用了告警且触发告警时显示） -->
                <span v-if="metric.alarmType && metric.isAlarm" class="alarm-indicator"></span>
              </div>
            </div>
            <transition name="fade" mode="out-in">
              <div :key="metric.value" style="font-size: 24px; font-weight: bold; color: #409EFF;">
                {{ formatMetricValue(metric) }}
              </div>
            </transition>
            <!-- 告警文本（只在启用了告警且触发告警时显示） -->
            <transition name="fade">
              <div v-if="metric.alarmType && metric.isAlarm" class="alarm-text">
                <el-icon><Warning /></el-icon>
                {{ getAlarmText(metric.alarmType) }}
              </div>
            </transition>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 设备因子详情弹框 -->
    <el-dialog
      v-model="detailDialogVisible"
      :title="`${currentMetric?.label || '设备因子'} - 详细信息`"
      width="600px"
      :before-close="handleDialogClose"
      class="metric-detail-dialog"
    >
      <div v-if="currentMetric" class="dialog-content">
        <div class="info-grid">
          <!-- 基本信息区（总是显示） -->
          <div class="info-section basic-info">
            <h3 class="section-title">
              <el-icon><InfoFilled /></el-icon> 基本信息
            </h3>
            <div class="info-row">
              <span class="info-label">因子名称:</span>
              <span class="info-value">{{ currentMetric.label }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">字段名称:</span>
              <span class="info-value">{{ currentMetric.fieldName }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">实时数值:</span>
              <span class="info-value" :class="{'alarm-value': currentMetric.alarmType && currentMetric.isAlarm}">
                {{ formatValueOnly(currentMetric.value) }} {{ currentMetric.unit || '' }}
              </span>
            </div>
            <div class="info-row">
              <span class="info-label">单位:</span>
              <span class="info-value">{{ currentMetric.unit || '无' }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">更新时间:</span>
              <span class="info-value">{{ formatTime(currentMetric.lastUpdate || new Date()) }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">告警监控:</span>
              <span class="info-value" :class="{
                'status-normal': !currentMetric.isAlarm,
                'status-warning': currentMetric.isAlarm
              }">
                <el-icon v-if="currentMetric.isAlarm" style="vertical-align: middle; margin-right: 5px; color: #f56c6c;">
                  <Warning />
                </el-icon>
                <el-icon v-else style="vertical-align: middle; margin-right: 5px; color: #67c23a;">
                  <CircleCheck />
                </el-icon>
                {{ currentMetric.isAlarm ? '启用' : '禁用' }}
              </span>
            </div>
          </div>

          <!-- 告警状态区（只在启用了告警时显示） -->
          <div v-if="currentMetric.isAlarm" class="info-section alarm-info">
            <h3 class="section-title">
              <el-icon><Warning /></el-icon> 告警配置与状态
            </h3>
            <div class="info-row">
              <span class="info-label">阈值比较类型:</span>
              <span class="info-value">{{ getConfigTypeText(currentMetric.config?.type) }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">阈值配置:</span>
              <span class="info-value">{{ getThresholdDisplay(currentMetric) }}</span>
            </div>
            <div v-if="currentMetric.config?.min !== undefined" class="info-row">
              <span class="info-label">阈值最小值:</span>
              <span class="info-value">{{ formatNumber(currentMetric.config.min) }}</span>
            </div>
            <div v-if="currentMetric.config?.max !== undefined" class="info-row">
              <span class="info-label">阈值最大值:</span>
              <span class="info-value">{{ formatNumber(currentMetric.config.max) }}</span>
            </div>
            <div v-if="currentMetric.config?.threshold !== undefined" class="info-row">
              <span class="info-label">阈值:</span>
              <span class="info-value">{{ formatNumber(currentMetric.config.threshold) }}</span>
            </div>
            <div class="info-row">
              <span class="info-label">当前状态:</span>
              <span class="info-value" :class="{
                'status-normal': !currentMetric.alarmType,
                'status-alarm': currentMetric.alarmType
              }">
                <el-icon v-if="currentMetric.alarmType" style="vertical-align: middle; margin-right: 5px;">
                  <Warning />
                </el-icon>
                {{ currentMetric.alarmType ? getAlarmText(currentMetric.alarmType) : '正常' }}
              </span>
            </div>
          </div>

          <!-- 阈值比较区（只在启用了告警且有数值时显示） -->
          <div v-if="currentMetric.isAlarm && currentMetric.value !== null" class="info-section comparison-info">
            <h3 class="section-title">
              <el-icon><ScaleToOriginal /></el-icon> 阈值比较结果
            </h3>
            
            <!-- 区间报警比较 -->
            <div v-if="currentMetric.config?.type === 'range' && currentMetric.config?.min !== undefined && currentMetric.config?.max !== undefined">
              <div class="comparison-row">
                <div class="comparison-item">
                  <div class="comparison-label">最小值比较:</div>
                  <div class="comparison-content">
                    <div class="comparison-value">
                      {{ formatValueOnly(currentMetric.value) }} 
                      {{ parseFloat(currentMetric.value) >= currentMetric.config.min ? '≥' : '<' }} 
                      {{ currentMetric.config.min }}
                    </div>
                    <div class="comparison-result" :class="{
                      'result-pass': parseFloat(currentMetric.value) >= currentMetric.config.min,
                      'result-fail': parseFloat(currentMetric.value) < currentMetric.config.min
                    }">
                      <el-icon v-if="parseFloat(currentMetric.value) < currentMetric.config.min" style="color: #f56c6c;">
                        <Warning />
                      </el-icon>
                      {{ parseFloat(currentMetric.value) >= currentMetric.config.min ? '正常' : '低于最小值' }}
                    </div>
                  </div>
                </div>
                
                <div class="comparison-item">
                  <div class="comparison-label">最大值比较:</div>
                  <div class="comparison-content">
                    <div class="comparison-value">
                      {{ formatValueOnly(currentMetric.value) }} 
                      {{ parseFloat(currentMetric.value) <= currentMetric.config.max ? '≤' : '>' }} 
                      {{ currentMetric.config.max }}
                    </div>
                    <div class="comparison-result" :class="{
                      'result-pass': parseFloat(currentMetric.value) <= currentMetric.config.max,
                      'result-fail': parseFloat(currentMetric.value) > currentMetric.config.max
                    }">
                      <el-icon v-if="parseFloat(currentMetric.value) > currentMetric.config.max" style="color: #f56c6c;">
                        <Warning />
                      </el-icon>
                      {{ parseFloat(currentMetric.value) <= currentMetric.config.max ? '正常' : '超过最大值' }}
                    </div>
                  </div>
                </div>
              </div>
            </div>
            
            <!-- 高阈值报警比较 -->
            <div v-else-if="currentMetric.config?.type === 'high' && currentMetric.config?.threshold !== undefined">
              <div class="comparison-row">
                <div class="comparison-item">
                  <div class="comparison-label">高阈值比较:</div>
                  <div class="comparison-content">
                    <div class="comparison-value">
                      {{ formatValueOnly(currentMetric.value) }} 
                      {{ parseFloat(currentMetric.value) > currentMetric.config.threshold ? '>' : '≤' }} 
                      {{ currentMetric.config.threshold }}
                    </div>
                    <div class="comparison-result" :class="{
                      'result-pass': parseFloat(currentMetric.value) <= currentMetric.config.threshold,
                      'result-fail': parseFloat(currentMetric.value) > currentMetric.config.threshold
                    }">
                      <el-icon v-if="parseFloat(currentMetric.value) > currentMetric.config.threshold" style="color: #f56c6c;">
                        <Warning />
                      </el-icon>
                      {{ parseFloat(currentMetric.value) <= currentMetric.config.threshold ? '正常' : '超过阈值' }}
                    </div>
                  </div>
                </div>
              </div>
            </div>
            
            <!-- 低阈值报警比较 -->
            <div v-else-if="currentMetric.config?.type === 'low' && currentMetric.config?.threshold !== undefined">
              <div class="comparison-row">
                <div class="comparison-item">
                  <div class="comparison-label">低阈值比较:</div>
                  <div class="comparison-content">
                    <div class="comparison-value">
                      {{ formatValueOnly(currentMetric.value) }} 
                      {{ parseFloat(currentMetric.value) < currentMetric.config.threshold ? '<' : '≥' }} 
                      {{ currentMetric.config.threshold }}
                    </div>
                    <div class="comparison-result" :class="{
                      'result-pass': parseFloat(currentMetric.value) >= currentMetric.config.threshold,
                      'result-fail': parseFloat(currentMetric.value) < currentMetric.config.threshold
                    }">
                      <el-icon v-if="parseFloat(currentMetric.value) < currentMetric.config.threshold" style="color: #f56c6c;">
                        <Warning />
                      </el-icon>
                      {{ parseFloat(currentMetric.value) >= currentMetric.config.threshold ? '正常' : '低于阈值' }}
                    </div>
                  </div>
                </div>
              </div>
            </div>
            
            <!-- 不支持的阈值类型或无阈值配置 -->
            <div v-else class="no-comparison">
              无有效的阈值配置
            </div>
          </div>

          <!-- 告警禁用时的提示 -->
          <div v-else-if="!currentMetric.isAlarm" class="info-section no-alarm-info">
            <h3 class="section-title">
              <el-icon><InfoFilled /></el-icon> 告警配置
            </h3>
            <div class="no-alarm-message">
              <el-icon><CircleCheck /></el-icon>
              <span>该因子告警监控已禁用，不进行告警判断</span>
            </div>
          </div>
        </div>
      </div>
      
      <template #footer>
        <span class="dialog-footer">
          <el-button @click="detailDialogVisible = false">关闭</el-button>
        </span>
      </template>
    </el-dialog>
  </el-card>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch, computed } from "vue";
import { Refresh, Clock, Warning, InfoFilled, ScaleToOriginal, CircleCheck } from '@element-plus/icons-vue';
import {
  getDevices,
  getDeviceTable,
  getDataByTableName,
} from "../api/device";

const devices = ref([]);
const deviceId = ref(null);
const tablename = ref(null);
const metrics = ref([]);

// 详情弹框相关
const detailDialogVisible = ref(false);
const currentMetric = ref(null);

// 使用计算属性返回可见的指标
const visibleMetrics = computed(() => {
  return metrics.value.filter(m => m.isVisible !== false);
});

// 刷新相关状态
const refreshInterval = ref(60000);
const autoRefreshEnabled = ref(true);
const isRefreshing = ref(false);
const lastRefreshTime = ref(null);
const countdownTimer = ref(null);
const currentCountdown = ref(0);
const alarmSoundEnabled = ref(true);

// 音频实例
let alarmAudio = null;

// 格式化时间显示
const formatTime = (date) => {
  return date.toLocaleTimeString('zh-CN', { 
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
};

// 格式化数字
const formatNumber = (value) => {
  if (value === undefined || value === null) return '';
  return parseFloat(value).toString();
};

// 仅格式化值，不包含单位
const formatValueOnly = (value) => {
  if (value === null || value === undefined || value === "") {
    return "-";
  }
  
  if (typeof value === 'string' && value.includes('T')) {
    return value.replace(/T/g, ' ');
  }
  
  return value;
};

// 格式化指标值（带单位）
const formatMetricValue = (metric) => {
  if (metric.value === null || metric.value === undefined || metric.value === "") {
    return "-";
  }
  
  let value = metric.value;
  if (typeof value === 'string' && value.includes('T')) {
    value = value.replace(/T/g, ' ');
  }
  
  return metric.unit ? `${value} ${metric.unit}` : value;
};

// 格式化倒计时显示
const formatCountdown = (milliseconds) => {
  if (milliseconds <= 0) return '0秒';
  
  const minutes = Math.floor(milliseconds / 60000);
  const seconds = Math.floor((milliseconds % 60000) / 1000);
  
  if (minutes > 0) {
    return `${minutes}分${seconds < 10 ? '0' : ''}${seconds}秒`;
  } else {
    return `${seconds}秒`;
  }
};

// 是否显示倒计时
const showCountdown = computed(() => {
  return autoRefreshEnabled.value && refreshInterval.value > 0 && lastRefreshTime.value;
});

// 倒计时显示
const countdownDisplay = computed(() => {
  return formatCountdown(currentCountdown.value);
});

// 获取配置类型文本
const getConfigTypeText = (type) => {
  const types = {
    'range': '区间比较',
    'high': '高阈值',
    'low': '低阈值',
    'both': '双阈值'
  };
  return types[type] || '未知类型';
};

// 获取阈值显示文本
const getThresholdDisplay = (config) => {
  debugger
  if (!config) return '无';
  
  if (config.type === 'range' && config.min !== undefined && config.max !== undefined) {
    return `${config.min} ~ ${config.max}`;
  } else if (config.type === 'high' && config.threshold !== undefined) {
    return `> ${config.threshold}`;
  } else if (config.type === 'low' && config.threshold !== undefined) {
    return `< ${config.threshold}`;
  } else if (config.type === 'both' && config.min !== undefined && config.max !== undefined) {
    return `${config.min} ~ ${config.max}`;
  }
  return '未配置';
};

// 获取报警配置提示
const getAlarmConfigTip = (config) => {
  if (!config) return '未配置告警';
  
  if (config.type === 'range' && config.min !== undefined && config.max !== undefined) {
    return `正常范围: ${config.min} ~ ${config.max}`;
  } else if (config.type === 'high' && config.threshold !== undefined) {
    return `阈值: > ${config.threshold}`;
  } else if (config.type === 'low' && config.threshold !== undefined) {
    return `阈值: < ${config.threshold}`;
  } else if (config.type === 'both' && config.min !== undefined && config.max !== undefined) {
    return `正常范围: ${config.min} ~ ${config.max}`;
  }
  return '告警配置不完整';
};

// 获取报警文本
const getAlarmText = (alarmType) => {
  const texts = {
    'high': '超过上限',
    'low': '低于下限',
    'range': '超出范围'
  };
  return texts[alarmType] || '告警';
};

// 显示指标详情弹框
const showMetricDetail = (metric) => {
  currentMetric.value = metric;
  detailDialogVisible.value = true;
};

// 处理弹框关闭
const handleDialogClose = (done) => {
  currentMetric.value = null;
  done();
};

// 计算下次刷新时间
const calculateNextRefreshTime = () => {
  if (!autoRefreshEnabled.value || refreshInterval.value === 0 || !lastRefreshTime.value) {
    return 0;
  }
  
  const now = new Date();
  const nextRefresh = new Date(lastRefreshTime.value.getTime() + refreshInterval.value);
  const timeUntilNext = nextRefresh - now;
  
  return Math.max(0, timeUntilNext);
};

// 启动倒计时定时器
const startCountdownTimer = () => {
  stopCountdownTimer();
  
  const update = () => {
    const timeUntilNext = calculateNextRefreshTime();
    currentCountdown.value = timeUntilNext;
    
    if (timeUntilNext <= 1000 && autoRefreshEnabled.value && !isRefreshing.value) {
      loadData();
    }
  };
  
  update();
  countdownTimer.value = setInterval(update, 1000);
};

// 停止倒计时定时器
const stopCountdownTimer = () => {
  if (countdownTimer.value) {
    clearInterval(countdownTimer.value);
    countdownTimer.value = null;
  }
  currentCountdown.value = 0;
};

// 检查是否需要告警（支持区间报警）
const checkAlarm = (metric, rawValue) => {
  const config = metric.config;
  if (!metric.isAlarm || !config) return null; // 没有启用告警或没有配置，直接返回null
  
  const numValue = parseFloat(rawValue);
  if (isNaN(numValue)) return null;
  
  switch (config.type) {
    case 'range':
      if (config.min !== undefined && config.max !== undefined) {
        if (numValue < config.min) return 'low';
        if (numValue > config.max) return 'high';
      }
      break;
      
    case 'high':
      if (config.threshold !== undefined && numValue > config.threshold) {
        return 'high';
      }
      break;
      
    case 'low':
      if (config.threshold !== undefined && numValue < config.threshold) {
        return 'low';
      }
      break;
      
    case 'both':
      if (config.min !== undefined && config.max !== undefined) {
        if (numValue < config.min) return 'low';
        if (numValue > config.max) return 'high';
      }
      break;
  }
  
  return null;
};

// 解析报警配置
const parseAlarmConfig = (configType, minValue, maxValue) => {
  if (!configType) return null;
  
  try {
    if (configType === 'range' || configType === 'both') {
      return { 
        type: configType, 
        min: minValue !== undefined ? parseFloat(minValue) : undefined,
        max: maxValue !== undefined ? parseFloat(maxValue) : undefined
      };
    } else if (configType === 'high') {
      return { 
        type: configType, 
        threshold: maxValue !== undefined ? parseFloat(maxValue) : undefined
      };
    } else if (configType === 'low') {
      return { 
        type: configType, 
        threshold: minValue !== undefined ? parseFloat(minValue) : undefined
      };
    }
  } catch (e) {
    console.warn('解析报警配置失败:', e);
  }
  
  return null;
};

// 初始化设备列表
const initDevices = async () => {
  try {
    const res = await getDevices();
    devices.value = res;
    if (devices.value.length > 0 && !deviceId.value) {
      deviceId.value = devices.value[0].deviceId;
      tablename.value = devices.value[0].deviceTable;
    }
  } catch (error) {
    console.error("获取设备列表失败:", error);
  }
};

// 加载设备因子配置
const loadDeviceTable = async () => {
  if (!tablename.value) return;
  
  try {
    const res = await getDeviceTable(tablename.value);
    if (!res || !Array.isArray(res)) return;
    
    const initialMetrics = res.map((m) => {
      debugger
      // 使用新的字段名
      const config = parseAlarmConfig(m.configType, m.configMinValue, m.configMaxValue);
      const isAlarm = m.isAlarm === true || m.isAlarm === 1 || m.isAlarm === 'true' || m.isAlarm === '1';
      
      return {
        label: m.displayName,
        value: null,
        alarmType: null,
        fieldName: m.fieldName,
        unit: m.unit || '',
        config: config,
        isAlarm: isAlarm, // 使用isAlarm字段
        isVisible: m.isVisible !== false,
        // 缓存上次值用于比较
        lastValue: null,
        lastAlarmType: null,
        lastUpdate: null,
        // 记录告警状态变化
        alarmTriggered: false
      };
    });
    
    metrics.value = initialMetrics;
    
  } catch (error) {
    console.error("加载设备因子失败:", error);
  }
};

// 加载实时数据
const loadData = async () => {
  if (!tablename.value || metrics.value.length === 0) return;
  
  // 如果正在刷新，跳过
  if (isRefreshing.value) return;
  
  try {
    isRefreshing.value = true;
    
    const topNumber = 1;
    const orderby = "DID";
    const res = await getDataByTableName(tablename.value, orderby, topNumber);
    
    if (!res || !Array.isArray(res) || res.length === 0) {
      console.warn("未获取到数据");
      return;
    }
    
    const seledata = res[0];
    let hasNewAlarm = false;
    
    // 创建新数组，避免直接修改原数组
    const newMetrics = [...metrics.value];
    
    newMetrics.forEach((metric) => {
      const rawValue = seledata[metric.fieldName];
      
      // 更新值
      metric.lastValue = metric.value;
      metric.value = rawValue !== undefined && rawValue !== null && rawValue !== "" 
        ? rawValue 
        : null;
      metric.lastUpdate = new Date();
      
      // 检查报警状态（只有在启用了告警时才检查）
      if (metric.isAlarm && metric.config && rawValue !== undefined && rawValue !== null && rawValue !== "") {
        const newAlarmType = checkAlarm(metric, rawValue);
        
        // 只有在报警状态变化时才更新
        if (newAlarmType !== metric.lastAlarmType) {
          metric.lastAlarmType = metric.alarmType;
          metric.alarmType = newAlarmType;
          
          // 如果有新的报警（之前不是报警状态）
          if (newAlarmType && !metric.lastAlarmType) {
            metric.alarmTriggered = true;
            hasNewAlarm = true;
            
            // 记录告警触发时间
            metric.alarmTime = new Date();
          } else if (!newAlarmType && metric.lastAlarmType) {
            // 报警恢复
            metric.alarmTriggered = false;
          }
        }
      } else {
        // 没有启用告警或没有值，清空告警状态
        metric.alarmType = null;
        metric.lastAlarmType = null;
        metric.alarmTriggered = false;
      }
    });
    
    // 批量更新
    metrics.value = newMetrics;
    
    // 更新最后刷新时间
    lastRefreshTime.value = new Date();
    
    // 如果有新的报警，播放报警音
    if (hasNewAlarm && alarmSoundEnabled.value) {
      playAlarm();
    }
    
  } catch (error) {
    console.error("加载数据失败:", error);
  } finally {
    // 短暂延迟确保DOM更新
    setTimeout(() => {
      isRefreshing.value = false;
    }, 50);
  }
};

// 手动刷新
const manualRefresh = async () => {
  if (isRefreshing.value) return;
  
  // 临时禁用自动刷新倒计时
  const wasAutoEnabled = autoRefreshEnabled.value;
  if (wasAutoEnabled) {
    stopCountdownTimer();
  }
  
  await loadData();
  
  // 重新启动倒计时
  if (wasAutoEnabled && refreshInterval.value > 0) {
    startCountdownTimer();
  }
};

// 播放告警音
const playAlarm = () => {
  try {
    if (!alarmAudio) {
      alarmAudio = new Audio("/data/alarm.mp3");
      // 预加载
      alarmAudio.load();
    }
    
    // 重置并播放
    alarmAudio.currentTime = 0;
    alarmAudio.play().catch(e => {
      console.warn("告警音播放失败:", e);
    });
  } catch (error) {
    console.warn("告警音播放失败:", error);
  }
};

// 设备切换处理
const onDeviceChange = () => {
  const device = devices.value.find(d => d.deviceId === deviceId.value);
  if (device) {
    tablename.value = device.deviceTable;
    restartMonitoring();
  }
};

// 启动定时器
const startTimer = () => {
  if (!autoRefreshEnabled.value || refreshInterval.value <= 0) return;
  startCountdownTimer();
};

// 清除定时器
const clearTimer = () => {
  stopCountdownTimer();
};

// 重新开始监控
const restartMonitoring = async () => {
  clearTimer();
  
  await loadDeviceTable();
  
  // 立即加载一次数据
  await loadData();
  
  // 启动定时刷新
  if (autoRefreshEnabled.value && refreshInterval.value > 0) {
    startTimer();
  }
};

// 切换自动刷新
const toggleAutoRefresh = (enabled) => {
  if (enabled && refreshInterval.value > 0) {
    startTimer();
  } else {
    clearTimer();
  }
};

// 监听刷新频率变化
watch(refreshInterval, (newInterval) => {
  if (newInterval === 0) {
    autoRefreshEnabled.value = false;
    clearTimer();
  } else if (autoRefreshEnabled.value) {
    clearTimer();
    startTimer();
  }
});

// 监听自动刷新状态变化
watch(autoRefreshEnabled, (enabled) => {
  if (enabled && refreshInterval.value > 0) {
    startTimer();
  } else {
    clearTimer();
  }
});

// 监听最后刷新时间变化
watch(lastRefreshTime, () => {
  if (autoRefreshEnabled.value && refreshInterval.value > 0) {
    // 重置倒计时
    currentCountdown.value = calculateNextRefreshTime();
  }
});

// 监听告警音开关
watch(alarmSoundEnabled, (enabled) => {
  if (!enabled && alarmAudio) {
    alarmAudio.pause();
  }
});

// 启动监控
const startMonitoring = async () => {
  await initDevices();
  if (deviceId.value) {
    await restartMonitoring();
  }
};

onMounted(() => {
  startMonitoring();
});

onUnmounted(() => {
  // 清理所有定时器
  clearTimer();
  
  // 清理音频资源
  if (alarmAudio) {
    alarmAudio.pause();
    alarmAudio = null;
  }
});
</script>

<style scoped>
/* 不同类型的报警样式 - 只在启用了告警时应用 */
.alarm-high, .alarm-low, .alarm-range {
  border: 2px solid #f56c6c !important;
  background-color: #fff5f5 !important;
  animation: pulse-red 1s infinite;
}

/* 添加一个通用的告警类 - 只在启用了告警时应用 */
.alarm-critical {
  background-color: rgba(245, 108, 108, 0.1) !important;
}

@keyframes pulse-red {
  0%, 100% { 
    border-color: #f56c6c; 
    box-shadow: 0 0 8px rgba(245, 108, 108, 0.6); 
  }
  50% { 
    border-color: rgba(245, 108, 108, 0.5); 
    box-shadow: 0 0 12px rgba(245, 108, 108, 0.8); 
  }
}

.metric-card {
  transition: all 0.3s ease;
  min-height: 120px;
  display: flex;
  flex-direction: column;
  justify-content: center;
  position: relative;
  overflow: hidden;
  cursor: pointer;
}

.metric-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15) !important;
}

/* 告警指示器 - 只在启用了告警且触发告警时显示 */
.alarm-indicator {
  width: 8px;
  height: 8px;
  background-color: #f56c6c;
  border-radius: 50%;
  margin-left: 8px;
  animation: blink 1s infinite;
}

@keyframes blink {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.3; }
}

/* 告警文本样式 - 只在启用了告警且触发告警时显示 */
.alarm-text {
  color: #f56c6c !important;
  font-size: 12px;
  margin-top: 5px;
  font-weight: 500;
  display: flex;
  align-items: center;
  animation: text-blink 1s infinite;
}

@keyframes text-blink {
  0%, 100% { opacity: 1; }
  50% { opacity: 0.7; }
}

/* 弹框样式 */
.metric-detail-dialog :deep(.el-dialog) {
  border-radius: 12px;
  overflow: hidden;
}

.metric-detail-dialog :deep(.el-dialog__header) {
  background: linear-gradient(135deg, #409EFF 0%, #337ecc 100%);
  margin: 0;
  padding: 20px;
}

.metric-detail-dialog :deep(.el-dialog__title) {
  color: white;
  font-weight: 600;
  font-size: 18px;
}

.metric-detail-dialog :deep(.el-dialog__headerbtn) {
  top: 20px;
}

.metric-detail-dialog :deep(.el-dialog__headerbtn .el-dialog__close) {
  color: white;
  font-size: 20px;
}

.metric-detail-dialog :deep(.el-dialog__body) {
  padding: 30px;
  max-height: 70vh;
  overflow-y: auto;
}

/* 弹框内容样式 */
.dialog-content {
  font-size: 14px;
}

.info-grid {
  display: flex;
  flex-direction: column;
  gap: 25px;
}

.info-section {
  padding: 20px;
  border-radius: 10px;
  background: #f8f9fa;
  border-left: 4px solid #409EFF;
}

.basic-info {
  border-left-color: #409EFF;
}

.alarm-info {
  border-left-color: #f56c6c;
}

.comparison-info {
  border-left-color: #67c23a;
}

.no-alarm-info {
  border-left-color: #909399;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 8px;
  margin: 0 0 15px 0;
  font-size: 16px;
  color: #303133;
}

.section-title .el-icon {
  font-size: 18px;
}

.info-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 0;
  border-bottom: 1px solid #e8e8e8;
}

.info-row:last-child {
  border-bottom: none;
}

.info-label {
  font-weight: 600;
  color: #606266;
  min-width: 100px;
}

.info-value {
  color: #303133;
  text-align: right;
  max-width: 250px;
  word-break: break-word;
}

/* 状态样式 */
.status-normal {
  color: #67c23a !important;
  font-weight: 500;
}

.status-warning {
  color: #e6a23c !important;
  font-weight: 500;
}

.status-alarm {
  color: #f56c6c !important;
  font-weight: 500;
}

.alarm-value {
  color: #f56c6c !important;
  font-weight: bold;
}

/* 阈值比较样式 */
.comparison-row {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.comparison-item {
  padding: 12px;
  background: white;
  border-radius: 8px;
  border: 1px solid #e8e8e8;
}

.comparison-label {
  font-weight: 600;
  color: #606266;
  margin-bottom: 8px;
  font-size: 13px;
}

.comparison-content {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.comparison-value {
  color: #303133;
  font-size: 14px;
  font-weight: 500;
}

.comparison-result {
  font-size: 13px;
  font-weight: 500;
  display: flex;
  align-items: center;
  gap: 5px;
}

.result-pass {
  color: #67c23a;
}

.result-fail {
  color: #f56c6c;
  font-weight: 600;
}

/* 无告警提示 */
.no-alarm-message, .no-comparison {
  text-align: center;
  color: #909399;
  padding: 20px;
  background: white;
  border-radius: 8px;
  border: 1px dashed #dcdfe6;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
}

.no-alarm-message .el-icon {
  color: #67c23a;
}

/* 弹框底部 */
.dialog-footer {
  display: flex;
  justify-content: center;
}

/* 平滑过渡动画 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.3s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

/* 响应式布局 */
@media (max-width: 768px) {
  .el-col {
    width: 100%;
    margin-bottom: 10px;
  }
  
  .el-col:last-child {
    margin-bottom: 0;
  }
  
  .metric-detail-dialog :deep(.el-dialog) {
    width: 90% !important;
    margin-top: 5vh !important;
  }
  
  .metric-detail-dialog :deep(.el-dialog__body) {
    padding: 20px;
  }
  
  .info-row {
    flex-direction: column;
    align-items: flex-start;
    gap: 5px;
  }
  
  .info-label {
    min-width: auto;
  }
  
  .info-value {
    text-align: left;
    max-width: 100%;
  }
  
  .comparison-content {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }
}
</style>