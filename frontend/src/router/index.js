import { createRouter, createWebHistory } from 'vue-router';
import SpotifyLogin from '../layouts/SpotifyLogin.vue';
import SpotifyCallback from '../layouts/SpotifyCallback.vue';

const routes = [
  {
    path: '/',
    name: 'SpotifyLogin',
    component: SpotifyLogin,
  },
  {
    path: '/callback',
    name: 'SpotifyCallback',
    component: SpotifyCallback,
  },
];

const router = createRouter({
  history: createWebHistory(),
  routes,
});

export default router;
