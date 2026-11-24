<script setup>
import { ref } from 'vue';
import { useRoute } from 'vue-router';
import Navbar from './components/Navbar.vue';
import Sidebar from './components/Sidebar.vue';
import Toast from './components/Toast.vue';

const route = useRoute();
const sidebarCollapsed = ref(false);

function toggleSidebar() {
  sidebarCollapsed.value = !sidebarCollapsed.value;
}
</script>

<template>
  <div class="app-layout">
    <Toast />

    <!-- Страница входа без навбара и сайдбара -->
    <template v-if="route.name === 'login'">
      <router-view />
    </template>

    <!-- Основной интерфейс системы ТОиР-Проф -->
    <template v-else>
      <Navbar :sidebarCollapsed="sidebarCollapsed" @toggleSidebar="toggleSidebar" />
      <div class="main-wrapper">
        <Sidebar :collapsed="sidebarCollapsed" />
        <main class="content-area">
          <router-view />
        </main>
      </div>
    </template>
  </div>
</template>

