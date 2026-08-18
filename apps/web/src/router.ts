import { createRouter, createWebHistory } from 'vue-router';
import BootstrapStatus from './views/BootstrapStatus.vue';

export const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/',
      name: 'bootstrap-status',
      component: BootstrapStatus
    }
  ]
});

