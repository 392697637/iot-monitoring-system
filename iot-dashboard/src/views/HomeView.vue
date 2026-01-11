<template>
  <div>
    <h1>IoT 仪表盘系统</h1>
    
    <select v-model="selectedDeviceId">
      <option v-for="d in devices" :key="d.deviceId" :value="d.deviceId">{{ d.deviceName }}</option>
    </select>

    <DeviceDashboard v-if="selectedDeviceId" 
               :deviceId="selectedDeviceId" 
               :deviceName="selectedDeviceName" />

    <HistoryTable v-if="selectedDeviceId"
                  :deviceId="selectedDeviceId"
                  :factor="selectedFactor"
                  :start="startDate"
                  :end="endDate" />

    <AlarmSound :play="hasAlarm" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import DeviceDashboard from '@/components/DeviceDashboard.vue';
import HistoryTable from '@/components/HistoryTable.vue';
import AlarmSound from '@/components/AlarmSound.vue';
import { getDevices, getDeviceAlarms } from '@/api/device';

const devices = ref([]);
const selectedDeviceId = ref(null);
const selectedFactor = ref('温度');
const startDate = ref('2026-01-01 00:00');
const endDate = ref('2026-01-11 23:59');
const hasAlarm = ref(false);

const selectedDeviceName = computed(() => {
  const device = devices.value.find(d => d.deviceId === selectedDeviceId.value);
  return device ? device.deviceName : '';
});

const fetchDevices = async () => {
  const res = await getDevices();
  devices.value = res.data;
};

const fetchAlarms = async () => {
  if (!selectedDeviceId.value) return;
  const res = await getDeviceAlarms(selectedDeviceId.value);
  hasAlarm.value = res.data.length > 0;
};

onMounted(() => {
  fetchDevices();
  setInterval(fetchAlarms, 60000);
});
</script>
