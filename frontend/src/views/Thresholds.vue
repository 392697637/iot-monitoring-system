<template>
  <div class="devices-container">
    <!-- 页面标题 -->
    <div class="page-header">
      <h1 class="page-title">
        <i class="el-icon-cpu"></i>
        设备管理
      </h1>
      <div class="page-actions">
        <el-button
          type="primary"
          icon="el-icon-plus"
          @click="handleCreate"
        >
          新增设备
        </el-button>
        <el-button
          type="success"
          icon="el-icon-refresh"
          @click="refreshDevices"
          :loading="refreshing"
        >
          刷新列表
        </el-button>
        <el-button
          type="info"
          icon="el-icon-setting"
          @click="showSettings"
        >
          设备设置
        </el-button>
      </div>
    </div>

    <!-- 设备统计 -->
    <div class="device-stats">
      <el-row :gutter="20">
        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card total">
            <div class="stat-content">
              <div class="stat-icon">
                <i class="el-icon-cpu"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">设备总数</div>
                <div class="stat-value">{{ stats.totalDevices }}</div>
                <div class="stat-trend" :class="getTrendClass(stats.deviceTrend)">
                  <i :class="getTrendIcon(stats.deviceTrend)"></i>
                  {{ stats.deviceTrend > 0 ? '+' : '' }}{{ Math.abs(stats.deviceTrend) }}台
                </div>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card active">
            <div class="stat-content">
              <div class="stat-icon">
                <i class="el-icon-success"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">在线设备</div>
                <div class="stat-value">{{ stats.activeDevices }}</div>
                <div class="stat-trend">
                  {{ stats.activeRate }}% 在线率
                </div>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card warning">
            <div class="stat-content">
              <div class="stat-icon">
                <i class="el-icon-warning"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">告警设备</div>
                <div class="stat-value">{{ stats.warningDevices }}</div>
                <div class="stat-trend">
                  {{ stats.warningRate }}% 告警率
                </div>
              </div>
            </div>
          </el-card>
        </el-col>

        <el-col :xs="24" :sm="6">
          <el-card shadow="hover" class="stat-card data">
            <div class="stat-content">
              <div class="stat-icon">
                <i class="el-icon-data-line"></i>
              </div>
              <div class="stat-info">
                <div class="stat-label">今日数据</div>
                <div class="stat-value">{{ stats.todayData }}</div>
                <div class="stat-trend">
                  {{ formatBytes(stats.dataSize) }}
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
        <span>设备查询</span>
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
            <el-form-item label="设备名称" prop="deviceName">
              <el-input
                v-model="queryForm.deviceName"
                placeholder="输入设备名称"
                clearable
                style="width: 100%;"
              />
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="设备编码" prop="deviceCode">
              <el-input
                v-model="queryForm.deviceCode"
                placeholder="输入设备编码"
                clearable
                style="width: 100%;"
              />
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="设备状态" prop="isActive">
              <el-select
                v-model="queryForm.isActive"
                placeholder="选择状态"
                clearable
                style="width: 100%;"
              >
                <el-option label="启用" :value="true" />
                <el-option label="停用" :value="false" />
              </el-select>
            </el-form-item>
          </el-col>

          <el-col :xs="24" :sm="12" :md="8" :lg="6">
            <el-form-item label="安装位置" prop="location">
              <el-input
                v-model="queryForm.location"
                placeholder="输入安装位置"
                clearable
                style="width: 100%;"
              />
            </el-form-item>
          </el-col>
        </el-row>

        <!-- 高级查询条件 -->
        <el-collapse-transition>
          <div v-show="showAdvancedQuery">
            <el-divider></el-divider>
            <el-row :gutter="20">
              <el-col :xs="24" :sm="12" :md="8" :lg="6">
                <el-form-item label="制造商" prop="manufacturer">
                  <el-input
                    v-model="queryForm.manufacturer"
                    placeholder="输入制造商"
                    clearable
                    style="width: 100%;"
                  />
                </el-form-item>
              </el-col>

              <el-col :xs="24" :sm="12" :md="8" :lg="6">
                <el-form-item label="设备型号" prop="model">
                  <el-input
                    v-model="queryForm.model"
                    placeholder="输入设备型号"
                    clearable
                    style="width: 100%;"
                  />
                </el-form-item>
              </el-col>

              <el-col :xs="24" :sm="12" :md="8" :lg="6">
                <el-form-item label="安装日期" prop="installationDate">
                  <el-date-picker
                    v-model="queryForm.installationDate"
                    type="daterange"
                    range-separator="至"
                    start-placeholder="开始日期"
                    end-placeholder="结束日期"
                    value-format="yyyy-MM-dd"
                    style="width: 100%;"
                  />
                </el-form-item>
              </el-col>

              <el-col :xs="24" :sm="12" :md="8" :lg="6">
                <el-form-item label="数据状态" prop="dataStatus">
                  <el-select
                    v-model="queryForm.dataStatus"
                    placeholder="选择数据状态"
                    clearable
                    style="width: 100%;"
                  >
                    <el-option label="有数据" value="hasData" />
                    <el-option label="无数据" value="noData" />
                    <el-option label="数据异常" value="abnormal" />
                  </el-select>
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
            @click="exportDevices"
          >
            导出设备
          </el-button>
          <el-button
            type="text"
            @click="importDevices"
          >
            导入设备
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>

    <!-- 设备列表 -->
    <el-card shadow="never" class="devices-card">
      <div slot="header" class="table-header">
        <span>设备列表</span>
        <div class="table-actions">
          <el-select
            v-model="pageSize"
            size="small"
            style="width: 100px; margin-right: 10px;"
            @change="handlePageSizeChange"
          >
            <el-option label="10条/页" :value="10" />
            <el-option label="20条/页" :value="20" />
            <el-option label="50条/页" :value="50" />
            <el-option label="100条/页" :value="100" />
          </el-select>
          <el-button
            size="small"
            icon="el-icon-printer"
            @click="printDevices"
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
          @selection-change="handleSelectionChange"
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
            prop="deviceCode"
            label="设备编码"
            width="120"
            align="center"
            sortable="custom"
          >
            <template slot-scope="{ row }">
              <div class="device-code-cell">
                <el-tag
                  size="small"
                  :type="row.isActive ? 'success' : 'info'"
                  effect="plain"
                >
                  {{ row.deviceCode }}
                </el-tag>
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="deviceName"
            label="设备名称"
            width="150"
            align="center"
            sortable="custom"
          >
            <template slot-scope="{ row }">
              <div class="device-name-cell">
                <div class="name-main">{{ row.deviceName }}</div>
                <div class="name-model" v-if="row.model">
                  {{ row.model }}
                </div>
              </div>
            </template>
          </el-table-column>

          <el-table-column
            prop="location"
            label="安装位置"
            width="150"
            align="center"
            show-overflow-tooltip
          />

          <el-table-column
            prop="manufacturer"
            label="制造商"
            width="120"
            align="center"
          />

          <el-table-column
            prop="installationDate"
            label="安装日期"
            width="120"
            align="center"
            sortable="custom"
          >
            <template slot-scope="{ row }">
              {{ row.installationDate || '--' }}
            </template>
          </el-table-column>

          <el-table-column
            label="设备状态"
            width="120"
            align="center"
          >
            <template slot-scope="{ row }">
              <div class="device-status">
                <el-tag
                  :type="getDeviceStatusType(row)"
                  size="small"
                  effect="plain"
                >
                  {{ getDeviceStatusText(row) }}
                </el-tag>
                <div class="status-detail" v-if="row.lastDataTime">
                  {{ formatTimeAgo(row.lastDataTime) }}
                </div>
              </div>
            </template>
          </el-table-column>

          <el-table-column
            label="监测数据"
            width="180"
            align="center"
          >
            <template slot-scope="{ row }">
              <div class="device-data">
                <div class="data-item">
                  <span class="label">温度:</span>
                  <span class="value" :class="getDataValueClass(row.lastTemperature, 'temperature')">
                    {{ row.lastTemperature !== null ? row.lastTemperature.toFixed(2) : '--' }}°C
                  </span>
                </div>
                <div class="data-item">
                  <span class="label">湿度:</span>
                  <span class="value" :class="getDataValueClass(row.lastHumidity, 'humidity')">
                    {{ row.lastHumidity !== null ? row.lastHumidity.toFixed(2) : '--' }}%
                  </span>
                </div>
              </div>
            </template>
          </el-table-column>

          <el-table-column
            label="数据统计"
            width="150"
            align="center"
          >
            <template slot-scope="{ row }">
              <div class="data-stats">
                <div class="stat-item">
                  <span class="label">今日:</span>
                  <span class="value">{{ row.todayDataCount || 0 }}</span>
                </div>
                <div class="stat-item">
                  <span class="label">报警:</span>
                  <span class="value">{{ row.activeAlarmCount || 0 }}</span>
                </div>
              </div>
            </template>
          </el-table-column>

          <el-table-column
            label="操作"
            width="200"
            align="center"
            fixed="right"
          >
            <template slot-scope="{ row }">
              <el-button
                type="primary"
                size="mini"
                plain
                @click="viewDeviceDetails(row)"
              >
                详情
              </el-button>
              <el-button
                type="warning"
                size="mini"
                plain
                @click="editDevice(row)"
              >
                编辑
              </el-button>
              <el-button
                size="mini"
                plain
                :type="row.isActive ? 'danger' : 'success'"
                @click="toggleDeviceStatus(row)"
              >
                {{ row.isActive ? '停用' : '启用' }}
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
            <i class="el-icon-cpu" style="font-size: 48px; color: #909399;"></i>
          </template>
          <template v-slot:action>
            <el-button type="primary" @click="handleCreate">新增设备</el-button>
          </template>
        </empty-state>
      </div>

      <!-- 分页 -->
      <div class="pagination-container" v-if="total > 0">
        <el-pagination
          :current-page="currentPage"
          :page-sizes="[10, 20, 50, 100]"
          :page-size="pageSize"
          :total="total"
          layout="total, sizes, prev, pager, next, jumper"
          @size-change="handlePageSizeChange"
          @current-change="handlePageChange"
        />
      </div>
    </el-card>

    <!-- 批量操作 -->
    <div class="batch-actions" v-if="selectedDevices.length > 0">
      <el-card shadow="never">
        <div class="batch-content">
          <span class="batch-count">已选择 {{ selectedDevices.length }} 台设备</span>
          <div class="batch-buttons">
            <el-button
              size="small"
              @click="batchEnableDevices(true)"
              :disabled="selectedDevices.every(d => d.isActive)"
            >
              批量启用
            </el-button>
            <el-button
              size="small"
              @click="batchEnableDevices(false)"
              :disabled="selectedDevices.every(d => !d.isActive)"
            >
              批量停用
            </el-button>
            <el-button
              size="small"
              type="danger"
              @click="batchDeleteDevices"
            >
              批量删除
            </el-button>
            <el-button
              size="small"
              type="primary"
              @click="batchMonitor"
            >
              批量监控
            </el-button>
          </div>
        </div>
      </el-card>
    </div>

    <!-- 设备详情对话框 -->
    <el-dialog
      :title="currentDevice ? currentDevice.deviceName + ' - 设备详情' : '设备详情'"
      :visible.sync="detailDialogVisible"
      width="800px"
      :close-on-click-modal="false"
    >
      <div v-if="currentDevice" class="device-details">
        <el-tabs v-model="detailActiveTab">
          <el-tab-pane label="基本信息" name="basic">
            <el-descriptions :column="2" border>
              <el-descriptions-item label="设备编码">
                {{ currentDevice.deviceCode }}
              </el-descriptions-item>
              <el-descriptions-item label="设备名称">
                {{ currentDevice.deviceName }}
              </el-descriptions-item>
              <el-descriptions-item label="安装位置">
                {{ currentDevice.location || '--' }}
              </el-descriptions-item>
              <el-descriptions-item label="设备状态">
                <el-tag :type="currentDevice.isActive ? 'success' : 'info'">
                  {{ currentDevice.isActive ? '启用' : '停用' }}
                </el-tag>
              </el-descriptions-item>
              <el-descriptions-item label="制造商">
                {{ currentDevice.manufacturer || '--' }}
              </el-descriptions-item>
              <el-descriptions-item label="设备型号">
                {{ currentDevice.model || '--' }}
              </el-descriptions-item>
              <el-descriptions-item label="序列号">
                {{ currentDevice.serialNumber || '--' }}
              </el-descriptions-item>
              <el-descriptions-item label="安装日期">
                {{ currentDevice.installationDate || '--' }}
              </el-descriptions-item>
              <el-descriptions-item label="创建时间">
                {{ formatDateTime(currentDevice.createdAt) }}
              </el-descriptions-item>
              <el-descriptions-item label="最后更新">
                {{ formatDateTime(currentDevice.updatedAt) }}
              </el-descriptions-item>
            </el-descriptions>
          </el-tab-pane>
          
          <el-tab-pane label="实时数据" name="realtime">
            <div class="realtime-data">
              <div v-if="deviceRealtimeData" class="data-cards">
                <el-row :gutter="20">
                  <el-col :span="6">
                    <el-card shadow="hover">
                      <div class="data-card">
                        <div class="data-icon temperature">
                          <i class="el-icon-thermometer"></i>
                        </div>
                        <div class="data-info">
                          <div class="data-label">温度</div>
                          <div class="data-value">
                            {{ deviceRealtimeData.temperature || '--' }}°C
                          </div>
                          <div class="data-time">
                            {{ deviceRealtimeData.timestamp ? formatTime(deviceRealtimeData.timestamp) : '--' }}
                          </div>
                        </div>
                      </div>
                    </el-card>
                  </el-col>
                  
                  <el-col :span="6">
                    <el-card shadow="hover">
                      <div class="data-card">
                        <div class="data-icon humidity">
                          <i class="el-icon-watermelon"></i>
                        </div>
                        <div class="data-info">
                          <div class="data-label">湿度</div>
                          <div class="data-value">
                            {{ deviceRealtimeData.humidity || '--' }}%
                          </div>
                          <div class="data-time">
                            {{ deviceRealtimeData.timestamp ? formatTime(deviceRealtimeData.timestamp) : '--' }}
                          </div>
                        </div>
                      </div>
                    </el-card>
                  </el-col>
                  
                  <el-col :span="6">
                    <el-card shadow="hover">
                      <div class="data-card">
                        <div class="data-icon current">
                          <i class="el-icon-lightning"></i>
                        </div>
                        <div class="data-info">
                          <div class="data-label">电流</div>
                          <div class="data-value">
                            {{ deviceRealtimeData.current || '--' }}A
                          </div>
                          <div class="data-time">
                            {{ deviceRealtimeData.timestamp ? formatTime(deviceRealtimeData.timestamp) : '--' }}
                          </div>
                        </div>
                      </div>
                    </el-card>
                  </el-col>
                  
                  <el-col :span="6">
                    <el-card shadow="hover">
                      <div class="data-card">
                        <div class="data-icon voltage">
                          <i class="el-icon-flash"></i>
                        </div>
                        <div class="data-info">
                          <div class="data-label">电压</div>
                          <div class="data-value">
                            {{ deviceRealtimeData.voltage || '--' }}V
                          </div>
                          <div class="data-time">
                            {{ deviceRealtimeData.timestamp ? formatTime(deviceRealtimeData.timestamp) : '--' }}
                          </div>
                        </div>
                      </div>
                    </el-card>
                  </el-col>
                </el-row>
              </div>
              <empty-state v-else message="暂无实时数据" />
            </div>
          </el-tab-pane>
          
          <el-tab-pane label="数据统计" name="statistics">
            <div class="device-statistics">
              <el-row :gutter="20">
                <el-col :span="12">
                  <el-card shadow="hover">
                    <div slot="header">
                      <span>今日数据统计</span>
                    </div>
                    <div class="stat-content">
                      <el-descriptions :column="1" size="small">
                        <el-descriptions-item label="数据总数">
                          {{ currentDevice.todayDataCount || 0 }} 条
                        </el-descriptions-item>
                        <el-descriptions-item label="正常数据">
                          {{ deviceStats?.normalCount || 0 }} 条
                        </el-descriptions-item>
                        <el-descriptions-item label="告警数据">
                          {{ deviceStats?.warningCount || 0 }} 条
                        </el-descriptions-item>
                        <el-descriptions-item label="故障数据">
                          {{ deviceStats?.faultCount || 0 }} 条
                        </el-descriptions-item>
                        <el-descriptions-item label="数据频率">
                          {{ deviceStats?.dataFrequency || '--' }}
                        </el-descriptions-item>
                      </el-descriptions>
                    </div>
                  </el-card>
                </el-col>
                
                <el-col :span="12">
                  <el-card shadow="hover">
                    <div slot="header">
                      <span>报警统计</span>
                    </div>
                    <div class="stat-content">
                      <el-descriptions :column="1" size="small">
                        <el-descriptions-item label="未确认报警">
                          {{ currentDevice.activeAlarmCount || 0 }} 个
                        </el-descriptions-item>
                        <el-descriptions-item label="今日报警">
                          {{ deviceStats?.todayAlarms || 0 }} 个
                        </el-descriptions-item>
                        <el-descriptions-item label="累计报警">
                          {{ deviceStats?.totalAlarms || 0 }} 个
                        </el-descriptions-item>
                        <el-descriptions-item label="报警响应率">
                          {{ deviceStats?.alarmResponseRate || '0' }}%
                        </el-descriptions-item>
                        <el-descriptions-item label="平均响应时间">
                          {{ deviceStats?.avgResponseTime || '--' }}
                        </el-descriptions-item>
                      </el-descriptions>
                    </div>
                  </el-card>
                </el-col>
              </el-row>
              
              <div class="data-chart" v-if="deviceHistoryData.length > 0">
                <h4>最近24小时数据趋势</h4>
                <v-chart
                  :option="deviceChartOption"
                  :autoresize="true"
                  style="height: 300px;"
                />
              </div>
            </div>
          </el-tab-pane>
          
          <el-tab-pane label="阈值配置" name="thresholds">
            <div class="device-thresholds">
              <div class="thresholds-header">
                <span>设备阈值配置</span>
                <el-button
                  size="small"
                  type="primary"
                  @click="goToThresholds"
                >
                  阈值管理
                </el-button>
              </div>
              <el-table
                :data="deviceThresholds"
                size="small"
                stripe
              >
                <el-table-column prop="factorName" label="监测因子" width="100" />
                <el-table-column prop="lowerLimit" label="下限" width="100" />
                <el-table-column prop="upperLimit" label="上限" width="100" />
                <el-table-column prop="isRealTimeAlert" label="报警" width="80">
                  <template slot-scope="{ row }">
                    <el-tag
                      size="small"
                      :type="row.isRealTimeAlert ? 'success' : 'info'"
                    >
                      {{ row.isRealTimeAlert ? '启用' : '关闭' }}
                    </el-tag>
                  </template>
                </el-table-column>
                <el-table-column prop="alertMessage" label="报警信息" show-overflow-tooltip />
              </el-table>
              <empty-state v-if="deviceThresholds.length === 0" message="暂无阈值配置" />
            </div>
          </el-tab-pane>
        </el-tabs>
      </div>
      <span slot="footer" class="dialog-footer">
        <el-button @click="detailDialogVisible = false">关闭</el-button>
        <el-button
          type="primary"
          @click="goToDeviceDashboard"
        >
          查看监控
        </el-button>
      </span>
    </el-dialog>

    <!-- 新增/编辑设备对话框 -->
    <el-dialog
      :title="isEditing ? '编辑设备' : '新增设备'"
      :visible.sync="editDialogVisible"
      width="600px"
      :close-on-click-modal="false"
    >
      <el-form
        :model="editForm"
        :rules="deviceRules"
        ref="editForm"
        label-width="100px"
      >
        <el-form-item label="设备编码" prop="deviceCode">
          <el-input
            v-model="editForm.deviceCode"
            placeholder="请输入设备编码"
            :disabled="isEditing"
          />
          <div class="form-tip">设备编码必须唯一</div>
        </el-form-item>

        <el-form-item label="设备名称" prop="deviceName">
          <el-input
            v-model="editForm.deviceName"
            placeholder="请输入设备名称"
          />
        </el-form-item>

        <el-form-item label="安装位置" prop="location">
          <el-input
            v-model="editForm.location"
            placeholder="请输入安装位置"
          />
        </el-form-item>

        <el-form-item label="制造商" prop="manufacturer">
          <el-input
            v-model="editForm.manufacturer"
            placeholder="请输入制造商"
          />
        </el-form-item>

        <el-form-item label="设备型号" prop="model">
          <el-input
            v-model="editForm.model"
            placeholder="请输入设备型号"
          />
        </el-form-item>

        <el-form-item label="序列号" prop="serialNumber">
          <el-input
            v-model="editForm.serialNumber"
            placeholder="请输入序列号"
          />
        </el-form-item>

        <el-form-item label="安装日期" prop="installationDate">
          <el-date-picker
            v-model="editForm.installationDate"
            type="date"
            placeholder="选择安装日期"
            value-format="yyyy-MM-dd"
            style="width: 100%;"
          />
        </el-form-item>

        <el-form-item label="设备状态" prop="isActive">
          <el-switch
            v-model="editForm.isActive"
            active-text="启用"
            inactive-text="停用"
          />
        </el-form-item>

        <el-form-item label="设备描述" prop="description">
          <el-input
            v-model="editForm.description"
            type="textarea"
            :rows="3"
            placeholder="请输入设备描述"
            maxlength="500"
            show-word-limit
          />
        </el-form-item>
      </el-form>
      <span slot="footer" class="dialog-footer">
        <el-button @click="editDialogVisible = false">取消</el-button>
        <el-button
          type="primary"
          @click="submitEditForm"
          :loading="submitting"
        >
          {{ isEditing ? '保存修改' : '创建设备' }}
        </el-button>
      </span>
    </el-dialog>

    <!-- 设备设置对话框 -->
    <el-dialog
      title="设备设置"
      :visible.sync="settingsDialogVisible"
      width="500px"
    >
      <el-form :model="settingsForm" label-width="120px">
        <el-form-item label="默认设备状态">
          <el-switch
            v-model="settingsForm.defaultDeviceStatus"
            active-text="启用"
            inactive-text="停用"
          />
        </el-form-item>
        <el-form-item label="自动刷新数据">
          <el-switch v-model="settingsForm.autoRefreshData" />
          <span class="form-tip">自动刷新设备列表和数据</span>
        </el-form-item>
        <el-form-item label="刷新间隔" v-if="settingsForm.autoRefreshData">
          <el-select v-model="settingsForm.refreshInterval">
            <el-option label="30秒" :value="30" />
            <el-option label="1分钟" :value="60" />
            <el-option label="5分钟" :value="300" />
          </el-select>
        </el-form-item>
        <el-form-item label="设备离线判断">
          <el-input-number
            v-model="settingsForm.offlineThreshold"
            :min="1"
            :max="60"
            :step="1"
            controls-position="right"
          >
            <template slot="append">分钟</template>
          </el-input-number>
          <span class="form-tip">超过时间无数据视为离线</span>
        </el-form-item>
        <el-form-item label="默认页面大小">
          <el-select v-model="settingsForm.defaultPageSize">
            <el-option label="10条" :value="10" />
            <el-option label="20条" :value="20" />
            <el-option label="50条" :value="50" />
            <el-option label="100条" :value="100" />
          </el-select>
        </el-form-item>
        <el-form-item label="导出格式">
          <el-select v-model="settingsForm.exportFormat">
            <el-option label="Excel" value="excel" />
            <el-option label="CSV" value="csv" />
            <el-option label="PDF" value="pdf" />
          </el-select>
        </el-form-item>
      </el-form>
      <span slot="footer" class="dialog-footer">
        <el-button @click="settingsDialogVisible = false">取消</el-button>
        <el-button type="primary" @click="saveSettings">保存设置</el-button>
      </span>
    </el-dialog>

    <!-- 导入设备对话框 -->
    <el-dialog
      title="导入设备"
      :visible.sync="importDialogVisible"
      width="500px"
    >
      <div class="import-content">
        <el-alert
          title="导入说明"
          type="info"
          :closable="false"
          description="请下载模板文件，按照模板格式填写设备信息后上传"
          show-icon
        />
        
        <div class="import-template">
          <h4>模板下载:</h4>
          <el-button
            type="primary"
            size="small"
            icon="el-icon-download"
            @click="downloadTemplate"
          >
            下载导入模板
          </el-button>
        </div>
        
        <div class="import-upload">
          <h4>上传文件:</h4>
          <el-upload
            class="upload-demo"
            drag
            action=""
            :before-upload="beforeUpload"
            :on-success="handleUploadSuccess"
            :on-error="handleUploadError"
            :show-file-list="false"
            accept=".xlsx,.xls,.csv"
          >
            <i class="el-icon-upload"></i>
            <div class="el-upload__text">将文件拖到此处，或<em>点击上传</em></div>
            <div class="el-upload__tip" slot="tip">支持 xlsx, xls, csv 格式，文件大小不超过10MB</div>
          </el-upload>
        </div>
        
        <div class="import-preview" v-if="importPreviewData.length > 0">
          <h4>导入预览:</h4>
          <el-table
            :data="importPreviewData"
            size="small"
            border
            max-height="200"
          >
            <el-table-column prop="deviceCode" label="设备编码" />
            <el-table-column prop="deviceName" label="设备名称" />
            <el-table-column prop="location" label="安装位置" />
            <el-table-column prop="manufacturer" label="制造商" />
          </el-table>
          <div class="import-actions">
            <el-button
              type="primary"
              size="small"
              @click="confirmImport"
              :loading="importing"
            >
              确认导入
            </el-button>
            <el-button
              size="small"
              @click="clearImport"
            >
              清空
            </el-button>
          </div>
        </div>
      </div>
    </el-dialog>
  </div>
</template>

<script>
import { mapState, mapActions } from 'vuex'
import { format, subHours, subDays } from 'date-fns'
import * as XLSX from 'xlsx'
import FileSaver from 'file-saver'
import EmptyState from '@/components/common/EmptyState.vue'

export default {
  name: 'Devices',
  components: {
    EmptyState
  },
  data() {
    return {
      // 查询表单
      queryForm: {
        deviceName: '',
        deviceCode: '',
        isActive: null,
        location: '',
        manufacturer: '',
        model: '',
        installationDate: null,
        dataStatus: ''
      },
      
      // 表格数据
      tableData: [],
      selectedDevices: [],
      loading: false,
      queryLoading: false,
      refreshing: false,
      
      // 分页
      currentPage: 1,
      pageSize: 20,
      total: 0,
      sortField: 'deviceName',
      sortOrder: 'asc',
      
      // 统计
      stats: {
        totalDevices: 0,
        deviceTrend: 0,
        activeDevices: 0,
        activeRate: 0,
        warningDevices: 0,
        warningRate: 0,
        todayData: 0,
        dataSize: 0
      },
      
      // 对话框
      showAdvancedQuery: false,
      detailDialogVisible: false,
      editDialogVisible: false,
      settingsDialogVisible: false,
      importDialogVisible: false,
      
      // 当前设备
      currentDevice: null,
      detailActiveTab: 'basic',
      deviceRealtimeData: null,
      deviceStats: null,
      deviceHistoryData: [],
      deviceThresholds: [],
      deviceChartOption: {},
      
      // 编辑设备
      isEditing: false,
      editForm: {
        deviceCode: '',
        deviceName: '',
        location: '',
        manufacturer: '',
        model: '',
        serialNumber: '',
        installationDate: '',
        isActive: true,
        description: ''
      },
      submitting: false,
      deviceRules: {
        deviceCode: [
          { required: true, message: '请输入设备编码', trigger: 'blur' },
          { min: 2, max: 50, message: '长度在 2 到 50 个字符', trigger: 'blur' }
        ],
        deviceName: [
          { required: true, message: '请输入设备名称', trigger: 'blur' },
          { min: 2, max: 100, message: '长度在 2 到 100 个字符', trigger: 'blur' }
        ],
        location: [
          { max: 200, message: '不能超过 200 个字符', trigger: 'blur' }
        ],
        manufacturer: [
          { max: 100, message: '不能超过 100 个字符', trigger: 'blur' }
        ],
        model: [
          { max: 50, message: '不能超过 50 个字符', trigger: 'blur' }
        ],
        serialNumber: [
          { max: 100, message: '不能超过 100 个字符', trigger: 'blur' }
        ]
      },
      
      // 设置
      settingsForm: {
        defaultDeviceStatus: true,
        autoRefreshData: false,
        refreshInterval: 60,
        offlineThreshold: 5,
        defaultPageSize: 20,
        exportFormat: 'excel'
      },
      
      // 导入
      importPreviewData: [],
      importing: false,
      
      // 刷新定时器
      refreshTimer: null,
      
      // 空状态信息
      emptyMessage: '暂无设备数据'
    }
  },
  computed: {
    ...mapState('devices', ['devices']),
    
    filteredDevices() {
      let filtered = [...this.devices]
      
      // 应用查询条件
      if (this.queryForm.deviceName) {
        filtered = filtered.filter(device =>
          device.deviceName.toLowerCase().includes(this.queryForm.deviceName.toLowerCase())
        )
      }
      
      if (this.queryForm.deviceCode) {
        filtered = filtered.filter(device =>
          device.deviceCode.toLowerCase().includes(this.queryForm.deviceCode.toLowerCase())
        )
      }
      
      if (this.queryForm.isActive !== null) {
        filtered = filtered.filter(device => device.isActive === this.queryForm.isActive)
      }
      
      if (this.queryForm.location) {
        filtered = filtered.filter(device =>
          device.location && device.location.toLowerCase().includes(this.queryForm.location.toLowerCase())
        )
      }
      
      if (this.queryForm.manufacturer) {
        filtered = filtered.filter(device =>
          device.manufacturer && device.manufacturer.toLowerCase().includes(this.queryForm.manufacturer.toLowerCase())
        )
      }
      
      if (this.queryForm.model) {
        filtered = filtered.filter(device =>
          device.model && device.model.toLowerCase().includes(this.queryForm.model.toLowerCase())
        )
      }
      
      if (this.queryForm.installationDate) {
        const [start, end] = this.queryForm.installationDate
        filtered = filtered.filter(device => {
          if (!device.installationDate) return false
          return device.installationDate >= start && device.installationDate <= end
        })
      }
      
      return filtered
    }
  },
  watch: {
    'settingsForm.autoRefreshData'(enabled) {
      if (enabled) {
        this.startAutoRefresh()
      } else {
        this.stopAutoRefresh()
      }
    }
  },
  created() {
    this.initPage()
    this.loadSettings()
  },
  mounted() {
    if (this.settingsForm.autoRefreshData) {
      this.startAutoRefresh()
    }
  },
  beforeDestroy() {
    this.stopAutoRefresh()
  },
  methods: {
    ...mapActions('devices', [
      'fetchDevices',
      'addDevice',
      'updateDevice',
      'deleteDevice'
    ]),
    
    async initPage() {
      try {
        await this.loadDevices()
        this.calculateStats()
      } catch (error) {
        console.error('初始化页面失败:', error)
      }
    },
    
    async loadDevices() {
      this.loading = true
      
      try {
        await this.fetchDevices()
        await this.loadDevicesWithStats()
        this.updateEmptyMessage()
      } catch (error) {
        console.error('加载设备列表失败:', error)
        this.$message.error('加载设备列表失败')
      } finally {
        this.loading = false
      }
    },
    
    async loadDevicesWithStats() {
      // 为每个设备加载统计数据
      this.tableData = await Promise.all(
        this.filteredDevices.map(async device => {
          const stats = await this.getDeviceStats(device.id)
          return {
            ...device,
            ...stats
          }
        })
      )
      
      this.total = this.tableData.length
      this.applyPagination()
    },
    
    async getDeviceStats(deviceId) {
      try {
        // 获取设备实时数据
        const realtimeResponse = await this.$api.getLatestData(deviceId)
        const realtimeData = realtimeResponse.success ? realtimeResponse.data : null
        
        // 获取设备统计
        const statsResponse = await this.$api.getDataStatistics(deviceId, new Date())
        const statsData = statsResponse.success ? statsResponse.data : null
        
        // 获取今日报警数量
        const alarmsResponse = await this.$api.getAlarms({
          deviceId: deviceId,
          isAcknowledged: false
        })
        const activeAlarms = alarmsResponse.success ? alarmsResponse.data.totalCount : 0
        
        return {
          lastTemperature: realtimeData?.temperature,
          lastHumidity: realtimeData?.humidity,
          lastCurrent: realtimeData?.current,
          lastVoltage: realtimeData?.voltage,
          lastDataTime: realtimeData?.timestamp,
          todayDataCount: statsData?.totalCount || 0,
          activeAlarmCount: activeAlarms,
          deviceStats: statsData
        }
      } catch (error) {
        console.error('获取设备统计失败:', error)
        return {}
      }
    },
    
    calculateStats() {
      const total = this.devices.length
      const active = this.devices.filter(d => d.isActive).length
      const warning = this.devices.filter(d => {
        // 这里可以根据实际情况判断设备是否告警
        return false
      }).length
      
      this.stats = {
        totalDevices: total,
        deviceTrend: 0, // 需要历史数据计算
        activeDevices: active,
        activeRate: total > 0 ? Math.round((active / total) * 100) : 0,
        warningDevices: warning,
        warningRate: total > 0 ? Math.round((warning / total) * 100) : 0,
        todayData: 0, // 需要从API获取
        dataSize: 0 // 需要从API获取
      }
    },
    
    applyPagination() {
      const start = (this.currentPage - 1) * this.pageSize
      const end = start + this.pageSize
      this.tableData = this.filteredDevices.slice(start, end)
    },
    
    async handleQuery() {
      this.queryLoading = true
      this.currentPage = 1
      
      try {
        await this.loadDevicesWithStats()
      } catch (error) {
        console.error('查询设备失败:', error)
      } finally {
        this.queryLoading = false
      }
    },
    
    handleReset() {
      this.$refs.queryForm.resetFields()
      this.currentPage = 1
      this.loadDevicesWithStats()
    },
    
    async refreshDevices() {
      this.refreshing = true
      try {
        await this.loadDevices()
        this.$message.success('设备列表已刷新')
      } catch (error) {
        console.error('刷新设备失败:', error)
      } finally {
        this.refreshing = false
      }
    },
    
    handlePageChange(page) {
      this.currentPage = page
      this.applyPagination()
    },
    
    handlePageSizeChange(size) {
      this.pageSize = size
      this.currentPage = 1
      this.applyPagination()
    },
    
    handleSortChange({ prop, order }) {
      this.sortField = prop
      this.sortOrder = order === 'ascending' ? 'asc' : 'desc'
      
      // 排序设备列表
      this.filteredDevices.sort((a, b) => {
        let aValue = a[prop]
        let bValue = b[prop]
        
        if (prop === 'installationDate') {
          aValue = aValue ? new Date(aValue).getTime() : 0
          bValue = bValue ? new Date(bValue).getTime() : 0
        }
        
        if (this.sortOrder === 'asc') {
          return aValue < bValue ? -1 : aValue > bValue ? 1 : 0
        } else {
          return aValue > bValue ? -1 : aValue < bValue ? 1 : 0
        }
      })
      
      this.applyPagination()
    },
    
    handleSelectionChange(selection) {
      this.selectedDevices = selection
    },
    
    indexMethod(index) {
      return (this.currentPage - 1) * this.pageSize + index + 1
    },
    
    getDeviceStatusType(device) {
      if (!device.isActive) return 'info'
      
      // 根据最后数据时间判断设备状态
      if (!device.lastDataTime) return 'warning'
      
      const lastTime = new Date(device.lastDataTime).getTime()
      const now = Date.now()
      const offlineThreshold = this.settingsForm.offlineThreshold * 60 * 1000
      
      if (now - lastTime > offlineThreshold) {
        return 'danger'
      }
      
      return 'success'
    },
    
    getDeviceStatusText(device) {
      if (!device.isActive) return '停用'
      
      if (!device.lastDataTime) return '无数据'
      
      const lastTime = new Date(device.lastDataTime).getTime()
      const now = Date.now()
      const offlineThreshold = this.settingsForm.offlineThreshold * 60 * 1000
      
      if (now - lastTime > offlineThreshold) {
        return '离线'
      }
      
      return '在线'
    },
    
    getDataValueClass(value, factor) {
      if (value === null) return 'value-null'
      
      // 这里可以根据阈值判断数值是否异常
      // 简化实现，只判断是否在合理范围内
      const ranges = {
        temperature: { min: 10, max: 40 },
        humidity: { min: 20, max: 80 }
      }
      
      const range = ranges[factor]
      if (!range) return 'value-normal'
      
      if (value < range.min || value > range.max) {
        return 'value-warning'
      }
      
      return 'value-normal'
    },
    
    formatTimeAgo(timestamp) {
      if (!timestamp) return '--'
      
      const now = new Date()
      const time = new Date(timestamp)
      const diff = Math.floor((now - time) / 1000)
      
      if (diff < 60) {
        return `${diff}秒前`
      } else if (diff < 3600) {
        return `${Math.floor(diff / 60)}分钟前`
      } else if (diff < 86400) {
        return `${Math.floor(diff / 3600)}小时前`
      } else {
        return `${Math.floor(diff / 86400)}天前`
      }
    },
    
    formatDateTime(timestamp) {
      if (!timestamp) return '--'
      return format(new Date(timestamp), 'yyyy-MM-dd HH:mm:ss')
    },
    
    formatTime(timestamp) {
      if (!timestamp) return '--'
      return format(new Date(timestamp), 'HH:mm:ss')
    },
    
    formatBytes(bytes) {
      if (!bytes) return '0 B'
      
      const units = ['B', 'KB', 'MB', 'GB', 'TB']
      let i = 0
      
      while (bytes >= 1024 && i < units.length - 1) {
        bytes /= 1024
        i++
      }
      
      return `${bytes.toFixed(2)} ${units[i]}`
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
    
    async viewDeviceDetails(device) {
      this.currentDevice = device
      this.detailDialogVisible = true
      this.detailActiveTab = 'basic'
      
      // 加载设备实时数据
      await this.loadDeviceRealtimeData(device.id)
      
      // 加载设备历史数据
      await this.loadDeviceHistoryData(device.id)
      
      // 加载设备阈值
      await this.loadDeviceThresholds(device.id)
    },
    
    async loadDeviceRealtimeData(deviceId) {
      try {
        const response = await this.$api.getLatestData(deviceId)
        if (response.success) {
          this.deviceRealtimeData = response.data
        }
      } catch (error) {
        console.error('加载设备实时数据失败:', error)
        this.deviceRealtimeData = null
      }
    },
    
    async loadDeviceHistoryData(deviceId) {
      try {
        const endTime = new Date()
        const startTime = subHours(endTime, 24)
        
        const response = await this.$api.getHistoricalData({
          deviceId: deviceId,
          startTime: format(startTime, 'yyyy-MM-dd HH:mm:ss'),
          endTime: format(endTime, 'yyyy-MM-dd HH:mm:ss'),
          pageSize: 100
        })
        
        if (response.success) {
          this.deviceHistoryData = response.data.data
          this.updateDeviceChart()
        }
      } catch (error) {
        console.error('加载设备历史数据失败:', error)
        this.deviceHistoryData = []
      }
    },
    
    async loadDeviceThresholds(deviceId) {
      try {
        const response = await this.$api.getDeviceThresholds(deviceId)
        if (response.success) {
          this.deviceThresholds = response.data
        }
      } catch (error) {
        console.error('加载设备阈值失败:', error)
        this.deviceThresholds = []
      }
    },
    
    updateDeviceChart() {
      if (this.deviceHistoryData.length === 0) return
      
      const temperatureData = this.deviceHistoryData
        .filter(item => item.temperature != null)
        .map(item => [new Date(item.timestamp), item.temperature])
      
      const humidityData = this.deviceHistoryData
        .filter(item => item.humidity != null)
        .map(item => [new Date(item.timestamp), item.humidity])
      
      this.deviceChartOption = {
        title: {
          text: '温度湿度趋势',
          left: 'center'
        },
        tooltip: {
          trigger: 'axis',
          axisPointer: {
            type: 'cross'
          }
        },
        legend: {
          data: ['温度', '湿度'],
          top: 30
        },
        grid: {
          left: '3%',
          right: '4%',
          bottom: '3%',
          top: '15%',
          containLabel: true
        },
        xAxis: {
          type: 'time',
          axisLabel: {
            formatter: value => format(new Date(value), 'HH:mm')
          }
        },
        yAxis: [
          {
            type: 'value',
            name: '温度(°C)',
            position: 'left'
          },
          {
            type: 'value',
            name: '湿度(%)',
            position: 'right'
          }
        ],
        series: [
          {
            name: '温度',
            type: 'line',
            yAxisIndex: 0,
            data: temperatureData,
            smooth: true,
            symbol: 'circle',
            symbolSize: 4,
            lineStyle: {
              width: 2,
              color: '#ff6b6b'
            },
            itemStyle: {
              color: '#ff6b6b'
            }
          },
          {
            name: '湿度',
            type: 'line',
            yAxisIndex: 1,
            data: humidityData,
            smooth: true,
            symbol: 'circle',
            symbolSize: 4,
            lineStyle: {
              width: 2,
              color: '#4ecdc4'
            },
            itemStyle: {
              color: '#4ecdc4'
            }
          }
        ]
      }
    },
    
    goToDeviceDashboard() {
      if (this.currentDevice) {
        this.$router.push({
          path: '/dashboard',
          query: { deviceId: this.currentDevice.id }
        })
        this.detailDialogVisible = false
      }
    },
    
    goToThresholds() {
      if (this.currentDevice) {
        this.$router.push({
          path: '/thresholds',
          query: { deviceId: this.currentDevice.id }
        })
        this.detailDialogVisible = false
      }
    },
    
    handleCreate() {
      this.isEditing = false
      this.editForm = {
        deviceCode: '',
        deviceName: '',
        location: '',
        manufacturer: '',
        model: '',
        serialNumber: '',
        installationDate: format(new Date(), 'yyyy-MM-dd'),
        isActive: this.settingsForm.defaultDeviceStatus,
        description: ''
      }
      this.editDialogVisible = true
      this.$nextTick(() => {
        this.$refs.editForm?.clearValidate()
      })
    },
    
    editDevice(device) {
      this.isEditing = true
      this.editForm = {
        deviceCode: device.deviceCode,
        deviceName: device.deviceName,
        location: device.location || '',
        manufacturer: device.manufacturer || '',
        model: device.model || '',
        serialNumber: device.serialNumber || '',
        installationDate: device.installationDate || '',
        isActive: device.isActive,
        description: device.description || ''
      }
      this.editDialogVisible = true
      this.$nextTick(() => {
        this.$refs.editForm?.clearValidate()
      })
    },
    
    async submitEditForm() {
      try {
        await this.$refs.editForm.validate()
        
        this.submitting = true
        
        if (this.isEditing) {
          const device = this.devices.find(d => d.deviceCode === this.editForm.deviceCode)
          if (device) {
            await this.updateDevice({
              id: device.id,
              deviceData: this.editForm
            })
            this.$message.success('设备更新成功')
          }
        } else {
          await this.addDevice(this.editForm)
          this.$message.success('设备创建成功')
        }
        
        this.editDialogVisible = false
        await this.loadDevices()
        this.calculateStats()
      } catch (error) {
        if (error.response?.status === 400) {
          this.$message.error('设备编码已存在')
        } else {
          console.error('保存设备失败:', error)
          this.$message.error('保存设备失败')
        }
      } finally {
        this.submitting = false
      }
    },
    
    async toggleDeviceStatus(device) {
      const newStatus = !device.isActive
      const action = newStatus ? '启用' : '停用'
      
      try {
        await this.updateDevice({
          id: device.id,
          deviceData: { isActive: newStatus }
        })
        
        this.$message.success(`设备已${action}`)
        await this.loadDevices()
        this.calculateStats()
      } catch (error) {
        console.error(`${action}设备失败:`, error)
        this.$message.error(`${action}设备失败`)
      }
    },
    
    batchEnableDevices(enabled) {
      this.$confirm(`确定要${enabled ? '启用' : '停用'}选中的 ${this.selectedDevices.length} 台设备吗？`, '批量操作', {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(async () => {
        try {
          const promises = this.selectedDevices.map(device =>
            this.updateDevice({
              id: device.id,
              deviceData: { isActive: enabled }
            })
          )
          
          await Promise.all(promises)
          this.$message.success(`已${enabled ? '启用' : '停用'} ${this.selectedDevices.length} 台设备`)
          this.selectedDevices = []
          await this.loadDevices()
          this.calculateStats()
        } catch (error) {
          console.error('批量操作失败:', error)
          this.$message.error('批量操作失败')
        }
      }).catch(() => {})
    },
    
    batchDeleteDevices() {
      if (this.selectedDevices.length === 0) return
      
      this.$confirm(`确定要删除选中的 ${this.selectedDevices.length} 台设备吗？此操作不可恢复。`, '批量删除', {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }).then(async () => {
        try {
          const promises = this.selectedDevices.map(device =>
            this.deleteDevice(device.id)
          )
          
          await Promise.all(promises)
          this.$message.success(`已删除 ${this.selectedDevices.length} 台设备`)
          this.selectedDevices = []
          await this.loadDevices()
          this.calculateStats()
        } catch (error) {
          console.error('批量删除失败:', error)
          this.$message.error('批量删除失败')
        }
      }).catch(() => {})
    },
    
    batchMonitor() {
      if (this.selectedDevices.length === 0) return
      
      const deviceIds = this.selectedDevices.map(d => d.id)
      this.$router.push({
        path: '/dashboard',
        query: { deviceIds: deviceIds.join(',') }
      })
    },
    
    exportDevices() {
      try {
        const data = this.tableData.map(device => ({
          设备编码: device.deviceCode,
          设备名称: device.deviceName,
          安装位置: device.location,
          制造商: device.manufacturer,
          设备型号: device.model,
          序列号: device.serialNumber,
          安装日期: device.installationDate,
          设备状态: device.isActive ? '启用' : '停用',
          创建时间: format(new Date(device.createdAt), 'yyyy-MM-dd HH:mm:ss'),
          最后更新: format(new Date(device.updatedAt), 'yyyy-MM-dd HH:mm:ss')
        }))
        
        const worksheet = XLSX.utils.json_to_sheet(data)
        const workbook = XLSX.utils.book_new()
        XLSX.utils.book_append_sheet(workbook, worksheet, '设备列表')
        
        // 设置列宽
        const colWidths = [
          { wch: 15 }, // 设备编码
          { wch: 20 }, // 设备名称
          { wch: 20 }, // 安装位置
          { wch: 15 }, // 制造商
          { wch: 15 }, // 设备型号
          { wch: 20 }, // 序列号
          { wch: 12 }, // 安装日期
          { wch: 10 }, // 设备状态
          { wch: 20 }, // 创建时间
          { wch: 20 }  // 最后更新
        ]
        worksheet['!cols'] = colWidths
        
        const excelBuffer = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' })
        const excelBlob = new Blob([excelBuffer], { 
          type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' 
        })
        
        const filename = `设备列表_${format(new Date(), 'yyyyMMddHHmmss')}.xlsx`
        FileSaver.saveAs(excelBlob, filename)
        
        this.$message.success('设备导出成功')
      } catch (error) {
        console.error('导出设备失败:', error)
        this.$message.error('导出设备失败')
      }
    },
    
    importDevices() {
      this.importDialogVisible = true
      this.importPreviewData = []
    },
    
    downloadTemplate() {
      const template = [
        {
          '设备编码*': 'DEV001',
          '设备名称*': '温度传感器-01',
          '安装位置': '实验室A区',
          '制造商': '华为',
          '设备型号': 'WS-100',
          '序列号': 'SN2023001',
          '安装日期': '2023-01-15',
          '设备状态': '启用',
          '设备描述': '实验室温度监测设备'
        }
      ]
      
      const worksheet = XLSX.utils.json_to_sheet(template)
      const workbook = XLSX.utils.book_new()
      XLSX.utils.book_append_sheet(workbook, worksheet, '模板')
      
      const excelBuffer = XLSX.write(workbook, { bookType: 'xlsx', type: 'array' })
      const excelBlob = new Blob([excelBuffer], { 
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' 
      })
      
      FileSaver.saveAs(excelBlob, '设备导入模板.xlsx')
    },
    
    beforeUpload(file) {
      const isExcel = file.type.includes('excel') || file.type.includes('spreadsheet')
      const isCSV = file.type === 'text/csv' || file.name.endsWith('.csv')
      
      if (!isExcel && !isCSV) {
        this.$message.error('只能上传 Excel 或 CSV 文件')
        return false
      }
      
      if (file.size > 10 * 1024 * 1024) {
        this.$message.error('文件大小不能超过 10MB')
        return false
      }
      
      return true
    },
    
    handleUploadSuccess(response, file) {
      const reader = new FileReader()
      reader.onload = (e) => {
        try {
          const data = new Uint8Array(e.target.result)
          const workbook = XLSX.read(data, { type: 'array' })
          const worksheet = workbook.Sheets[workbook.SheetNames[0]]
          const jsonData = XLSX.utils.sheet_to_json(worksheet)
          
          this.importPreviewData = jsonData.map((row, index) => ({
            deviceCode: row['设备编码*'] || row['设备编码'],
            deviceName: row['设备名称*'] || row['设备名称'],
            location: row['安装位置'] || '',
            manufacturer: row['制造商'] || '',
            model: row['设备型号'] || '',
            serialNumber: row['序列号'] || '',
            installationDate: row['安装日期'] || '',
            isActive: (row['设备状态'] || '启用') === '启用',
            description: row['设备描述'] || ''
          })).filter(item => item.deviceCode && item.deviceName)
          
          this.$message.success(`成功解析 ${this.importPreviewData.length} 条设备数据`)
        } catch (error) {
          console.error('解析文件失败:', error)
          this.$message.error('解析文件失败，请检查文件格式')
        }
      }
      reader.readAsArrayBuffer(file)
    },
    
    handleUploadError(error, file) {
      console.error('上传失败:', error)
      this.$message.error('文件上传失败')
    },
    
    async confirmImport() {
      if (this.importPreviewData.length === 0) {
        this.$message.warning('没有可导入的设备数据')
        return
      }
      
      this.importing = true
      
      try {
        const successCount = 0
        const errorCount = 0
        const errors = []
        
        for (const deviceData of this.importPreviewData) {
          try {
            await this.addDevice(deviceData)
            successCount++
          } catch (error) {
            errorCount++
            errors.push(`设备 ${deviceData.deviceCode}: ${error.message}`)
          }
        }
        
        if (errors.length > 0) {
          this.$message.warning(`导入完成，成功 ${successCount} 条，失败 ${errorCount} 条`)
          console.error('导入错误:', errors)
        } else {
          this.$message.success(`成功导入 ${successCount} 条设备数据`)
        }
        
        this.importDialogVisible = false
        this.importPreviewData = []
        await this.loadDevices()
        this.calculateStats()
      } catch (error) {
        console.error('导入设备失败:', error)
        this.$message.error('导入设备失败')
      } finally {
        this.importing = false
      }
    },
    
    clearImport() {
      this.importPreviewData = []
    },
    
    printDevices() {
      window.print()
    },
    
    showSettings() {
      this.settingsDialogVisible = true
    },
    
    loadSettings() {
      const savedSettings = localStorage.getItem('deviceSettings')
      if (savedSettings) {
        this.settingsForm = { ...this.settingsForm, ...JSON.parse(savedSettings) }
        this.pageSize = this.settingsForm.defaultPageSize
      }
    },
    
    saveSettings() {
      localStorage.setItem('deviceSettings', JSON.stringify(this.settingsForm))
      this.settingsDialogVisible = false
      this.$message.success('设置已保存')
      
      // 应用设置
      this.pageSize = this.settingsForm.defaultPageSize
      if (this.settingsForm.autoRefreshData) {
        this.startAutoRefresh()
      }
    },
    
    startAutoRefresh() {
      this.stopAutoRefresh()
      this.refreshTimer = setInterval(() => {
        if (document.visibilityState === 'visible') {
          this.loadDevices()
        }
      }, this.settingsForm.refreshInterval * 1000)
    },
    
    stopAutoRefresh() {
      if (this.refreshTimer) {
        clearInterval(this.refreshTimer)
        this.refreshTimer = null
      }
    },
    
    updateEmptyMessage() {
      if (this.total === 0) {
        this.emptyMessage = '暂无设备数据'
      } else {
        this.emptyMessage = ''
      }
    },
    
    toggleAdvancedQuery() {
      this.showAdvancedQuery = !this.showAdvancedQuery
    }
  }
}
</script>

<style lang="scss" scoped>
.devices-container {
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
  
  .device-stats {
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
      
      &.active {
        border-left: 4px solid #67c23a;
      }
      
      &.warning {
        border-left: 4px solid #e6a23c;
      }
      
      &.data {
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
          
          .stat-card.active & {
            background: linear-gradient(135deg, #67c23a20, #67c23a40);
            color: #67c23a;
          }
          
          .stat-card.warning & {
            background: linear-gradient(135deg, #e6a23c20, #e6a23c40);
            color: #e6a23c;
          }
          
          .stat-card.data & {
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
              color: #67c23a;
            }
            
            &.trend-down {
              color: #f56c6c;
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
  
  .devices-card {
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
      
      .device-code-cell {
        .el-tag {
          min-width: 80px;
        }
      }
      
      .device-name-cell {
        .name-main {
          font-weight: 600;
          color: var(--text-color-primary);
        }
        
        .name-model {
          font-size: 12px;
          color: var(--text-color-secondary);
          margin-top: 2px;
        }
      }
      
      .device-status {
        .status-detail {
          font-size: 12px;
          color: var(--text-color-secondary);
          margin-top: 2px;
        }
      }
      
      .device-data {
        .data-item {
          display: flex;
          align-items: center;
          justify-content: space-between;
          margin-bottom: 4px;
          font-size: 13px;
          
          .label {
            color: var(--text-color-secondary);
          }
          
          .value {
            font-weight: 600;
            
            &.value-normal {
              color: #67c23a;
            }
            
            &.value-warning {
              color: #e6a23c;
            }
            
            &.value-null {
              color: var(--text-color-secondary);
            }
          }
        }
      }
      
      .data-stats {
        .stat-item {
          display: flex;
          align-items: center;
          justify-content: space-between;
          margin-bottom: 4px;
          font-size: 13px;
          
          .label {
            color: var(--text-color-secondary);
          }
          
          .value {
            font-weight: 600;
            color: var(--text-color-primary);
          }
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
  
  .batch-actions {
    .batch-content {
      display: flex;
      align-items: center;
      justify-content: space-between;
      
      .batch-count {
        font-weight: 600;
        color: var(--text-color-primary);
      }
      
      .batch-buttons {
        display: flex;
        gap: 10px;
      }
    }
  }
  
  .device-details {
    .realtime-data {
      .data-cards {
        .data-card {
          display: flex;
          align-items: center;
          gap: 15px;
          
          .data-icon {
            width: 50px;
            height: 50px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 24px;
            
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
          
          .data-info {
            .data-label {
              font-size: 14px;
              color: var(--text-color-secondary);
              margin-bottom: 5px;
            }
            
            .data-value {
              font-size: 20px;
              font-weight: 700;
              color: var(--text-color-primary);
              margin-bottom: 5px;
            }
            
            .data-time {
              font-size: 12px;
              color: var(--text-color-secondary);
            }
          }
        }
      }
    }
    
    .device-statistics {
      .stat-content {
        padding: 15px 0;
      }
      
      .data-chart {
        margin-top: 20px;
        
        h4 {
          margin: 20px 0 10px 0;
          font-size: 16px;
          font-weight: 600;
          color: var(--text-color-primary);
        }
      }
    }
    
    .device-thresholds {
      .thresholds-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        margin-bottom: 15px;
        
        span {
          font-weight: 600;
          font-size: 16px;
          color: var(--text-color-primary);
        }
      }
    }
  }
  
  .import-content {
    .import-template {
      margin: 20px 0;
      
      h4 {
        margin-bottom: 10px;
        color: var(--text-color-primary);
      }
    }
    
    .import-upload {
      margin: 20px 0;
      
      h4 {
        margin-bottom: 10px;
        color: var(--text-color-primary);
      }
    }
    
    .import-preview {
      margin-top: 20px;
      
      h4 {
        margin-bottom: 10px;
        color: var(--text-color-primary);
      }
      
      .import-actions {
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
  .devices-container {
    .page-header {
      flex-direction: column;
      align-items: flex-start;
      gap: 10px;
    }
    
    .device-stats {
      .el-col {
        margin-bottom: 10px;
      }
    }
    
    .devices-card {
      .table-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 10px;
      }
    }
    
    .batch-actions {
      .batch-content {
        flex-direction: column;
        align-items: flex-start;
        gap: 10px;
      }
    }
    
    .device-details {
      .realtime-data {
        .data-cards {
          .el-col {
            margin-bottom: 10px;
          }
        }
      }
      
      .device-statistics {
        .el-col {
          margin-bottom: 10px;
        }
      }
    }
  }
}
</style>