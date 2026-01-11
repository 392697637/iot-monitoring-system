import Vue from 'vue'
import Vuex from 'vuex'
import createPersistedState from 'vuex-persistedstate'

Vue.use(Vuex)

// 模块
import connection from './modules/connection'
import devices from './modules/devices'
import alarms from './modules/alarms'
import thresholds from './modules/thresholds'
import user from './modules/user'

export default new Vuex.Store({
  modules: {
    connection,
    devices,
    alarms,
    thresholds,
    user
  },
  
  plugins: [
    createPersistedState({
      key: 'iot-monitoring',
      paths: [
        'connection.isAlarmMuted',
        'connection.autoRefresh',
        'devices.selectedDeviceId',
        'user.theme',
        'user.settings'
      ],
      storage: window.localStorage
    })
  ],
  
  strict: process.env.NODE_ENV !== 'production'
})