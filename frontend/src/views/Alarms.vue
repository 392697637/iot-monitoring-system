<template>
  <div class="alarms-container">
    <!-- 页面标题 -->
    <div class="page-header">
      <h1 class="page-title">
        <i class="el-icon-warning"></i>
        报警管理
        <el-badge 
          :value="unacknowledgedCount" 
          :max="99"
          class="badge"
          v-if="unacknowledgedCount > 0"
        />
      </h1>
      <div class="page-actions">
        <el-button
          type="primary"
          icon="el-icon-check"
          @click="handleBatchAcknowledge"
          :disabled="selectedAlarms.length === 0"
        >
          批量确认({{ selectedAlarms.length }})
        </el-button>
        <el-button
          type="danger"
          icon="el-icon-delete"
          @click="handleCleanup"
        >
          清理过期
        </el-button>
        <el-button
          type="info"
          icon="el-icon-setting"
          @click="showSettings"
        >
          报警设置
        </el-button>
      </div>
    </div>

    <!-- 报警统计 -->
    <div class="alarm-stats">
      <el-row :gutter="20">
        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card total">
            <div class="stat-content">
              <div class="stat-icon">
                <i class="el-icon-warning-outline"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">今日报警</div>
                <div class="stat-value">{{ stats.todayAlarms }}</div>
                <div class="stat-trend" :class="getTrendClass(stats.todayTrend)">
                  <i :class="getTrendIcon(stats.todayTrend)"></i>
                  {{ stats.todayTrend > 0 ? '+' : '' }}{{ Math.abs(stats.todayTrend) }}%
                </div>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card unacknowledged">
            <div class="stat-content">
              <div class="stat-icon">
                <i class="el-icon-bell"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">未确认</div>
                <div class="stat-value">{{ stats.unacknowledgedAlarms }}</div>
                <div class="stat-trend">
                  {{ stats.acknowledgeRate }}% 已确认
                </div>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card severe">
            <div class="stat-content">
              <div class="stat-icon">
                <i class="el-icon-error"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">严重报警</div>
                <div class="stat-value">{{ stats.severeAlarms }}</div>
                <div class="stat-trend">
                  {{ stats.severeRate }}% 占比
                </div>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card duration">
            <div class="stat-content">
              <div class="stat-icon">
                <i class="el-icon-timer"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">平均持续时间</div>
                <div class="stat-value">{{ formatDuration(stats.avgDuration) }}</div>
                <div class="stat-trend">
                  {{ stats.longestDuration }} 最长
                </div>
              </div>
            </div>
          </el-card>
        </el-col>
      </el-row>
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
        @submit.native.prevent
      >
        <el-row :gutter="20">
          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="设备" prop="deviceId">
              <el-select
                v-model="queryForm.deviceId"
                placeholder="选择设备"
                clearable
                filterable
                style="width: 100%;"
              >
                <el-option
                  v-for="device in devices"
                  :key="device.id"
                  :label="device.deviceName"
                  :value="device.id"
                />
              </el-select>
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="报警类型" prop="factorType">
              <el-select
                v-model="queryForm.factorType"
                placeholder="选择类型"
                clearable
                style="width: 100%;"
              >
                <el-option label="温度" value="Temperature" />
                <el-option label="湿度" value="Humidity" />
                <el-option label="电流" value="Current" />
                <el-option label="电压" value="Voltage" />
              </el-select>
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="确认状态" prop="isAcknowledged">
              <el-select
                v-model="queryForm.isAcknowledged"
                placeholder="选择状态"
                clearable
                style="width: 100%;"
              >
                <el-option label="未确认" :value="false" />
                <el-option label="已确认" :value="true" />
              </el-select>
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="报警级别" prop="severity">
              <el-select
                v-model="queryForm.severity"
                placeholder="选择级别"
                clearable
                style="width: 100%;"
              >
                <el-option label="严重" value="severe" />
                <el-option label="警告" value="warning" />
                <el-option label="提示" value="info" />
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
                <el-form-item label="阈值类型" prop="limitType">
                  <el-select
                    v-model="queryForm.limitType"
                    placeholder="选择类型"
                    clearable
                    style="width: 100%;"
                  >
                    <el-option label="超过上限" value="Upper" />
                    <el-option label="低于下限" value="Lower" />
                  </el-select>
                </el-form-item>
              </el-col>

              <el-col :xs="24" :sm="12" :md="8" :lg="6">
                <el-form-item label="持续时间" prop="duration">
                  <el-input-number
                    v-model="queryForm.duration"
                    placeholder="分钟"
                    :min="0"
                    :step="1"
                    style="width: 100%;"
                  >
                    <template slot="append">分钟</template>
                  </el-input-number>
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

    <!-- 报警趋势图 -->
    <el-card shadow="never" class="trend-card" v-if="trendData.length > 0">
      <div slot="header" class="chart-header">
        <span>报警趋势分析</span>
        <div class="chart-controls">
          <el-radio-group v-model="trendType" size="small">
            <el-radio-button label="day">按日</el-radio-button>
            <el-radio-button label="hour">按时</el-radio-button>
            <el-radio-button label="factor">按因子</el-radio-button>
          </el-radio-group>
        </div>
      </div>
      <div class="chart-container">
        <v-chart
          :option="trendChartOption"
          :autoresize="true"
          style="height: 300px;"
        />
      </div>
    </el-card>

    <!-- 报警列表 -->
    <el-card shadow="never" class="table-card">
      <div slot="header" class="table-header">
        <span>报警列表</span>
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
          </el-select>
          <el-button
            size="small"
            icon="el-icon-download"
            @click="exportAlarms"
          >
            导出
          </el-button>
        </div>
      </div>

      <div class="table-container">
        <el-table
          ref="alarmTable"
          :data="tableData"
          v-loading="loading"
          element-loading-text="数据加载中..."
          element-loading-spinner="el-icon-loading"
          element-loading-background="rgba(0, 0, 0, 0.1)"
          stripe
          border
          highlight-current-row
          style="width: 100%;"
          @selection-change="handleSelectionChange"
          @sort-change="handleSortChange"
        >
          <el-table-column
            type="selection"
            width="55"
            align="center"
          />
          
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
              <div class="time-cell">
                <div class="time-main">{{ formatTime(row.timestamp) }}</div>
                <div class="time-duration" v-if="row.duration">
                  {{ formatDuration(row.duration) }}
                </div>
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="deviceName"
            label="设备"
            width="150"
            align="center"
          >
            <template slot-scope="{ row }">
              <div class="device-cell">
                <div class="device-name">{{ row.deviceName }}</div>
                <div class="device-code">{{ row.deviceCode || '--' }}</div>
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="factorName"
            label="报警因子"
            width="120"
            align="center"
          >
            <template slot-scope="{ row }">
              <div class="factor-cell">
                <el-tag
                  size="small"
                  :type="getFactorTagType(row.factorType)"
                >
                  {{ row.factorName }}
                </el-tag>
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="value"
            label="当前值"
            width="120"
            align="center"
            sortable="custom"
          >
            <template slot-scope="{ row }">
              <div class="value-cell" :class="getValueClass(row)">
                {{ row.value !== null ? row.value.toFixed(2) : '--' }}
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="limitType"
            label="报警类型"
            width="120"
            align="center"
          >
            <template slot-scope="{ row }">
              <el-tag
                :type="row.limitType === 'Upper' ? 'danger' : 'warning'"
                size="small"
                effect="plain"
              >
                {{ row.limitType === 'Upper' ? '超过上限' : '低于下限' }}
              </el-tag>
            </template>
          </el-table-column>

          <el-table-column
            prop="message"
            label="报警信息"
            min-width="200"
            show-overflow-tooltip
          >
            <template slot-scope="{ row }">
              <div class="message-cell">
                {{ row.message }}
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="isAcknowledged"
            label="确认状态"
            width="120"
            align="center"
          >
            <template slot-scope="{ row }">
              <el-tag
                :type="row.isAcknowledged ? 'success' : 'danger'"
                size="small"
                :effect="row.isAcknowledged ? 'plain' : 'dark'"
              >
                {{ row.isAcknowledged ? '已确认' : '未确认' }}
              </el-tag>
            </template>
          </el-table-column>

          <el-table-column
            label="操作"
            width="180"
            align="center"
            fixed="right"
          >
            <template slot-scope="{ row }">
              <el-button
                type="primary"
                size="mini"
                plain
                @click="viewAlarmDetails(row)"
              >
                详情
              </el-button>
              <el-button
                type="success"
                size="mini"
                plain
                @click="acknowledgeAlarm(row)"
                v-if="!row.isAcknowledged"
              >
                确认
              </el-button>
              <el-button
                type="danger"
                size="mini"
                plain
                @click="deleteAlarm(row)"
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
          <template v-slot:icon>
            <i class="el-icon-bell" style="font-size: 48px; color: #909399;"></i>
          </template>
          <template v-slot:action>
            <el-button type="primary" @click="handleQuery">重新查询</el-button>
          </template>
        </empty-state>
      </div>

      <!-- 分页 -->
      <div class="pagination-container" v-if="total > 0">
        <el-pagination
          :current-page="currentPage"
          :page-sizes="[20, 50, 100]"
          :page-size="pageSize"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handlePageSizeChange"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

    <!-- 报警详情对话框 -->
    <el-dialog
      :title="currentAlarm ? '报警详情' : '报警详情'"
      :visible.sync="detailDialogVisible"
      width="700px"
      :close-on-click-modal="false"
    >
      <div v-if="currentAlarm" class="alarm-details">
        <el-descriptions :column="2" border>
          <el-descriptions-item label="报警ID">
            {{ currentAlarm.id }}
          </el-descriptions-item>
          <el-descriptions-item label="设备名称">
            {{ currentAlarm.deviceName }}
          </el-descriptions-item>
          <el-descriptions-item label="发生时间">
            {{ formatDateTime(currentAlarm.timestamp) }}
          </el-descriptions-item>
          <el-descriptions-item label="持续时间">
            {{ formatDuration(currentAlarm.duration) }}
          </el-descriptions-item>
          <el-descriptions-item label="报警因子">
            <el-tag :type="getFactorTagType(currentAlarm.factorType)">
              {{ currentAlarm.factorName }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="报警类型">
            <el-tag :type="currentAlarm.limitType === 'Upper' ? 'danger' : 'warning'">
              {{ currentAlarm.limitType === 'Upper' ? '超过上限' : '低于下限' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="当前值">
            <div class="detail-value">
              {{ currentAlarm.value !== null ? currentAlarm.value.toFixed(2) : '--' }}
              <span class="unit">{{ getFactorUnit(currentAlarm.factorType) }}</span>
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="阈值信息">
            <div class="threshold-info">
              {{ getThresholdInfo(currentAlarm) }}
            </div>
          </el-descriptions-item>
          <el-descriptions-item label="报警信息" :span="2">
            {{ currentAlarm.message }}
          </el-descriptions-item>
          <el-descriptions-item label="确认状态">
            <el-tag :type="currentAlarm.isAcknowledged ? 'success' : 'danger'">
              {{ currentAlarm.isAcknowledged ? '已确认' : '未确认' }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="确认时间" v-if="currentAlarm.isAcknowledged">
            {{ formatDateTime(currentAlarm.acknowledgedAt) }}
          </el-descriptions-item>
        </el-descriptions>

        <div class="alarm-history" v-if="alarmHistory.length > 0">
          <h4>同类报警历史</h4>
          <el-table :data="alarmHistory" size="small">
            <el-table-column prop="timestamp" label="时间" width="180">
              <template slot-scope="{ row }">
                {{ formatTime(row.timestamp) }}
              </template>
            </el-table-column>
            <el-table-column prop="value" label="值" width="100" />
            <el-table-column prop="limitType" label="类型" width="100">
              <template slot-scope="{ row }">
                <el-tag :type="row.limitType === 'Upper' ? 'danger' : 'warning'" size="small">
                  {{ row.limitType === 'Upper' ? '超上限' : '低下限' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="isAcknowledged" label="状态" width="100">
              <template slot-scope="{ row }">
                <el-tag :type="row.isAcknowledged ? 'success' : 'danger'" size="small">
                  {{ row.isAcknowledged ? '已确认' : '未确认' }}
                </el-tag>
              </template>
            </el-table-column>
            <el-table-column prop="message" label="信息" />
          </el-table>
        </div>

        <div class="alarm-actions" v-if="!currentAlarm.isAcknowledged">
          <el-alert
            title="请及时处理未确认的报警"
            type="warning"
            :closable="false"
            show-icon
          />
          <div class="action-buttons">
            <el-button
              type="primary"
              @click="acknowledgeCurrentAlarm"
            >
              确认报警
            </el-button>
            <el-button
              type="warning"
              @click="handleIgnore"
            >
              忽略报警
            </el-button>
            <el-button
              type="info"
              @click="handleAdjustThreshold"
            >
              调整阈值
            </el-button>
          </div>
        </div>
      </div>
      <span slot="footer" class="dialog-footer">
        <el-button @click="detailDialogVisible = false">关闭</el-button>
        <el-button
          type="primary"
          @click="viewDeviceDashboard"
          v-if="currentAlarm"
        >
          查看设备监控
        </el-button>
      </span>
    </el-dialog>

    <!-- 报警设置对话框 -->
    <el-dialog
      title="报警设置"
      :visible.sync="settingsDialogVisible"
      width="600px"
    >
      <el-tabs v-model="settingsActiveTab">
        <el-tab-pane label="基本设置" name="basic">
          <el-form :model="settingsForm" label-width="120px">
            <el-form-item label="报警声音">
              <el-switch v-model="settingsForm.alarmSound" />
              <span class="form-tip">启用报警声音提示</span>
            </el-form-item>
            <el-form-item label="声音类型">
              <el-select v-model="settingsForm.soundType" :disabled="!settingsForm.alarmSound">
                <el-option label="标准提示音" value="standard" />
                <el-option label="紧急报警音" value="urgent" />
                <el-option label="自定义声音" value="custom" />
              </el-select>
            </el-form-item>
            <el-form-item label="桌面通知">
              <el-switch v-model="settingsForm.desktopNotification" />
              <span class="form-tip">在桌面显示通知</span>
            </el-form-item>
            <el-form-item label="自动刷新">
              <el-switch v-model="settingsForm.autoRefresh" />
              <span class="form-tip">自动刷新报警列表</span>
            </el-form-item>
            <el-form-item label="刷新间隔" v-if="settingsForm.autoRefresh">
              <el-select v-model="settingsForm.refreshInterval">
                <el-option label="30秒" :value="30" />
                <el-option label="1分钟" :value="60" />
                <el-option label="5分钟" :value="300" />
                <el-option label="10分钟" :value="600" />
              </el-select>
            </el-form-item>
          </el-form>
        </el-tab-pane>
        
        <el-tab-pane label="过滤设置" name="filter">
          <el-form :model="settingsForm" label-width="120px">
            <el-form-item label="显示未确认">
              <el-switch v-model="settingsForm.showUnacknowledged" />
              <span class="form-tip">默认显示未确认报警</span>
            </el-form-item>
            <el-form-item label="显示已确认">
              <el-switch v-model="settingsForm.showAcknowledged" />
              <span class="form-tip">显示已确认的报警</span>
            </el-form-item>
            <el-form-item label="报警级别过滤">
              <el-checkbox-group v-model="settingsForm.severityFilter">
                <el-checkbox label="severe">严重</el-checkbox>
                <el-checkbox label="warning">警告</el-checkbox>
                <el-checkbox label="info">提示</el-checkbox>
              </el-checkbox-group>
            </el-form-item>
            <el-form-item label="默认时间范围">
              <el-select v-model="settingsForm.defaultTimeRange">
                <el-option label="今天" value="today" />
                <el-option label="最近24小时" value="24h" />
                <el-option label="最近一周" value="week" />
                <el-option label="最近一月" value="month" />
              </el-select>
            </el-form-item>
          </el-form>
        </el-tab-pane>
        
        <el-tab-pane label="通知设置" name="notification">
          <el-form :model="settingsForm" label-width="120px">
            <el-form-item label="邮件通知">
              <el-switch v-model="settingsForm.emailNotification" />
            </el-form-item>
            <el-form-item label="邮件接收人">
              <el-input
                v-model="settingsForm.emailRecipients"
                placeholder="多个邮箱用逗号分隔"
                :disabled="!settingsForm.emailNotification"
              />
            </el-form-item>
            <el-form-item label="短信通知">
              <el-switch v-model="settingsForm.smsNotification" />
            </el-form-item>
            <el-form-item label="短信接收人">
              <el-input
                v-model="settingsForm.smsRecipients"
                placeholder="多个手机号用逗号分隔"
                :disabled="!settingsForm.smsNotification"
              />
            </el-form-item>
            <el-form-item label="通知级别">
              <el-select v-model="settingsForm.notificationLevel">
                <el-option label="仅严重报警" value="severe" />
                <el-option label="严重和警告" value="warning" />
                <el-option label="所有报警" value="all" />
              </el-select>
            </el-form-item>
          </el-form>
        </el-tab-pane>
      </el-tabs>
      <span slot="footer" class="dialog-footer">
        <el-button @click="settingsDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveSettings">保存设置</el-button>
      </span>
    </el-dialog>
  </div>
</template>

<script>
import { mapState, mapActions } from 'vuex'
import { format, subDays, startOfDay, endOfDay, subHours, subMonths } from 'date-fns'
import EmptyState from '@/components/common/EmptyState.vue'

export default {
  name: 'Alarms',
  components: {
    EmptyState
  },
  data() {
    return {
      // 查询表单
      queryForm: {
        deviceId: null,
        factorType: null,
        isAcknowledged: null,
        severity: null,
        startTime: null,
        endTime: null,
        limitType: null,
        duration: null
      },
      
      // 表格数据
      tableData: [],
      selectedAlarms: [],
      loading: false,
      queryLoading: false,
      
      // 分页
      currentPage: 1,
      pageSize: 50,
      total: 0,
      sortField: 'timestamp',
      sortOrder: 'desc',
      
      // 统计
      stats: {
        todayAlarms: 0,
        todayTrend: 0,
        unacknowledgedAlarms: 0,
        acknowledgeRate: 0,
        severeAlarms: 0,
        severeRate: 0,
        avgDuration: 0,
        longestDuration: 0
      },
      
      // 图表
      trendType: 'day',
      trendData: [],
      trendChartOption: {},
      
      // 对话框
      showAdvancedQuery: false,
      detailDialogVisible: false,
      settingsDialogVisible: false,
      currentAlarm: null,
      alarmHistory: [],
      
      // 设置
      settingsActiveTab: 'basic',
      settingsForm: {
        alarmSound: true,
        soundType: 'standard',
        desktopNotification: true,
        autoRefresh: false,
        refreshInterval: 60,
        showUnacknowledged: true,
        showAcknowledged: false,
        severityFilter: ['severe', 'warning'],
        defaultTimeRange: '24h',
        emailNotification: false,
        emailRecipients: '',
        smsNotification: false,
        smsRecipients: '',
        notificationLevel: 'severe'
      },
      
      // 空状态信息
      emptyMessage: '暂无报警记录',
      
      // 刷新定时器
      refreshTimer: null
    }
  },
  computed: {
    ...mapState('devices', ['devices']),
    ...mapState('alarms', ['unacknowledgedCount']),
    
    unacknowledgedAlarms() {
      return this.tableData.filter(alarm => !alarm.isAcknowledged)
    }
  },
  watch: {
    'settingsForm.autoRefresh'(enabled) {
      if (enabled) {
        this.startAutoRefresh()
      } else {
        this.stopAutoRefresh()
      }
    },
    
    trendType() {
      this.updateTrendChart()
    }
  },
  created() {
    this.initPage()
    this.loadStatistics()
    this.setDefaultTimeRange()
    this.loadAlarms()
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
    ...mapActions('alarms', [
      'fetchAlarms',
      'acknowledgeAlarm',
      'acknowledgeAllAlarms',
      'batchAcknowledgeAlarms',
      'getAlarmStatistics',
      'cleanupOldAlarms'
    ]),
    
    async initPage() {
      try {
        // 加载设置
        const savedSettings = localStorage.getItem('alarmSettings')
        if (savedSettings) {
          this.settingsForm = { ...this.settingsForm, ...JSON.parse(savedSettings) }
        }
        
        // 应用过滤设置
        if (this.settingsForm.showUnacknowledged && !this.settingsForm.showAcknowledged) {
          this.queryForm.isAcknowledged = false
        }
        
        // 设置默认时间范围
        this.setDefaultTimeRange()
      } catch (error) {
        console.error('初始化页面失败:', error)
      }
    },
    
    async loadStatistics() {
      try {
        const response = await this.getAlarmStatistics({
          startDate: subDays(new Date(), 30),
          endDate: new Date()
        })
        
        if (response.success) {
          this.updateStatistics(response.data)
        }
      } catch (error) {
        console.error('加载报警统计失败:', error)
      }
    },
    
    updateStatistics(data) {
      this.stats = {
        todayAlarms: data.byDate[format(new Date(), 'yyyy-MM-dd')] || 0,
        todayTrend: this.calculateTrend(data.byDate),
        unacknowledgedAlarms: data.unacknowledgedAlarms,
        acknowledgeRate: data.totalAlarms > 0 
          ? Math.round((data.acknowledgedAlarms / data.totalAlarms) * 100) 
          : 0,
        severeAlarms: Object.values(data.byFactor)
          .reduce((sum, count) => sum + count, 0),
        severeRate: data.totalAlarms > 0
          ? Math.round((this.stats.severeAlarms / data.totalAlarms) * 100)
          : 0,
        avgDuration: 0, // 需要从API获取
        longestDuration: '--' // 需要从API获取
      }
    },
    
    calculateTrend(byDate) {
      const today = format(new Date(), 'yyyy-MM-dd')
      const yesterday = format(subDays(new Date(), 1), 'yyyy-MM-dd')
      
      const todayCount = byDate[today] || 0
      const yesterdayCount = byDate[yesterday] || 0
      
      if (yesterdayCount === 0) return 0
      return Math.round(((todayCount - yesterdayCount) / yesterdayCount) * 100)
    },
    
    setDefaultTimeRange() {
      const now = new Date()
      let start
      
      switch (this.settingsForm.defaultTimeRange) {
        case 'today':
          start = startOfDay(now)
          break
        case '24h':
          start = subHours(now, 24)
          break
        case 'week':
          start = subDays(now, 7)
          break
        case 'month':
          start = subMonths(now, 1)
          break
      }
      
      this.queryForm.startTime = format(start, 'yyyy-MM-dd HH:mm:ss')
      this.queryForm.endTime = format(now, 'yyyy-MM-dd HH:mm:ss')
    },
    
    async loadAlarms() {
      this.loading = true
      
      try {
        const params = {
          page: this.currentPage,
          pageSize: this.pageSize,
          sortBy: this.sortField,
          sortOrder: this.sortOrder
        }
        
        // 添加查询条件
        Object.keys(this.queryForm).forEach(key => {
          if (this.queryForm[key] !== null && this.queryForm[key] !== '') {
            params[key] = this.queryForm[key]
          }
        })
        
        const result = await this.fetchAlarms(params)
        
        if (result) {
          this.tableData = result.data
          this.total = result.totalCount
          
          // 更新趋势数据
          this.prepareTrendData(result.data)
          
          // 更新空状态信息
          this.updateEmptyMessage()
        }
      } catch (error) {
        console.error('加载报警数据失败:', error)
        this.$message.error('加载报警数据失败')
      } finally {
        this.loading = false
      }
    },
    
    prepareTrendData(data) {
      if (!data || data.length === 0) {
        this.trendData = []
        return
      }
      
      // 按时间分组
      const groups = {}
      data.forEach(alarm => {
        const date = new Date(alarm.timestamp)
        let key
        
        switch (this.trendType) {
          case 'day':
            key = format(date, 'yyyy-MM-dd')
            break
          case 'hour':
            key = format(date, 'yyyy-MM-dd HH:00')
            break
          case 'factor':
            key = alarm.factorType
            break
        }
        
        if (!groups[key]) {
          groups[key] = {
            key,
            count: 0,
            severe: 0,
            warning: 0
          }
        }
        
        groups[key].count++
        
        // 按级别分类
        const severity = this.getAlarmSeverity(alarm)
        if (severity === 'severe') {
          groups[key].severe++
        } else if (severity === 'warning') {
          groups[key].warning++
        }
      })
      
      this.trendData = Object.values(groups).sort((a, b) => {
        if (this.trendType === 'factor') {
          return b.count - a.count
        }
        return a.key.localeCompare(b.key)
      })
      
      this.updateTrendChart()
    },
    
    updateTrendChart() {
      if (this.trendData.length === 0) return
      
      const isTimeChart = this.trendType === 'day' || this.trendType === 'hour'
      const isFactorChart = this.trendType === 'factor'
      
      const xAxisData = this.trendData.map(item => item.key)
      const countData = this.trendData.map(item => item.count)
      const severeData = this.trendData.map(item => item.severe)
      const warningData = this.trendData.map(item => item.warning)
      
      this.trendChartOption = {
        title: {
          text: '报警趋势分析',
          left: 'center',
          textStyle: {
            fontSize: 14,
            fontWeight: 'normal'
          }
        },
        tooltip: {
          trigger: 'axis',
          formatter: params => {
            let result = `${params[0].axisValueLabel}<br/>`
            params.forEach(param => {
              const value = param.value || 0
              const seriesName = param.seriesName
              result += `${seriesName}: ${value}次<br/>`
            })
            return result
          }
        },
        legend: {
          data: ['总报警数', '严重报警', '警告报警'],
          top: 30
        },
        grid: {
          left: '3%',
          right: '4%',
          bottom: '3%',
          top: '20%',
          containLabel: true
        },
        xAxis: {
          type: isTimeChart ? 'category' : 'category',
          data: xAxisData,
          axisLabel: {
            rotate: isFactorChart ? 45 : 0,
            interval: 0
          }
        },
        yAxis: {
          type: 'value',
          name: '次数'
        },
        series: [
          {
            name: '总报警数',
            type: 'bar',
            barWidth: '60%',
            data: countData,
            itemStyle: {
              color: '#36a2eb'
            }
          },
          {
            name: '严重报警',
            type: 'line',
            data: severeData,
            smooth: true,
            lineStyle: {
              width: 2,
              color: '#f56c6c'
            },
            itemStyle: {
              color: '#f56c6c'
            }
          },
          {
            name: '警告报警',
            type: 'line',
            data: warningData,
            smooth: true,
            lineStyle: {
              width: 2,
              color: '#e6a23c'
            },
            itemStyle: {
              color: '#e6a23c'
            }
          }
        ]
      }
    },
    
    async handleQuery() {
      this.queryLoading = true
      this.currentPage = 1
      
      try {
        await this.loadAlarms()
      } catch (error) {
        console.error('查询报警失败:', error)
      } finally {
        this.queryLoading = false
      }
    },
    
    handleReset() {
      this.$refs.queryForm.resetFields()
      this.setDefaultTimeRange()
      this.selectedAlarms = []
      this.currentPage = 1
      this.loadAlarms()
    },
    
    quickSelectTime(type) {
      const now = new Date()
      let start
      
      switch (type) {
        case 'today':
          start = startOfDay(now)
          break
        case 'yesterday':
          start = startOfDay(subDays(now, 1))
          this.queryForm.endTime = format(endOfDay(subDays(now, 1)), 'yyyy-MM-dd HH:mm:ss')
          break
        case 'lastWeek':
          start = subDays(now, 7)
          break
        case 'lastMonth':
          start = subMonths(now, 1)
          break
      }
      
      this.queryForm.startTime = format(start, 'yyyy-MM-dd HH:mm:ss')
      if (type !== 'yesterday') {
        this.queryForm.endTime = format(now, 'yyyy-MM-dd HH:mm:ss')
      }
      
      // 自动查询
      this.handleQuery()
    },
    
    handlePageChange(page) {
      this.currentPage = page
      this.loadAlarms()
    },
    
    handlePageSizeChange(size) {
      this.pageSize = size
      this.currentPage = 1
      this.loadAlarms()
    },
    
    handleSortChange({ prop, order }) {
      this.sortField = prop
      this.sortOrder = order === 'ascending' ? 'asc' : 'desc'
      this.loadAlarms()
    },
    
    handleSelectionChange(selection) {
      this.selectedAlarms = selection
    },
    
    indexMethod(index) {
      return (this.currentPage - 1) * this.pageSize + index + 1
    },
    
    async handleBatchAcknowledge() {
      if (this.selectedAlarms.length === 0) {
        this.$message.warning('请选择要确认的报警')
        return
      }
      
      const alarmIds = this.selectedAlarms.map(alarm => alarm.id)
      
      try {
        await this.batchAcknowledgeAlarms(alarmIds)
        this.$message.success(`已确认 ${alarmIds.length} 条报警`)
        this.selectedAlarms = []
        await this.loadAlarms()
        await this.loadStatistics()
      } catch (error) {
        console.error('批量确认报警失败:', error)
        this.$message.error('批量确认失败')
      }
    },
    
    async acknowledgeAlarm(alarm) {
      try {
        await this.acknowledgeAlarm(alarm.id)
        this.$message.success('报警已确认')
        await this.loadAlarms()
        await this.loadStatistics()
      } catch (error) {
        console.error('确认报警失败:', error)
        this.$message.error('确认失败')
      }
    },
    
    async acknowledgeCurrentAlarm() {
      if (this.currentAlarm) {
        await this.acknowledgeAlarm(this.currentAlarm)
        this.detailDialogVisible = false
      }
    },
    
    async deleteAlarm(alarm) {
      this.$confirm('确定要删除这条报警记录吗？', '删除确认', {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(async () => {
        try {
          // 这里调用API删除报警
          // await this.$api.deleteAlarm(alarm.id)
          this.$message.success('报警记录已删除')
          await this.loadAlarms()
          await this.loadStatistics()
        } catch (error) {
          console.error('删除报警失败:', error)
          this.$message.error('删除失败')
        }
      }).catch(() => {})
    },
    
    async handleCleanup() {
      this.$confirm('确定要清理过期的报警记录吗？（默认清理180天前的已确认报警）', '清理确认', {
        confirmButtonText: '确定清理',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(async () => {
        try {
          await this.cleanupOldAlarms()
          this.$message.success('过期报警清理完成')
          await this.loadAlarms()
          await this.loadStatistics()
        } catch (error) {
          console.error('清理报警失败:', error)
          this.$message.error('清理失败')
        }
      }).catch(() => {})
    },
    
    async viewAlarmDetails(alarm) {
      this.currentAlarm = alarm
      this.detailDialogVisible = true
      
      // 加载同类报警历史
      try {
        const response = await this.$api.getAlarms({
          deviceId: alarm.deviceId,
          factorType: alarm.factorType,
          pageSize: 10
        })
        
        if (response.success) {
          this.alarmHistory = response.data.data.filter(item => item.id !== alarm.id)
        }
      } catch (error) {
        console.error('加载报警历史失败:', error)
      }
    },
    
    viewDeviceDashboard() {
      if (this.currentAlarm && this.currentAlarm.deviceId) {
        this.$router.push({
          path: '/dashboard',
          query: { deviceId: this.currentAlarm.deviceId }
        })
        this.detailDialogVisible = false
      }
    },
    
    handleIgnore() {
      this.$message.info('忽略报警功能开发中...')
    },
    
    handleAdjustThreshold() {
      if (this.currentAlarm) {
        this.$router.push({
          path: '/thresholds',
          query: { 
            deviceId: this.currentAlarm.deviceId,
            factorType: this.currentAlarm.factorType 
          }
        })
        this.detailDialogVisible = false
      }
    },
    
    exportAlarms() {
      this.$message.info('导出功能开发中...')
    },
    
    showSettings() {
      this.settingsDialogVisible = true
    },
    
    saveSettings() {
      localStorage.setItem('alarmSettings', JSON.stringify(this.settingsForm))
      this.settingsDialogVisible = false
      this.$message.success('设置已保存')
      
      // 应用设置
      if (this.settingsForm.autoRefresh) {
        this.startAutoRefresh()
      } else {
        this.stopAutoRefresh()
      }
    },
    
    startAutoRefresh() {
      this.stopAutoRefresh()
      this.refreshTimer = setInterval(() => {
        if (document.visibilityState === 'visible') {
          this.loadAlarms()
          this.loadStatistics()
        }
      }, this.settingsForm.refreshInterval * 1000)
    },
    
    stopAutoRefresh() {
      if (this.refreshTimer) {
        clearInterval(this.refreshTimer)
        this.refreshTimer = null
      }
    },
    
    // 工具方法
    formatDateTime(timestamp) {
      if (!timestamp) return '--'
      return format(new Date(timestamp), 'yyyy-MM-dd HH:mm:ss')
    },
    
    formatTime(timestamp) {
      if (!timestamp) return '--'
      return format(new Date(timestamp), 'HH:mm:ss')
    },
    
    formatDuration(duration) {
      if (!duration) return '--'
      
      const seconds = Math.floor(duration / 1000)
      const minutes = Math.floor(seconds / 60)
      const hours = Math.floor(minutes / 60)
      const days = Math.floor(hours / 24)
      
      if (days > 0) {
        return `${days}天${hours % 24}小时`
      } else if (hours > 0) {
        return `${hours}小时${minutes % 60}分钟`
      } else if (minutes > 0) {
        return `${minutes}分钟${seconds % 60}秒`
      } else {
        return `${seconds}秒`
      }
    },
    
    getFactorTagType(factorType) {
      const typeMap = {
        'Temperature': 'danger',
        'Humidity': 'warning',
        'Current': 'success',
        'Voltage': 'info'
      }
      return typeMap[factorType] || 'info'
    },
    
    getFactorUnit(factorType) {
      const unitMap = {
        'Temperature': '°C',
        'Humidity': '%',
        'Current': 'A',
        'Voltage': 'V'
      }
      return unitMap[factorType] || ''
    },
    
    getAlarmSeverity(alarm) {
      if (alarm.limitType === 'Upper') return 'severe'
      return 'warning'
    },
    
    getValueClass(alarm) {
      const severity = this.getAlarmSeverity(alarm)
      return `value-${severity}`
    },
    
    getThresholdInfo(alarm) {
      // 这里应该从阈值配置中获取具体数值
      return alarm.limitType === 'Upper' ? '超过上限阈值' : '低于下限阈值'
    },
    
    getTrendClass(trend) {
      if (trend > 0) return 'trend-up'
      if (trend < 0) return 'trend-down'
      return 'trend-neutral'
    },
    
    getTrendIcon(trend) {
      if (trend > 0) return 'el-icon-top'
      if (trend < 0) return 'el-icon-bottom'
      return 'el-icon-right'
    },
    
    updateEmptyMessage() {
      if (this.total === 0) {
        this.emptyMessage = '暂无报警记录'
      } else {
        this.emptyMessage = ''
      }
    }
  }
}
</script>

<style lang="scss" scoped>
.alarms-container {
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
        color: #e6a23c;
      }
      
      .badge {
        ::v-deep .el-badge__content {
          transform: translateY(-50%) translateX(100%);
        }
      }
    }
  }
  
  .alarm-stats {
    .stat-card {
      border-radius: 8px;
      border: 1px solid var(--border-color);
      transition: all 0.3s;
      
      &:hover {
        transform: translateY(-2px);
        box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1) !important;
      }
      
      &.total {
        border-left: 4px solid #36a2eb;
      }
      
      &.unacknowledged {
        border-left: 4px solid #f56c6c;
      }
      
      &.severe {
        border-left: 4px solid #ff6384;
      }
      
      &.duration {
        border-left: 4px solid #4ecdc4;
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
          
          .stat-card.total & {
            background: linear-gradient(135deg, #36a2eb20, #36a2eb40);
            color: #36a2eb;
          }
          
          .stat-card.unacknowledged & {
            background: linear-gradient(135deg, #f56c6c20, #f56c6c40);
            color: #f56c6c;
          }
          
          .stat-card.severe & {
            background: linear-gradient(135deg, #ff638420, #ff638440);
            color: #ff6384;
          }
          
          .stat-card.duration & {
            background: linear-gradient(135deg, #4ecdc420, #4ecdc440);
            color: #4ecdc4;
          }
        }
        
        .stat-info {
          flex: 1;
          
          .stat-label {
            font-size: 14px;
            color: var(--text-color-secondary);
            margin-bottom: 5px;
          }
          
          .stat-value {
            font-size: 24px;
            font-weight: 700;
            color: var(--text-color-primary);
            margin-bottom: 5px;
          }
          
          .stat-trend {
            font-size: 12px;
            
            &.trend-up {
              color: #f56c6c;
            }
            
            &.trend-down {
              color: #67c23a;
            }
            
            &.trend-neutral {
              color: var(--text-color-secondary);
            }
          }
        }
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
    }
  }
  
  .trend-card {
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
      
      .time-cell {
        .time-main {
          font-weight: 600;
          color: var(--text-color-primary);
        }
        
        .time-duration {
          font-size: 12px;
          color: var(--text-color-secondary);
          margin-top: 2px;
        }
      }
      
      .device-cell {
        .device-name {
          font-weight: 600;
          color: var(--text-color-primary);
        }
        
        .device-code {
          font-size: 12px;
          color: var(--text-color-secondary);
          margin-top: 2px;
        }
      }
      
      .factor-cell {
        .el-tag {
          min-width: 60px;
        }
      }
      
      .value-cell {
        padding: 4px 8px;
        border-radius: 4px;
        font-weight: 600;
        
        &.value-severe {
          background: #fef0f0;
          color: #f56c6c;
        }
        
        &.value-warning {
          background: #fdf6ec;
          color: #e6a23c;
        }
      }
      
      .message-cell {
        max-width: 300px;
        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
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
  
  .alarm-details {
    .detail-value {
      display: flex;
      align-items: center;
      gap: 4px;
      
      .unit {
        color: var(--text-color-secondary);
        font-size: 12px;
      }
    }
    
    .threshold-info {
      color: var(--text-color-primary);
      font-weight: 500;
    }
    
    .alarm-history {
      margin-top: 20px;
      
      h4 {
        margin: 20px 0 10px 0;
        font-size: 16px;
        font-weight: 600;
        color: var(--text-color-primary);
      }
    }
    
    .alarm-actions {
      margin-top: 20px;
      
      .action-buttons {
        margin-top: 15px;
        display: flex;
        gap: 10px;
      }
    }
  }
  
  .form-tip {
    margin-left: 10px;
    font-size: 12px;
    color: var(--text-color-secondary);
  }
}

@media (max-width: 768px) {
  .alarms-container {
    .page-header {
      flex-direction: column;
      align-items: flex-start;
      gap: 10px;
    }
    
    .alarm-stats {
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