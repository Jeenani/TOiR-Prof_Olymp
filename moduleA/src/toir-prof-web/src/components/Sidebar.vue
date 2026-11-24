<script setup>
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useAuthStore } from '../stores/authStore';

const props = defineProps({
  collapsed: Boolean
});

const route = useRoute();
const authStore = useAuthStore();

const menuItems = [
  { path: '/', label: 'Дашборд', icon: '📊', group: 'Обзор' },
  { path: '/analytics', label: 'Аналитика', icon: '📈', group: 'Обзор' },
  { path: '/work-orders', label: 'Заявки ТОиР', icon: '🔧', group: 'Операции' },
  { path: '/equipment', label: 'Оборудование', icon: '🏭', group: 'Операции' },
  { path: '/schedules', label: 'График ТО', icon: '📅', group: 'Операции' },
  { path: '/catalogs', label: 'Справочники', icon: '📦', group: 'Система' },
  { path: '/reports', label: 'Отчёты', icon: '📑', group: 'Система' },
  { path: '/users', label: 'Пользователи', icon: '👥', group: 'Система', adminOnly: true }
];

const filteredItems = computed(() => {
  if (authStore.isAdmin) return menuItems;
  return menuItems.filter(item => !item.adminOnly);
});
</script>

<template>
  <aside class="sidebar" :class="{ 'collapsed': collapsed }">
    <div class="sidebar-menu">
      <template v-for="(item, idx) in filteredItems" :key="item.path">
        <div
          v-if="!collapsed && (idx === 0 || item.group !== filteredItems[idx - 1]?.group)"
          class="menu-group-header"
        >
          {{ item.group }}
        </div>
        <router-link
          :to="item.path"
          class="menu-link"
          :class="{ 'active': route.path === item.path }"
          :title="collapsed ? item.label : ''"
        >
          <span class="menu-icon">{{ item.icon }}</span>
          <span v-if="!collapsed" class="menu-label">{{ item.label }}</span>
        </router-link>
      </template>
    </div>

    <div v-if="!collapsed" class="sidebar-footer">
      <div class="version-tag">ТОиР-Проф v1.0.0</div>
      <div class="legal-tag">WorldSkills Enterprise</div>
    </div>
  </aside>
</template>
