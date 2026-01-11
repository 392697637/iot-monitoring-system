<template>
  <div>
    <h3>历史数据</h3>
    <table border="1">
      <thead>
        <tr>
          <th>时间</th>
          <th>因子</th>
          <th>数值</th>
          <th>状态</th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="item in historyData" :key="item.dataId">
          <td>{{ item.createdAt }}</td>
          <td>{{ item.factorName }}</td>
          <td>{{ item.value }}</td>
          <td>{{ item.status }}</td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<script>
import { getHistoryData } from '@/api/device';

export default {
  props: ['deviceId','factor','start','end'],
  data() {
    return { historyData: [] };
  },
  watch: {
    deviceId: 'fetchHistory',
    factor: 'fetchHistory',
    start: 'fetchHistory',
    end: 'fetchHistory'
  },
  methods: {
    fetchHistory() {
      if (!this.deviceId || !this.factor) return;
      getHistoryData(this.deviceId, this.factor, this.start, this.end).then(res => {
        this.historyData = res.data;
      });
    }
  },
  mounted() { this.fetchHistory(); }
};
</script>
