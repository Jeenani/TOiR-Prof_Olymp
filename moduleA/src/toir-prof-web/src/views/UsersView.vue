<script setup>
import { ref, onMounted } from 'vue';
import { api } from '../services/api';
import { useToastStore } from '../stores/toastStore';

const toast = useToastStore();
const users = ref([]);
const roles = ref([]);
const workshops = ref([]);

async function loadData() {
  try {
    const [u, r, w] = await Promise.all([api.get('/auth/users'), api.get('/auth/roles'), api.get('/auth/workshops')]);
    users.value = u || [];
    roles.value = r || [];
    workshops.value = w || [];
  } catch (e) {
    toast.error(e.message || 'Ошибка загрузки пользователей');
  }
}

async function updateUser(u, patch) {
  try {
    await api.put(`/auth/users/${u.userId}`, patch);
    toast.success(`Пользователь ${u.fullName} обновлён`);
    loadData();
  } catch (e) {
    toast.error(e.message || 'Не удалось обновить пользователя');
  }
}

onMounted(loadData);
</script>

<template>
  <div>
    <div class="page-header">
      <div>
        <h1 class="page-title">Управление пользователями (RBAC)</h1>
        <p class="page-sub">Назначение ролей, цехов приписки и блокировка учётных записей</p>
      </div>
    </div>

    <div class="card">
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr><th>ID</th><th>ФИО</th><th>Email</th><th>Роль</th><th>Цех</th><th>Статус</th><th>Действие</th></tr>
          </thead>
          <tbody>
            <tr v-for="u in users" :key="u.userId">
              <td>{{ u.userId }}</td>
              <td style="font-weight: 600;">{{ u.fullName }}</td>
              <td>{{ u.email }}</td>
              <td>
                <select :value="u.roleId" @change="updateUser(u, { roleId: +$event.target.value })" class="form-select" style="width: auto;">
                  <option v-for="r in roles" :key="r.roleId" :value="r.roleId">{{ r.roleName }}</option>
                </select>
              </td>
              <td>
                <select :value="u.workshopId || 0" @change="updateUser(u, { workshopId: +$event.target.value })" class="form-select" style="width: auto;">
                  <option :value="0">Заводоуправление</option>
                  <option v-for="w in workshops" :key="w.workshopId" :value="w.workshopId">{{ w.workshopName }}</option>
                </select>
              </td>
              <td><span class="badge" :class="u.isActive ? 'badge-success' : 'badge-danger'">{{ u.isActive ? 'Активен' : 'Заблокирован' }}</span></td>
              <td>
                <button class="btn btn-sm" :class="u.isActive ? 'btn-danger' : 'btn-success'" @click="updateUser(u, { isActive: !u.isActive })">
                  {{ u.isActive ? 'Блокировать' : 'Разблокировать' }}
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

