<template>
  <div>
    <h2>{{ deviceName }} 实时数据</h2>
    <v-chart :option="chartOption" style="width: 100%; height: 400px;"></v-chart>
  </div>
</template>

<script>
import VueECharts from 'vue-echarts';
import { getRealtimeData } from '@/api/device';

export default {
  components: { 'v-chart': VueECharts },
  props: ['deviceId', 'deviceName'],
  data() {
    return {
      chartOption: {
        title: { text: '实时监控' },
        tooltip: {},
        xAxis: { type: 'category', data: ['温度','湿度','电流','电压','状态'] },
        yAxis: { type: 'value' },
        series: [{ name: '数值', type: 'bar', data: [0,0,0,0,0] }]
      }
    };
  },
  methods: {
    fetchRealtime() {
      getRealtimeData(this.deviceId).then(res => {
        // res 返回数组 [{FactorName, Value, Status}]
        const data = ['温度','湿度','电流','电压','状态'].map(name => {
          const item = res.data.find(f => f.factorName === name);
          return item ? item.value : 0;
        });
        this.chartOption.series[0].data = data;
      });
    }
  },
  mounted() {
    this.fetchRealtime();
    setInterval(this.fetchRealtime, 60000); // 每分钟刷新一次
  }
};
</script>
