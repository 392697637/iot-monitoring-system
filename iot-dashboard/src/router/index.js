import { createRouter, createWebHistory } from 'vue-router';
import HomeView from '@/views/HomeView.vue';
import NotFound from '@/views/NotFound.vue'; // 404 页面，可选

const routes = [
  {
    path: '/',
    name: 'HomeView',
    component: HomeView
  },
  // 可以扩展更多页面，比如设备详情
  {
    path: '/device/:id',
    name: 'DeviceDetail',
    component: () => import('@/views/HomeView.vue'), // 暂时用 Home.vue
    props: true
  },
  {
    path: '/:pathMatch(.*)*',
    name: 'NotFound',
    component: NotFound
  }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

export default router;
