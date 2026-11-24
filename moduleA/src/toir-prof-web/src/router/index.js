import { createRouter, createWebHistory } from 'vue-router';
import { useAuthStore } from '../stores/authStore';

import DashboardView from '../views/DashboardView.vue';
import AnalyticsView from '../views/AnalyticsView.vue';
import WorkOrdersView from '../views/WorkOrdersView.vue';
import EquipmentView from '../views/EquipmentView.vue';
import SchedulesView from '../views/SchedulesView.vue';
import CatalogsView from '../views/CatalogsView.vue';
import ReportsView from '../views/ReportsView.vue';
import UsersView from '../views/UsersView.vue';
import LoginView from '../views/LoginView.vue';

const routes = [
  { path: '/login', name: 'login', component: LoginView, meta: { public: true } },
  { path: '/', name: 'dashboard', component: DashboardView, meta: { title: 'Дашборд KPI' } },
  { path: '/analytics', name: 'analytics', component: AnalyticsView, meta: { title: 'Аналитика ТОиР' } },
  { path: '/work-orders', name: 'work-orders', component: WorkOrdersView, meta: { title: 'Заявки на ремонт' } },
  { path: '/equipment', name: 'equipment', component: EquipmentView, meta: { title: 'Оборудование' } },
  { path: '/schedules', name: 'schedules', component: SchedulesView, meta: { title: 'График ТО' } },
  { path: '/catalogs', name: 'catalogs', component: CatalogsView, meta: { title: 'Справочники НСИ' } },
  { path: '/reports', name: 'reports', component: ReportsView, meta: { title: 'Отчёты системы' } },
  { path: '/users', name: 'users', component: UsersView, meta: { title: 'Пользователи и роли' } },
  { path: '/:pathMatch(.*)*', redirect: '/' }
];

const router = createRouter({
  history: createWebHistory(),
  routes
});

router.beforeEach((to, from, next) => {
  const authStore = useAuthStore();
  if (!to.meta.public && !authStore.isAuthenticated) {
    next({ name: 'login' });
  } else if (to.name === 'login' && authStore.isAuthenticated) {
    next({ name: 'dashboard' });
  } else {
    next();
  }
});

export default router;
