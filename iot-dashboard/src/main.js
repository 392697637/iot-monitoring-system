// import { createApp } from 'vue';
// import App from './App.vue';
// import router from './router';

// createApp(App)
// .use(router)
// .mount('#app');

import { createApp } from 'vue'
import App from './App.vue'
import config from './utils/config'
import apiService from './services/api'

import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'
// 引入中文
import zhCn from "element-plus/es/locale/lang/zh-cn";

// 1. 加载配置
const appConfig = await config.load();

// 2. 初始化 API 服务
await apiService.init(appConfig);
const app = createApp(App);
app.use(ElementPlus, {
  locale: zhCn,
});

app.mount("#app");
