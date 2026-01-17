<template>
  <el-card>
    <h2>实时监控</h2>
    <el-select v-model="deviceId" placeholder="选择设备">
      <el-option
        v-for="d in devices"
        :key="d.deviceId"
        :label="d.deviceName"
        :value="d.deviceId"
      />
    </el-select>

    <el-row :gutter="20" style="margin-top: 20px">
      <el-col :span="6" v-for="metric in metrics" :key="metric.label">
        <el-card :class="{ alarm: metric.alarm }">
          <div>{{ metric.label }}</div>
          <div>{{ metric.value }}</div>
        </el-card>
      </el-col>
    </el-row>
  </el-card>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from "vue"; // ✅ 添加 onUnmounted
import {
  getDevices,
  getLatestData,
  getDeviceTable,
  getDataByTableName,
} from "../api/device";

const devices = ref([]);
const deviceId = ref(null);
const tablename = ref(null);

const metrics = ref([
  { label: "温度 (℃)", value: "-", alarm: false },
  { label: "湿度 (%)", value: "-", alarm: false },
  { label: "电压 (V)", value: "-", alarm: false },
  { label: "电流 (A)", value: "-", alarm: false },
]);

let timer = null;
const alarmAudio = new Audio("/data/alarm.mp3");
onMounted(async () => {
  const res = await getDevices();

  devices.value = res.data;
  if (devices.value.length > 0) {
    deviceId.value = devices.value[0].deviceId;
    tablename.value = devices.value[0].deviceTable;
    loadDeviceTable();
    loadData();
    timer = setInterval(loadData, 5000);
  }
});
//设备因子显示
const loadDeviceTable = async () => {
  if (!tablename.value) return;
  const res = await getDeviceTable(tablename.value);
  const d = res.data;
  if (!d) return;
  d.forEach((m) => {
    metrics.value.push({ label: m.displayName, value: "-", alarm: false });
  });
};
//数据添加
const loadData = async () => {
  if (!tablename.value) return;
  var topNumber = 1;
  var orderby = "DID";
  const res = await getDataByTableName(tablename.value, topNumber, orderby);
  const d = res.data;
  debugger;
  if (!d) return;
 var seledata= d.data[0];
    debugger;
    metrics.value.forEach((metr) => {
      metr.value=seledata[metr.label]
    })
    
  var status = 0;
  //告警
  if (status === 1) alarmAudio.play();
};
// ✅ 修复 no-undef
onUnmounted(() => {
  if (timer) clearInterval(timer);
});
</script>

<style scoped>
.alarm {
  border: 2px solid red;
}
</style>
