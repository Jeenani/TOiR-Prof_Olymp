<script setup>
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { api } from '../services/api';
import { useToastStore } from '../stores/toastStore';

const route = useRoute();
const toast = useToastStore();

const equipment = ref([]);
const total = ref(0);
const page = ref(1);
const pageSize = ref(25);
const totalPages = ref(1);
const isLoading = ref(false);

const filterWorkshop = ref(0);
const filterStatus = ref(0);
const searchQuery = ref(route.query.search || '');
const sortBy = ref('inventoryNumber');
const sortDesc = ref(false);

const workshops = ref([]);
const statuses = ref([]);
const showModal = ref(false);
const showDetail = ref(false);
const selectedItem = ref(null);

const form = ref({
  workshopId: 1, statusId: 1, inventoryNumber: '', equipmentName: '',
  modelName: '', serialNumber: '', manufacturer: '',
  commissioningDate: new Date().toISOString().slice(0, 10),
  operationDate: new Date().toISOString().slice(0, 10)
});
const errors = ref({});

async function loadData() {
  isLoading.value = true;
  try {
    const [res, ws, st] = await Promise.all([
      api.get('/equipment', {
        page: page.value, pageSize: pageSize.value,
        workshopId: filterWorkshop.value || undefined,
        statusId: filterStatus.value || undefined,
        search: searchQuery.value || undefined,
        sortBy: sortBy.value, sortDesc: sortDesc.value
      }),
      workshops.value.length ? Promise.resolve(workshops.value) : api.get('/auth/workshops'),
      statuses.value.length ? Promise.resolve(statuses.value) : api.get('/equipment/statuses')
    ]);
    equipment.value = res.items || [];
    total.value = res.total || 0;
    totalPages.value = res.totalPages || 1;
    if (!workshops.value.length) workshops.value = ws;
    if (!statuses.value.length) statuses.value = st;
  } catch (e) {
    toast.error('Ошибка загрузки оборудования');
  } finally { isLoading.value = false; }
}

async function openDetail(id) {
  try {
    selectedItem.value = await api.get(`/equipment/${id}`);
    showDetail.value = true;
  } catch (e) { toast.error('Не удалось загрузить паспорт'); }
}

async function submitCreate() {
  errors.value = {};
  if (!form.value.inventoryNumber?.trim()) errors.value.inventoryNumber = 'Укажите инв. номер';
  if (!form.value.equipmentName?.trim()) errors.value.equipmentName = 'Укажите наименование';
  if (Object.keys(errors.value).length > 0) return;

  try {
    await api.post('/equipment', form.value);
    toast.success('Оборудование успешно зарегистрировано!');
    showModal.value = false;
    loadData();
  } catch (err) { toast.error(err.message || 'Ошибка создания'); }
}

onMounted(() => { loadData(); });
</script>

<template>
  <div class="equipment-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">Реестр оборудования</h1>
        <p class="page-sub">Паспорта, техническое состояние и история эксплуатации станков</p>
      </div>
      <button class="btn btn-primary" @click="showModal = true">+ Новая единица</button>
    </div>

    <div class="filter-toolbar">
      <div class="filter-item">
        <label>Цех:</label>
        <select v-model="filterWorkshop" @change="page = 1; loadData()" class="form-select">
          <option :value="0">Все цеха</option>
          <option v-for="w in workshops" :key="w.workshopId" :value="w.workshopId">{{ w.workshopName }}</option>
        </select>
      </div>
      <div class="filter-item">
        <label>Статус:</label>
        <select v-model="filterStatus" @change="page = 1; loadData()" class="form-select">
          <option :value="0">Все статусы</option>
          <option v-for="s in statuses" :key="s.statusId" :value="s.statusId">{{ s.statusName }}</option>
        </select>
      </div>
      <div class="filter-item" style="flex: 1; min-width: 200px;">
        <input v-model="searchQuery" @input="page = 1; loadData()" type="text" placeholder="Поиск по инв. номеру, названию, модели..." class="form-input" />
      </div>
      <button class="btn btn-secondary btn-sm" @click="filterWorkshop = 0; filterStatus = 0; searchQuery = ''; loadData()">Сброс</button>
    </div>

    <div class="card">
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th>Инв. №</th>
              <th>Наименование</th>
              <th>Модель</th>
              <th>Цех</th>
              <th>Статус</th>
              <th>Ввод в экспл.</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="isLoading"><td colspan="7" style="text-align: center; padding: 20px;">Загрузка...</td></tr>
            <tr v-else-if="!equipment.length"><td colspan="7" style="text-align: center; padding: 20px; color: #94a3b8;">Оборудование не найдено</td></tr>
            <tr v-for="eq in equipment" :key="eq.equipmentId">
              <td style="font-weight: 700; color: #38bdf8; cursor: pointer;" @click="openDetail(eq.equipmentId)">{{ eq.inventoryNumber }}</td>
              <td>{{ eq.equipmentName }}</td>
              <td>{{ eq.modelName }}</td>
              <td>{{ eq.workshopName }}</td>
              <td>
                <span class="badge" :class="eq.statusId === 1 ? 'badge-success' : eq.statusId === 2 ? 'badge-warning' : 'badge-danger'">
                  {{ eq.statusName }}
                </span>
              </td>
              <td>{{ eq.commissioningDate }}</td>
              <td><button class="btn btn-secondary btn-sm" @click="openDetail(eq.equipmentId)">Паспорт</button></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pagination-bar">
        <div>Всего: {{ total }} единиц оборудования</div>
        <div class="pagination-controls">
          <button class="page-btn" :disabled="page <= 1" @click="page--; loadData()">‹</button>
          <span>Стр. {{ page }} из {{ totalPages }}</span>
          <button class="page-btn" :disabled="page >= totalPages" @click="page++; loadData()">›</button>
        </div>
      </div>
    </div>

    <!-- Модалка Создания -->
    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal-content">
        <div class="modal-header">
          <div class="modal-title">Добавление единицы оборудования</div>
          <button class="modal-close" @click="showModal = false">✕</button>
        </div>
        <div class="modal-body">
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px;">
            <div class="form-group">
              <label class="form-label">Инвентарный № *</label>
              <input v-model="form.inventoryNumber" type="text" class="form-input" :class="{ 'is-invalid': errors.inventoryNumber }" placeholder="EQ-..." />
              <div v-if="errors.inventoryNumber" class="invalid-feedback">{{ errors.inventoryNumber }}</div>
            </div>
            <div class="form-group">
              <label class="form-label">Цех размещения *</label>
              <select v-model="form.workshopId" class="form-select">
                <option v-for="w in workshops" :key="w.workshopId" :value="w.workshopId">{{ w.workshopName }}</option>
              </select>
            </div>
          </div>
          <div class="form-group">
            <label class="form-label">Наименование оборудования *</label>
            <input v-model="form.equipmentName" type="text" class="form-input" :class="{ 'is-invalid': errors.equipmentName }" placeholder="Токарно-винторезный станок..." />
          </div>
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px;">
            <div class="form-group">
              <label class="form-label">Модель</label>
              <input v-model="form.modelName" type="text" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label">Серийный номер</label>
              <input v-model="form.serialNumber" type="text" class="form-input" />
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="showModal = false">Отмена</button>
          <button class="btn btn-primary" @click="submitCreate">Сохранить</button>
        </div>
      </div>
    </div>

    <!-- Модалка Карточки / Паспорта (R160) -->
    <div v-if="showDetail && selectedItem" class="modal-overlay" @click.self="showDetail = false">
      <div class="modal-content" style="max-width: 800px;">
        <div class="modal-header">
          <div class="modal-title">Паспорт оборудования {{ selectedItem.inventoryNumber }}</div>
          <button class="modal-close" @click="showDetail = false">✕</button>
        </div>
        <div class="modal-body">
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-bottom: 16px;">
            <div><div class="detail-label">Наименование:</div><div class="detail-val">{{ selectedItem.equipmentName }}</div></div>
            <div><div class="detail-label">Модель / Завод:</div><div class="detail-val">{{ selectedItem.modelName }} ({{ selectedItem.manufacturer || 'РФ' }})</div></div>
            <div><div class="detail-label">Цех:</div><div class="detail-val">{{ selectedItem.workshopName }}</div></div>
            <div><div class="detail-label">Статус:</div><div class="detail-val">{{ selectedItem.statusName }}</div></div>
          </div>

          <div style="font-weight: 700; margin-bottom: 8px; font-size: 13px;">История изменения статусов</div>
          <table class="data-table" style="margin-bottom: 16px;">
            <thead><tr><th>Дата</th><th>Статус</th><th>Причина</th></tr></thead>
            <tbody>
              <tr v-if="!selectedItem.statusHistory?.length"><td colspan="3" style="text-align: center; color: #94a3b8;">История пуста</td></tr>
              <tr v-for="h in selectedItem.statusHistory" :key="h.historyId">
                <td>{{ h.changedAt?.slice(0, 16).replace('T', ' ') }}</td>
                <td>{{ h.statusName }}</td>
                <td>{{ h.reason || 'Плановое изменение' }}</td>
              </tr>
            </tbody>
          </table>

          <div style="font-weight: 700; margin-bottom: 8px; font-size: 13px;">График технического обслуживания</div>
          <table class="data-table">
            <thead><tr><th>Вид ТО</th><th>Плановая дата</th><th>Бригада</th><th>Статус</th></tr></thead>
            <tbody>
              <tr v-if="!selectedItem.schedules?.length"><td colspan="4" style="text-align: center; color: #94a3b8;">Нет запланированных ТО</td></tr>
              <tr v-for="sc in selectedItem.schedules" :key="sc.scheduleId">
                <td>{{ sc.typeCode }} ({{ sc.typeName }})</td>
                <td>{{ sc.plannedDate }}</td>
                <td>{{ sc.teamName || 'Не назначена' }}</td>
                <td>{{ sc.scheduleStatus === 'planned' ? 'Запланировано' : 'В работе' }}</td>
              </tr>
            </tbody>
          </table>
        </div>
        <div class="modal-footer"><button class="btn btn-secondary" @click="showDetail = false">Закрыть</button></div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; flex-wrap: wrap; gap: 14px; }
.page-title { font-size: 22px; font-weight: 800; color: #f8fafc; }
.page-sub { font-size: 13px; color: #94a3b8; }
.detail-label { font-size: 11px; color: #94a3b8; text-transform: uppercase; font-weight: 600; }
.detail-val { font-size: 14px; font-weight: 600; color: #f8fafc; }
</style>
