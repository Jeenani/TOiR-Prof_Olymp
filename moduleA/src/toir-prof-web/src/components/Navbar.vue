<script setup>
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { connectionStatus } from '../services/realtime';
import { api } from '../services/api';

defineProps({ sidebarCollapsed: Boolean });
const emit = defineEmits(['toggleSidebar']);

const router = useRouter();
const authStore = useAuthStore();

const searchQuery = ref('');
const searchResults = ref(null);
const showNotifications = ref(false);

const notifications = ref([
  { id: 1, title: 'Плановое ТО-1', desc: 'Станок токарный EQ-001 запланирован на смену', time: '10 мин назад' },
  { id: 2, title: 'Аварийный простой', desc: 'Заявка Z-2027-0002 в работе у бригады', time: '1 час назад' }
]);

async function handleSearch() {
  const q = searchQuery.value.trim();
  if (q.length < 2) { searchResults.value = null; return; }
  try {
    const [eqRes, woRes, usersRes] = await Promise.all([
      api.get('/equipment', { search: q, pageSize: 4 }),
      api.get('/work-orders', { search: q, pageSize: 4 }),
      api.get('/auth/users', { search: q }).catch(() => [])
    ]);
    searchResults.value = {
      equipment: eqRes.items || [],
      orders: woRes.items || [],
      users: (usersRes || []).slice(0, 4)
    };
  } catch (e) {
    console.error('Ошибка глобального поиска:', e);
  }
}

function selectResult(type, item) {
  searchResults.value = null;
  searchQuery.value = '';
  if (type === 'equipment') router.push(`/equipment?search=${encodeURIComponent(item.inventoryNumber)}`);
  else if (type === 'order') router.push(`/work-orders?search=${encodeURIComponent(item.orderNumber)}`);
  else router.push(`/users?search=${encodeURIComponent(item.fullName)}`);
}

function handleLogout() {
  authStore.logout();
  router.push('/login');
}
</script>

<template>
  <header class="navbar">
    <div class="navbar-left">
      <button class="menu-toggle" @click="emit('toggleSidebar')" title="Свернуть / Развернуть">☰</button>
      <div class="logo">
        <span class="logo-badge">ТОиР</span>
        <span class="logo-title">Проф</span>
        <span class="logo-sub">Enterprise CMMS</span>
      </div>
    </div>

    <div class="navbar-center">
      <div class="search-box">
        <span class="search-icon">🔍</span>
        <input v-model="searchQuery" @input="handleSearch" type="text" placeholder="Поиск: № заявки, инв. №, ФИО..." />
        <div v-if="searchQuery" class="clear-search" @click="searchQuery = ''; searchResults = null">✕</div>

        <div v-if="searchResults" class="search-dropdown">
          <div v-if="!searchResults.equipment.length && !searchResults.orders.length && !searchResults.users.length" class="no-results">Ничего не найдено</div>
          <div v-if="searchResults.equipment.length" class="search-group">
            <div class="group-title">Оборудование</div>
            <div v-for="eq in searchResults.equipment" :key="eq.equipmentId" class="search-item" @click="selectResult('equipment', eq)">
              <span class="item-tag tag-blue">{{ eq.inventoryNumber }}</span>
              <span>{{ eq.equipmentName }} ({{ eq.workshopName }})</span>
            </div>
          </div>
          <div v-if="searchResults.orders.length" class="search-group">
            <div class="group-title">Заявки</div>
            <div v-for="wo in searchResults.orders" :key="wo.workOrderId" class="search-item" @click="selectResult('order', wo)">
              <span class="item-tag tag-orange">{{ wo.orderNumber }}</span>
              <span>{{ wo.equipmentName }} — {{ wo.statusName }}</span>
            </div>
          </div>
          <div v-if="searchResults.users.length" class="search-group">
            <div class="group-title">Сотрудники</div>
            <div v-for="u in searchResults.users" :key="u.userId" class="search-item" @click="selectResult('user', u)">
              <span class="item-tag tag-blue">{{ u.roleName }}</span>
              <span>{{ u.fullName }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>

    <div class="navbar-right">
      <div class="connection-badge" :class="`conn-${connectionStatus}`">
        <span class="conn-dot"></span>
        <span>{{ connectionStatus === 'connected' ? 'В сети' : 'Переподключение' }}</span>
      </div>

      <div class="notifications-wrapper">
        <button class="icon-btn" @click="showNotifications = !showNotifications">
          🔔<span class="badge-count">{{ notifications.length }}</span>
        </button>
        <div v-if="showNotifications" class="notifications-dropdown">
          <div class="notif-header"><span>Уведомления</span><span class="notif-count">{{ notifications.length }} новых</span></div>
          <div v-for="n in notifications" :key="n.id" class="notif-item">
            <div class="notif-title">{{ n.title }}</div>
            <div class="notif-desc">{{ n.desc }}</div>
            <div class="notif-time">{{ n.time }}</div>
          </div>
        </div>
      </div>

      <div class="user-profile">
        <div class="avatar">{{ authStore.user?.firstName?.[0] || 'A' }}</div>
        <div class="user-info">
          <div class="user-name">{{ authStore.user?.fullName || 'Пользователь' }}</div>
          <div class="user-role">{{ authStore.user?.roleName || 'Оператор' }} • {{ authStore.user?.workshopName || 'Заводоуправление' }}</div>
        </div>
        <button class="logout-btn" @click="handleLogout" title="Выйти">➔</button>
      </div>
    </div>
  </header>
</template>

