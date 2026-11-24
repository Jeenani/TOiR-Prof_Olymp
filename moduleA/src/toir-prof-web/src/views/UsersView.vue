<script setup>
import { ref, onMounted } from 'vue';
import { api } from '../services/api';
import { useToastStore } from '../stores/toastStore';

const toast = useToastStore();

const users = ref([
  { userId: 1, email: 'admin@pmz.ru', fullName: 'Администраторов Алексей Игоревич', roleName: 'Администратор системы', roleCode: 'admin', workshopName: 'Заводоуправление', isActive: true },
  { userId: 2, email: 'egorov.aa@pmz.ru', fullName: 'Егоров Анатолий Алексеевич', roleName: 'Главный инженер', roleCode: 'chief_engineer', workshopName: 'Заводоуправление', isActive: true },
  { userId: 3, email: 'belov.ia@pmz.ru', fullName: 'Белов Игорь Анатольевич', roleName: 'Диспетчер завода', roleCode: 'dispatcher', workshopName: 'Заводоуправление', isActive: true },
  { userId: 4, email: 'ivanov.ii@pmz.ru', fullName: 'Иванов Иван Иванович', roleName: 'Мастер цеха', roleCode: 'master', workshopName: 'ЦРМ (Цех 1)', isActive: true },
  { userId: 5, email: 'petrov.pp@pmz.ru', fullName: 'Петров Петр Петрович', roleName: 'Мастер цеха', roleCode: 'master', workshopName: 'Механический цех (Цех 2)', isActive: true },
  { userId: 7, email: 'sokolov.vm@pmz.ru', fullName: 'Соколов Владимир Михайлович', roleName: 'Бригадир', roleCode: 'team_leader', workshopName: 'ЦРМ (Цех 1)', isActive: true }
]);

function toggleUserStatus(u) {
  u.isActive = !u.isActive;
  toast.info(`Статус пользователя ${u.fullName} изменен на: ${u.isActive ? 'Активен' : 'Заблокирован'}`);
}
</script>

<template>
  <div class="users-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">Управление пользователями и правами (RBAC)</h1>
        <p class="page-sub">Назначение ролей, цехов приписки и контроль активности учётных записей</p>
      </div>
    </div>

    <!-- Таблица пользователей (R164) -->
    <div class="card">
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th>ID</th>
              <th>ФИО Сотрудника</th>
              <th>Email</th>
              <th>Роль доступа</th>
              <th>Цех приписки</th>
              <th>Статус</th>
              <th>Действие</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="u in users" :key="u.userId">
              <td>{{ u.userId }}</td>
              <td style="font-weight: 600;">{{ u.fullName }}</td>
              <td>{{ u.email }}</td>
              <td>
                <span class="badge" :class="u.roleCode === 'admin' ? 'badge-danger' : u.roleCode === 'master' ? 'badge-warning' : 'badge-info'">
                  {{ u.roleName }}
                </span>
              </td>
              <td>{{ u.workshopName }}</td>
              <td>
                <span class="badge" :class="u.isActive ? 'badge-success' : 'badge-danger'">
                  {{ u.isActive ? 'Активен' : 'Заблокирован' }}
                </span>
              </td>
              <td>
                <button
                  class="btn btn-sm"
                  :class="u.isActive ? 'btn-danger' : 'btn-success'"
                  @click="toggleUserStatus(u)"
                >
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

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; flex-wrap: wrap; gap: 14px; }
.page-title { font-size: 22px; font-weight: 800; color: #f8fafc; }
.page-sub { font-size: 13px; color: #94a3b8; }
</style>
