<template>
  <div class="real-time-chart">
    <div class="chart-header">
      <slot name="header">
        <div class="header-left">
          <h3>{{ title }}</h3>
          <el-tag size="small" :type="statusType">
            {{ statusText }}
          </el-tag>
        </div>
      </slot>
      <div class="chart-controls">
        <slot name="controls">
          <el-button-group size="small">
            <el-button
              :type="timeRange === 15 ? 'primary' : ''"
              @click="changeTimeRange(15)"
            >
              15分钟
            </el-button>
            <el-button
              :type="timeRange === 30 ? 'primary' : ''"
              @click="changeTimeRange(30)"
            >
              30分钟
            </el-button>
            <el-button
              :type="timeRange === 60 ? 'primary' : ''"
              @click="changeTimeRange(60)"
            >
              1小时
            </el-button>
          </el-button-group>
          <el-button
            size="small"
            icon="el-icon-refresh"
            @click="refreshChart"
            :loading="refreshing"
          >
            刷新
          </el-button>
        </slot>
      </div>
    </div>
    
    <div class="chart-container">
      <v-chart
        v-if="chartOption && !loading"
        :option="chartOption"
        :autoresize="true"
        :style="{ height: height + 'px' }"
      />
      <div v-else-if="loading" class="chart-loading">
        <el-skeleton :rows="5" animated />
      </div>
      <div v-else class="chart-empty">
        <empty-state :message="emptyMessage" />
      </div>
    </div>
    
    <div class="chart-footer">
      <slot name="footer">
        <div class="footer-info">
          <div class="info-item">
            <span class="label">最新值:</span>
            <span class="value">{{ latestValue || '--' }}</span>
            <span class="unit">{{ unit }}</span>
          </div>
          <div class="info-item">
            <span class="label">更新时间:</span>
            <span class="value">{{ lastUpdateTime || '--' }}</span>
          </div>
          <div class="info-item">
            <span class="label">数据点数:</span>
            <span class="value">{{ dataCount }}</span>
          </div>
        </div>
      </slot>
    </div>
  </div>
</template>

<script>
import { format } from 'date-fns'
import { throttle } from 'lodash'

export default {
  name: 'RealTimeChart',
  props: {
    title: {
      type: String,
      default: '实时数据'
    },
    data: {
      type: Array,
      default: () => []
    },
    unit: {
      type: String,
      default: ''
    },
    height: {
      type: Number,
      default: 300
    },
    loading: {
      type: Boolean,
      default: false
    },
    emptyMessage: {
      type: String,
      default: '暂无数据'
    },
    thresholds: {
      type: Object,
      default: () => ({})
    },
    showThreshold: {
      type: Boolean,
      default: true
    },
    chartType: {
      type: String,
      default: 'line',
      validator: value => ['line', 'bar', 'scatter'].includes(value)
    },
    smooth: {
      type: Boolean,
      default: true
    },
    color: {
      type: String,
      default: '#36a2eb'
    },
    timeRange: {
      type: Number,
      default: 30
    }
  },
  data() {
    return {
      refreshing: false,
      chartOption: null,
      lastUpdateTime: null,
      latestValue: null,
      dataCount: 0,
      internalTimeRange: this.timeRange
    }
  },
  computed: {
    statusType() {
      if (!this.latestValue || !this.thresholds) return 'info'
      
      const { upper, lower } = this.thresholds
      
      if (upper && this.latestValue > upper) return 'danger'
      if (lower && this.latestValue < lower) return 'warning'
      
      return 'success'
    },
    
    statusText() {
      if (!this.latestValue || !this.thresholds) return '正常'
      
      const { upper, lower } = this.thresholds
      
      if (upper && this.latestValue > upper) return '超上限'
      if (lower && this.latestValue < lower) return '低下限'
      
      return '正常'
    }
  },
  watch: {
    data: {
      handler(newData) {
        this.updateChartData(newData)
      },
      deep: true
    },
    
    timeRange(newRange) {
      this.internalTimeRange = newRange
      this.$emit('time-range-change', newRange)
    },
    
    thresholds: {
      handler() {
        this.updateChart()
      },
      deep: true
    }
  },
  created() {
    this.initChart()
    this.updateChartData(this.data)
    
    // 节流更新
    this.throttledUpdate = throttle(this.updateChart, 1000)
  },
  beforeDestroy() {
    if (this.throttledUpdate) {
      this.throttledUpdate.cancel()
    }
  },
  methods: {
    initChart() {
      this.chartOption = {
        backgroundColor: 'transparent',
        tooltip: {
          trigger: 'axis',
          backgroundColor: 'rgba(255, 255, 255, 0.9)',
          borderColor: '#ccc',
          borderWidth: 1,
          textStyle: {
            color: '#333'
          },
          axisPointer: {
            type: 'cross',
            label: {
              backgroundColor: '#6a7985'
            }
          },
          formatter: this.tooltipFormatter
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
          boundaryGap: false,
          axisLine: {
            lineStyle: {
              color: '#ccc'
            }
          },
          axisLabel: {
            color: '#666',
            formatter: value => {
              return format(new Date(value), 'HH:mm')
            }
          },
          splitLine: {
            show: true,
            lineStyle: {
              type: 'dashed',
              color: '#eee'
            }
          }
        },
        yAxis: {
          type: 'value',
          name: this.unit,
          nameTextStyle: {
            color: '#666',
            padding: [0, 0, 0, 10]
          },
          axisLine: {
            show: true,
            lineStyle: {
              color: '#ccc'
            }
          },
          axisLabel: {
            color: '#666'
          },
          splitLine: {
            show: true,
            lineStyle: {
              type: 'dashed',
              color: '#eee'
            }
          }
        },
        series: []
      }
    },
    
    updateChartData(data) {
      if (!data || data.length === 0) {
        this.dataCount = 0
        this.latestValue = null
        this.lastUpdateTime = null
        return
      }
      
      // 处理数据
      const chartData = data.map(item => ({
        name: new Date(item.timestamp),
        value: [new Date(item.timestamp), item.value],
        raw: item
      }))
      
      // 更新统计信息
      this.dataCount = chartData.length
      const lastData = chartData[chartData.length - 1]
      this.latestValue = lastData.value[1]
      this.lastUpdateTime = format(lastData.value[0], 'HH:mm:ss')
      
      // 更新图表
      this.updateSeries(chartData)
      this.updateThresholdMark()
      
      // 触发更新
      this.throttledUpdate()
    },
    
    updateSeries(chartData) {
      const series = {
        name: this.title,
        type: this.chartType,
        smooth: this.smooth,
        symbol: 'circle',
        symbolSize: 4,
        showSymbol: chartData.length < 50, // 数据点多时不显示符号
        lineStyle: {
          width: 2,
          color: this.color
        },
        itemStyle: {
          color: this.color
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
              color: this.color + '40'
            }, {
              offset: 1,
              color: this.color + '00'
            }]
          }
        } : undefined,
        data: chartData.map(item => item.value)
      }
      
      this.chartOption.series = [series]
    },
    
    updateThresholdMark() {
      if (!this.showThreshold || !this.thresholds) return
      
      const { upper, lower } = this.thresholds
      const markLineData = []
      const markAreaData = []
      
      // 添加阈值线
      if (upper !== undefined) {
        markLineData.push({
          name: '上限',
          yAxis: upper,
          lineStyle: {
            type: 'dashed',
            color: '#f56c6c'
          },
          label: {
            formatter: '上限: {c}'
          }
        })
        
        markAreaData.push([
          {
            yAxis: upper,
            itemStyle: {
              color: 'rgba(245, 108, 108, 0.1)'
            }
          },
          {
            yAxis: Infinity
          }
        ])
      }
      
      if (lower !== undefined) {
        markLineData.push({
          name: '下限',
          yAxis: lower,
          lineStyle: {
            type: 'dashed',
            color: '#e6a23c'
          },
          label: {
            formatter: '下限: {c}'
          }
        })
        
        markAreaData.push([
          {
            yAxis: -Infinity
          },
          {
            yAxis: lower,
            itemStyle: {
              color: 'rgba(230, 162, 60, 0.1)'
            }
          }
        ])
      }
      
      if (markLineData.length > 0) {
        this.chartOption.series[0].markLine = {
          data: markLineData,
          symbol: ['none', 'none'],
          lineStyle: {
            type: 'dashed'
          }
        }
      }
      
      if (markAreaData.length > 0) {
        this.chartOption.series[0].markArea = {
          data: markAreaData,
          silent: true
        }
      }
    },
    
    updateChart() {
      if (this.chartOption) {
        this.chartOption = { ...this.chartOption }
      }
    },
    
    tooltipFormatter(params) {
      const date = new Date(params[0].value[0])
      const time = format(date, 'yyyy-MM-dd HH:mm:ss')
      const value = params[0].value[1]
      
      let html = `<div style="font-weight: bold; margin-bottom: 5px;">${time}</div>`
      html += `<div>${this.title}: <span style="color: ${this.color}; font-weight: bold;">${value}${this.unit}</span></div>`
      
      // 添加阈值信息
      if (this.thresholds) {
        const { upper, lower } = this.thresholds
        if (upper !== undefined) {
          const status = value > upper ? '❌ 超上限' : '✅ 正常'
          html += `<div>上限: ${upper}${this.unit} ${status}</div>`
        }
        if (lower !== undefined) {
          const status = value < lower ? '❌ 低下限' : '✅ 正常'
          html += `<div>下限: ${lower}${this.unit} ${status}</div>`
        }
      }
      
      return html
    },
    
    changeTimeRange(range) {
      this.internalTimeRange = range
      this.$emit('time-range-change', range)
    },
    
    async refreshChart() {
      this.refreshing = true
      try {
        await this.$emit('refresh')
        this.$message.success('图表已刷新')
      } catch (error) {
        console.error('刷新图表失败:', error)
      } finally {
        this.refreshing = false
      }
    }
  }
}
</script>

<style lang="scss" scoped>
.real-time-chart {
  background: var(--bg-color-primary);
  border-radius: 8px;
  border: 1px solid var(--border-color);
  overflow: hidden;
  
  .chart-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    padding: 15px 20px;
    border-bottom: 1px solid var(--border-color);
    background: var(--bg-color-secondary);
    
    .header-left {
      display: flex;
      align-items: center;
      gap: 10px;
      
      h3 {
        margin: 0;
        font-size: 16px;
        font-weight: 600;
        color: var(--text-color-primary);
      }
    }
    
    .chart-controls {
      display: flex;
      align-items: center;
      gap: 10px;
    }
  }
  
  .chart-container {
    padding: 20px;
    position: relative;
    
    .chart-loading,
    .chart-empty {
      height: 300px;
      display: flex;
      align-items: center;
      justify-content: center;
    }
  }
  
  .chart-footer {
    padding: 15px 20px;
    border-top: 1px solid var(--border-color);
    background: var(--bg-color-secondary);
    
    .footer-info {
      display: flex;
      align-items: center;
      justify-content: space-between;
      
      .info-item {
        display: flex;
        align-items: center;
        gap: 5px;
        
        .label {
          color: var(--text-color-secondary);
          font-size: 14px;
        }
        
        .value {
          color: var(--text-color-primary);
          font-weight: 600;
          font-size: 14px;
        }
        
        .unit {
          color: var(--text-color-secondary);
          font-size: 12px;
        }
      }
    }
  }
}

@media (max-width: 768px) {
  .real-time-chart {
    .chart-header {
      flex-direction: column;
      align-items: flex-start;
      gap: 10px;
      
      .chart-controls {
        width: 100%;
        justify-content: space-between;
      }
    }
    
    .chart-footer {
      .footer-info {
        flex-direction: column;
        align-items: flex-start;
        gap: 10px;
      }
    }
  }
}
</style>