// import { createApp } from 'vue';
// import App from './App.vue';
// import router from './router';

// createApp(App)
// .use(router)
// .mount('#app');

 
 
import { createApp } from 'vue'
import App from './App.vue'

import ElementPlus from 'element-plus'
import 'element-plus/dist/index.css'

// 引入中文
import zhCn from 'element-plus/es/locale/lang/zh-cn'

const app = createApp(App)
app.use(ElementPlus, {
  locale: zhCn
})

app.mount('#app')