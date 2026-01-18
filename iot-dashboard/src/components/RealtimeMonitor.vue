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
        <el-card :class="{ alarm: metric.alarm }" shadow="hover" class="metric-card">
          <div style="font-weight: bold; margin-bottom: 8px;">{{ metric.label }}</div>
          <transition name="fade" mode="out-in">
            <div :key="metric.value" style="font-size: 24px; font-weight: bold; color: #409EFF;">
              {{ metric.value }}
            </div>
          </transition>
          <transition name="fade">
            <div v-if="metric.alarm" style="color: red; font-size: 12px; margin-top: 5px;">
              <el-icon><Warning /></el-icon>
              告警
            </div>
          </transition>
        </el-card>
      </el-col>
    </el-row>
  </el-card>
</template>

<script setup>
import { ref, onMounted, onUnmounted, watch, computed, nextTick } from "vue";
import { Refresh, Clock, Warning } from '@element-plus/icons-vue';
import {
  getDevices,
  getDeviceTable,
  getDataByTableName,
} from "../api/device";

const devices = ref([]);
const deviceId = ref(null);
const tablename = ref(null);
const metrics = ref([]);

// 使用计算属性返回可见的指标，避免直接修改metrics导致闪烁
const visibleMetrics = computed(() => {
  return metrics.value.filter(m => m.isVisible !== false);
});

// 刷新相关状态
const refreshInterval = ref(60000); // 默认1分钟（60000毫秒）
const autoRefreshEnabled = ref(true); // 默认开启自动刷新
const isRefreshing = ref(false);
const lastRefreshTime = ref(null);
const countdownInterval = ref(1000); // 倒计时更新间隔1秒
const timer = ref(null);
const countdownTimer = ref(null); // 倒计时定时器
const currentCountdown = ref(0); // 当前倒计时秒数

const alarmAudio = new Audio("/data/alarm.mp3");

// 格式化时间显示
const formatTime = (date) => {
  return date.toLocaleTimeString('zh-CN', { 
    hour12: false,
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit'
  });
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
  
  // 立即计算一次
  updateCountdown();
  
  // 每秒更新一次
  countdownTimer.value = setInterval(() => {
    updateCountdown();
  }, countdownInterval.value);
};

// 停止倒计时定时器
const stopCountdownTimer = () => {
  if (countdownTimer.value) {
    clearInterval(countdownTimer.value);
    countdownTimer.value = null;
  }
  currentCountdown.value = 0;
};

// 更新倒计时
const updateCountdown = () => {
  const timeUntilNext = calculateNextRefreshTime();
  currentCountdown.value = timeUntilNext;
  
  // 如果倒计时小于等于1秒，触发自动刷新
  if (timeUntilNext <= 1000 && autoRefreshEnabled.value && !isRefreshing.value) {
    loadData();
  }
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
    
    // 初始化metrics，设置默认值
    const initialMetrics = res.map((m) => ({
      label: m.displayName,
      value: "-", // 初始值为"-"
      alarm: false,
      fieldName: m.fieldName,
      unit: m.unit || '',
      threshold: m.threshold || null,
      isVisible: m.isVisible !== false,
    }));
    
    // 使用nextTick确保DOM更新完成
    await nextTick();
    metrics.value = initialMetrics;
    
  } catch (error) {
    console.error("加载设备因子失败:", error);
  }
};

// 检查是否需要告警
const checkAlarm = (metric, value) => {
  if (!metric.threshold) return false;
  
  const numValue = parseFloat(value);
  if (isNaN(numValue)) return false;
  
  // 这里可以根据实际需求设置告警逻辑
  // 示例：超过阈值触发告警
  return numValue > metric.threshold;
};

// 加载实时数据（优化版，减少闪烁）
const loadData = async (isManual = false) => {
  if (!tablename.value || metrics.value.length === 0) return;
  
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
    let hasAlarm = false;
    
    // 先计算新数据，避免在循环中直接修改导致频繁更新
    const updatedMetrics = [...metrics.value];
    
    updatedMetrics.forEach((metric) => {
      const rawValue = seledata[metric.fieldName];
      
      if (rawValue !== undefined && rawValue !== null && rawValue !== "") {
        // 处理字符串类型的值
        let displayValue;
        if (typeof rawValue === 'string') {
          // 如果是日期时间类型，替换 T 为空格
          displayValue = rawValue.includes('T') 
            ? rawValue.replace(/T/g, ' ')
            : rawValue;
        } else {
          displayValue = rawValue;
        }
        
        // 只有在值发生变化时才更新
        if (metric.value !== displayValue) {
          metric.value = displayValue;
        }
        
        // 检查告警状态
        const newAlarmState = checkAlarm(metric, rawValue);
        if (metric.alarm !== newAlarmState) {
          metric.alarm = newAlarmState;
        }
        
        if (newAlarmState) {
          hasAlarm = true;
        }
      } else {
        // 只有在值发生变化时才更新
        if (metric.value !== "-") {
          metric.value = "-";
          metric.alarm = false;
        }
      }
    });
    
    // 批量更新，减少DOM操作
    metrics.value = updatedMetrics;
    
    // 更新最后刷新时间
    lastRefreshTime.value = new Date();
    
    // 重置倒计时
    if (autoRefreshEnabled.value && refreshInterval.value > 0) {
      updateCountdown();
    }
    
    // 检查是否需要播放告警音
    if (hasAlarm) {
      playAlarm();
    }
    
  } catch (error) {
    console.error("加载数据失败:", error);
  } finally {
    // 使用setTimeout确保DOM更新完成后再设置isRefreshing为false
    setTimeout(() => {
      isRefreshing.value = false;
    }, 50);
  }
};

// 手动刷新
const manualRefresh = async () => {
  if (isRefreshing.value) return;
  
  // 如果是手动刷新模式，临时禁用自动刷新
  if (autoRefreshEnabled.value) {
    stopCountdownTimer();
  }
  
  await loadData(true);
  
  // 重新启动倒计时
  if (autoRefreshEnabled.value && refreshInterval.value > 0) {
    startCountdownTimer();
  }
};

// 播放告警音
const playAlarm = () => {
  try {
    alarmAudio.currentTime = 0;
    alarmAudio.play().catch(e => console.warn("告警音播放失败:", e));
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

// 启动定时器（优化为使用倒计时触发）
const startTimer = () => {
  if (!autoRefreshEnabled.value || refreshInterval.value <= 0) return;
  
  // 使用倒计时机制替代固定间隔定时器
  startCountdownTimer();
};

// 清除定时器
const clearTimer = () => {
  stopCountdownTimer();
};

// 重新开始监控
const restartMonitoring = () => {
  clearTimer();
  
  // 重新加载设备因子
  loadDeviceTable().then(() => {
    // 立即加载一次数据
    loadData();
    
    // 启动定时刷新
    if (autoRefreshEnabled.value && refreshInterval.value > 0) {
      startTimer();
    }
  });
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
    updateCountdown();
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
  clearTimer();
});
</script>

<style scoped>
.alarm {
  border: 2px solid red;
  animation: blink 1s infinite;
  background-color: #fff5f5;
}

@keyframes blink {
  0%, 100% { border-color: red; }
  50% { border-color: transparent; }
}

.metric-card {
  transition: all 0.3s ease;
  min-height: 120px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.metric-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
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

/* 数值变化时的平滑动画 */
.value-transition {
  transition: all 0.3s ease;
}

/* 倒计时样式 */
.countdown {
  display: inline-block;
  min-width: 60px;
  text-align: center;
  font-family: 'Courier New', monospace;
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
}
</style>