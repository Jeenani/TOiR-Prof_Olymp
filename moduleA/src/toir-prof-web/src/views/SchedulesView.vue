<script setup>
import { ref, onMounted } from 'vue';
import { api } from '../services/api';
import { useToastStore } from '../stores/toastStore';

const toast = useToastStore();

const schedules = ref([]);
const maintenanceTypes = ref([]);
const equipmentList = ref([]);
const teams = ref([]);
const isLoading = ref(false);

const filterMonth = ref(new Date().getMonth() + 1);
const filterYear = ref(2027);

const showModal = ref(false);
const form = ref({
  equipmentId: null,
  maintenanceTypeId: 1,
  assignedTeamId: 1,
  plannedDate: new Date().toISOString().slice(0, 10)
});

async function loadData() {
  isLoading.value = true;
  try {
    const [sc, mt, eq, tm] = await Promise.all([
      api.get('/schedules', { month: filterMonth.value, year: filterYear.value }),
      api.get('/schedules/types'),
      api.get('/equipment', { pageSize: 100 }),
      api.get('/auth/teams')
    ]);
    schedules.value = sc || [];
    maintenanceTypes.value = mt || [];
    equipmentList.value = eq.items || [];
    teams.value = tm || [];
    if (!form.value.equipmentId && equipmentList.value.length) {
      form.value.equipmentId = equipmentList.value[0].equipmentId;
    }
  } catch (e) {
    toast.error('Не удалось загрузить график ТО');
  } finally {
    isLoading.value = false;
  }
}

async function handleGenerateOrder(scheduleId) {
  try {
    const res = await api.post(`/schedules/${scheduleId}/generate-order`, { priorityId: 2 });
    toast.success(res.message);
    loadData();
  } catch (e) {
    toast.error(e.message || 'Ошибка генерации заявки');
  }
}

async function submitCreateSchedule() {
  if (!form.value.equipmentId || !form.value.plannedDate) {
    toast.warning('Заполните обязательные поля');
    return;
  }
  try {
    await api.post('/schedules', form.value);
    toast.success('Запись графика ТО добавлена');
    showModal.value = false;
    loadData();
  } catch (e) {
    toast.error(e.message || 'Ошибка добавления ТО');
  }
}

onMounted(() => { loadData(); });
</script>

<template>
  <div class="schedules-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">Календарный график ТО (ППР)</h1>
        <p class="page-sub">Планирование регламентных работ ТО-1, ТО-2, ТО-3 и капитальных ремонтов</p>
      </div>
      <button class="btn btn-primary" @click="showModal = true">+ Запланировать ТО</button>
    </div>

    <!-- Панель месяца / года (R161) -->
    <div class="filter-toolbar">
      <div class="filter-item">
        <label>Месяц:</label>
        <select v-model.number="filterMonth" @change="loadData" class="form-select">
          <option :value="1">Январь</option>
          <option :value="2">Февраль</option>
          <option :value="3">Март</option>
          <option :value="4">Апрель</option>
          <option :value="5">Май</option>
          <option :value="6">Июнь</option>
          <option :value="7">Июль</option>
          <option :value="8">Август</option>
          <option :value="9">Сентябрь</option>
          <option :value="10">Октябрь</option>
          <option :value="11">Ноябрь</option>
          <option :value="12">Декабрь</option>
        </select>
      </div>

      <div class="filter-item">
        <label>Год:</label>
        <select v-model.number="filterYear" @change="loadData" class="form-select">
          <option :value="2027">2027</option>
          <option :value="2026">2026</option>
        </select>
      </div>

      <button class="btn btn-secondary btn-sm" @click="loadData">Обновить</button>
    </div>

    <!-- Таблица плановых ТО с кнопкой генерации заявки в 1 клик (R161) -->
    <div class="card">
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th>Плановая дата</th>
              <th>Оборудование</th>
              <th>Цех</th>
              <th>Вид ТО</th>
              <th>Бригада</th>
              <th>Статус</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="isLoading"><td colspan="7" style="text-align: center; padding: 25px;">Загрузка графика...</td></tr>
            <tr v-else-if="!schedules.length"><td colspan="7" style="text-align: center; padding: 25px; color: #94a3b8;">В выбранном месяце работ не запланировано</td></tr>
            <tr v-for="s in schedules" :key="s.scheduleId">
              <td style="font-weight: 700;">{{ s.plannedDate }}</td>
              <td>
                <div style="font-weight: 600;">{{ s.inventoryNumber }}</div>
                <div style="font-size: 11px; color: #94a3b8;">{{ s.equipmentName }}</div>
              </td>
              <td>{{ s.workshopName }}</td>
              <td>
                <span class="badge badge-info">{{ s.typeCode }}</span>
                <span style="font-size: 12px; margin-left: 6px;">{{ s.typeName }}</span>
              </td>
              <td>{{ s.teamName || '—' }}</td>
              <td>
                <span class="badge" :class="s.scheduleStatus === 'completed' ? 'badge-success' : s.scheduleStatus === 'in_progress' ? 'badge-warning' : 'badge-info'">
                  {{ s.scheduleStatus === 'completed' ? 'Выполнено' : s.scheduleStatus === 'in_progress' ? 'Заявка сформирована' : 'Запланировано' }}
                </span>
              </td>
              <td>
                <button
                  v-if="s.scheduleStatus === 'planned'"
                  class="btn btn-primary btn-sm"
                  @click="handleGenerateOrder(s.scheduleId)"
                  title="Автоматически сформировать заявку на ремонт из графика ТО"
                >
                  ⚡ Сформировать заявку
                </button>
                <span v-else style="font-size: 12px; color: #34d399;">✓ В работе</span>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Модалка планирования -->
    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal-content">
        <div class="modal-header">
          <div class="modal-title">Запланировать ТО</div>
          <button class="modal-close" @click="showModal = false">✕</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label class="form-label">Оборудование *</label>
            <select v-model="form.equipmentId" class="form-select">
              <option v-for="eq in equipmentList" :key="eq.equipmentId" :value="eq.equipmentId">
                {{ eq.inventoryNumber }} - {{ eq.equipmentName }} ({{ eq.workshopName }})
              </option>
            </select>
          </div>
          <div class="form-group">
            <label class="form-label">Вид регламентного обслуживания *</label>
            <select v-model="form.maintenanceTypeId" class="form-select">
              <option v-for="m in maintenanceTypes" :key="m.maintenanceTypeId" :value="m.maintenanceTypeId">
                {{ m.typeCode }} - {{ m.typeName }} (периодичность: {{ m.periodicityDays }} дн., норматив: {{ m.standardDurationHours }} ч.)
              </option>
            </select>
          </div>
          <div class="form-group">
            <label class="form-label">Назначенная бригада</label>
            <select v-model="form.assignedTeamId" class="form-select">
              <option v-for="t in teams" :key="t.teamId" :value="t.teamId">{{ t.teamName }} ({{ t.specialization }})</option>
            </select>
          </div>
          <div class="form-group">
            <label class="form-label">Плановая дата проведения *</label>
            <input v-model="form.plannedDate" type="date" class="form-input" />
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="showModal = false">Отмена</button>
          <button class="btn btn-primary" @click="submitCreateSchedule">Запланировать</button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; flex-wrap: wrap; gap: 14px; }
.page-title { font-size: 22px; font-weight: 800; color: #f8fafc; }
.page-sub { font-size: 13px; color: #94a3b8; }
</style>
