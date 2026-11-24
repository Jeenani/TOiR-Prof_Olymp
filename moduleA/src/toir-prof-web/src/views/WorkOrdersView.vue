<script setup>
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { api } from '../services/api';
import { useToastStore } from '../stores/toastStore';

const route = useRoute();
const toast = useToastStore();

const orders = ref([]);
const totalOrders = ref(0);
const page = ref(1);
const pageSize = ref(25);
const totalPages = ref(1);
const isLoading = ref(false);

const filterStatus = ref(0);
const filterPriority = ref(0);
const filterWorkshop = ref(0);
const searchQuery = ref(route.query.search || '');
const sortBy = ref('createdAt');
const sortDesc = ref(true);

const statuses = ref([]);
const priorities = ref([]);
const workshops = ref([]);
const equipmentList = ref([]);
const teams = ref([]);
const materialsList = ref([]);

const showCreateModal = ref(false);
const showDetailModal = ref(false);
const selectedOrder = ref(null);
const isLoadingDetail = ref(false);

const createForm = ref({
  orderType: 'emergency', priorityId: 1, equipmentId: null,
  description: '', failureReason: '',
  plannedStartDate: new Date().toISOString().slice(0, 16),
  plannedEndDate: new Date(Date.now() + 4 * 3600000).toISOString().slice(0, 16)
});
const createErrors = ref({});

const assignTeamId = ref(null);
const updateStatusId = ref(null);
const closeReportText = ref('');
const closeDowntimeHours = ref(null);
const selectedMaterialId = ref(null);
const materialQuantity = ref(1);

async function loadLookups() {
  try {
    const [st, pr, ws, tm, mats] = await Promise.all([
      api.get('/work-orders/statuses'), api.get('/work-orders/priorities'),
      api.get('/auth/workshops'), api.get('/auth/teams'), api.get('/materials')
    ]);
    statuses.value = st; priorities.value = pr; workshops.value = ws;
    teams.value = tm; materialsList.value = mats;
  } catch (e) {
    console.error('Ошибка справочников:', e);
  }
}

async function loadOrders() {
  isLoading.value = true;
  try {
    const params = {
      page: page.value, pageSize: pageSize.value,
      statusId: filterStatus.value || undefined,
      priorityId: filterPriority.value || undefined,
      workshopId: filterWorkshop.value || undefined,
      search: searchQuery.value || undefined,
      sortBy: sortBy.value, sortDesc: sortDesc.value
    };
    const res = await api.get('/work-orders', params);
    orders.value = res.items || [];
    totalOrders.value = res.total || 0;
    totalPages.value = res.totalPages || 1;
  } catch (e) {
    toast.error('Не удалось загрузить заявки');
  } finally {
    isLoading.value = false;
  }
}

function handleSort(col) {
  if (sortBy.value === col) sortDesc.value = !sortDesc.value;
  else { sortBy.value = col; sortDesc.value = false; }
  loadOrders();
}

async function openCreateModal() {
  createErrors.value = {};
  if (!equipmentList.value.length) {
    const res = await api.get('/equipment', { pageSize: 100 });
    equipmentList.value = res.items || [];
  }
  createForm.value = {
    orderType: 'emergency', priorityId: 1, equipmentId: equipmentList.value[0]?.equipmentId || null,
    description: '', failureReason: '',
    plannedStartDate: new Date().toISOString().slice(0, 16),
    plannedEndDate: new Date(Date.now() + 4 * 3600000).toISOString().slice(0, 16)
  };
  showCreateModal.value = true;
}

async function submitCreateOrder() {
  createErrors.value = {};
  if (!createForm.value.equipmentId) createErrors.value.equipmentId = 'Выберите оборудование';
  if (!createForm.value.description?.trim()) createErrors.value.description = 'Укажите описание дефекта';
  if (new Date(createForm.value.plannedEndDate) < new Date(createForm.value.plannedStartDate)) {
    createErrors.value.plannedEndDate = 'Дата окончания не может быть раньше начала';
  }
  if (Object.keys(createErrors.value).length > 0) return;

  try {
    const res = await api.post('/work-orders', createForm.value);
    toast.success(`Заявка ${res.orderNumber} создана!`);
    showCreateModal.value = false;
    loadOrders();
  } catch (err) { toast.error(err.message || 'Ошибка создания'); }
}

async function openDetailModal(id) {
  isLoadingDetail.value = true;
  showDetailModal.value = true;
  try {
    const data = await api.get(`/work-orders/${id}`);
    selectedOrder.value = data;
    assignTeamId.value = data.assignedTeamId || null;
    updateStatusId.value = data.orderStatusId;
    closeReportText.value = data.reportText || '';
    closeDowntimeHours.value = data.downtimeHours || null;
  } catch (e) {
    toast.error('Не удалось открыть карточку заявки');
    showDetailModal.value = false;
  } finally { isLoadingDetail.value = false; }
}

async function handleAssignTeam() {
  if (!assignTeamId.value) return;
  try {
    await api.put(`/work-orders/${selectedOrder.value.workOrderId}/assign-team`, { teamId: assignTeamId.value });
    toast.success('Бригада назначена');
    openDetailModal(selectedOrder.value.workOrderId);
    loadOrders();
  } catch (e) { toast.error(e.message); }
}

async function handleUpdateStatus() {
  try {
    await api.put(`/work-orders/${selectedOrder.value.workOrderId}/status`, {
      orderStatusId: updateStatusId.value,
      reportText: closeReportText.value,
      downtimeHours: closeDowntimeHours.value
    });
    toast.success('Статус заявки обновлен');
    openDetailModal(selectedOrder.value.workOrderId);
    loadOrders();
  } catch (e) { toast.error(e.message); }
}

async function handleAddMaterial() {
  if (!selectedMaterialId.value || materialQuantity.value <= 0) return;
  try {
    await api.post(`/work-orders/${selectedOrder.value.workOrderId}/materials`, {
      materialId: selectedMaterialId.value, quantityUsed: materialQuantity.value
    });
    toast.success('Материал добавлен к заявке');
    selectedMaterialId.value = null;
    openDetailModal(selectedOrder.value.workOrderId);
  } catch (e) { toast.error(e.message); }
}

onMounted(() => { loadLookups(); loadOrders(); });
</script>

<template>
  <div class="orders-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">Реестр заявок на ремонт</h1>
        <p class="page-sub">Управление аварийными и плановыми работами ТОиР</p>
      </div>
      <button class="btn btn-primary" @click="openCreateModal">+ Новая заявка</button>
    </div>

    <!-- Фильтры (R159) -->
    <div class="filter-toolbar">
      <div class="filter-item">
        <label>Цех:</label>
        <select v-model="filterWorkshop" @change="page = 1; loadOrders()" class="form-select">
          <option :value="0">Все цеха</option>
          <option v-for="w in workshops" :key="w.workshopId" :value="w.workshopId">{{ w.workshopName }}</option>
        </select>
      </div>
      <div class="filter-item">
        <label>Статус:</label>
        <select v-model="filterStatus" @change="page = 1; loadOrders()" class="form-select">
          <option :value="0">Все статусы</option>
          <option v-for="s in statuses" :key="s.orderStatusId" :value="s.orderStatusId">{{ s.statusName }}</option>
        </select>
      </div>
      <div class="filter-item" style="flex: 1; min-width: 200px;">
        <input v-model="searchQuery" @input="page = 1; loadOrders()" type="text" placeholder="Поиск по номеру, оборудованию..." class="form-input" />
      </div>
      <button class="btn btn-secondary btn-sm" @click="filterStatus = 0; filterPriority = 0; filterWorkshop = 0; searchQuery = ''; loadOrders()">Сброс</button>
    </div>

    <!-- Таблица (R139) -->
    <div class="card">
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th @click="handleSort('orderNumber')">№ Заявки {{ sortBy === 'orderNumber' ? (sortDesc ? '▼' : '▲') : '' }}</th>
              <th>Тип</th>
              <th>Оборудование</th>
              <th>Цех</th>
              <th>Бригада</th>
              <th @click="handleSort('status')">Статус</th>
              <th>Плановые даты</th>
              <th>Простой</th>
              <th>Действия</th>
            </tr>
          </thead>
          <tbody>
            <tr v-if="isLoading"><td colspan="9" style="text-align: center; padding: 25px;">Загрузка данных...</td></tr>
            <tr v-else-if="!orders.length"><td colspan="9" style="text-align: center; padding: 25px; color: #94a3b8;">Заявки не найдены</td></tr>
            <tr v-for="wo in orders" :key="wo.workOrderId">
              <td><span class="order-num" @click="openDetailModal(wo.workOrderId)">{{ wo.orderNumber }}</span></td>
              <td>
                <span class="type-tag" :class="wo.orderType === 'emergency' ? 'tag-red' : 'tag-blue'">
                  {{ wo.orderType === 'emergency' ? 'Аварийный' : 'Плановый' }}
                </span>
              </td>
              <td>
                <div style="font-weight: 600;">{{ wo.inventoryNumber }}</div>
                <div style="font-size: 11px; color: #94a3b8;">{{ wo.equipmentName }}</div>
              </td>
              <td>{{ wo.workshopName }}</td>
              <td>{{ wo.teamName || '—' }}</td>
              <td>
                <span class="badge" :class="wo.orderStatusId === 5 ? 'badge-success' : wo.orderStatusId === 3 ? 'badge-warning' : 'badge-info'">
                  {{ wo.statusName }}
                </span>
              </td>
              <td style="font-size: 11px;">
                <div>С: {{ wo.plannedStartDate.slice(0, 16).replace('T', ' ') }}</div>
                <div>По: {{ wo.plannedEndDate.slice(0, 16).replace('T', ' ') }}</div>
              </td>
              <td>{{ wo.downtimeHours }} ч.</td>
              <td><button class="btn btn-secondary btn-sm" @click="openDetailModal(wo.workOrderId)">Детали</button></td>
            </tr>
          </tbody>
        </table>
      </div>

      <div class="pagination-bar">
        <div>Показано {{ orders.length }} из {{ totalOrders }} заявок</div>
        <div class="pagination-controls">
          <label>Строк:</label>
          <select v-model="pageSize" @change="page = 1; loadOrders()" class="form-select form-select-sm" style="width: 70px;">
            <option :value="25">25</option>
            <option :value="50">50</option>
            <option :value="100">100</option>
          </select>
          <button class="page-btn" :disabled="page <= 1" @click="page--; loadOrders()">‹</button>
          <span>Стр. {{ page }} из {{ totalPages }}</span>
          <button class="page-btn" :disabled="page >= totalPages" @click="page++; loadOrders()">›</button>
        </div>
      </div>
    </div>

    <!-- Модалка Создания -->
    <div v-if="showCreateModal" class="modal-overlay" @click.self="showCreateModal = false">
      <div class="modal-content">
        <div class="modal-header">
          <div class="modal-title">Создание заявки на ремонт</div>
          <button class="modal-close" @click="showCreateModal = false">✕</button>
        </div>
        <div class="modal-body">
          <div class="form-group">
            <label class="form-label">Тип ремонта</label>
            <select v-model="createForm.orderType" class="form-select">
              <option value="emergency">Аварийный (останов оборудования)</option>
              <option value="planned">Плановый</option>
            </select>
          </div>
          <div class="form-group">
            <label class="form-label">Оборудование *</label>
            <select v-model="createForm.equipmentId" class="form-select" :class="{ 'is-invalid': createErrors.equipmentId }">
              <option v-for="eq in equipmentList" :key="eq.equipmentId" :value="eq.equipmentId">
                {{ eq.inventoryNumber }} - {{ eq.equipmentName }} ({{ eq.workshopName }})
              </option>
            </select>
            <div v-if="createErrors.equipmentId" class="invalid-feedback">{{ createErrors.equipmentId }}</div>
          </div>
          <div class="form-group">
            <label class="form-label">Описание дефекта *</label>
            <textarea v-model="createForm.description" class="form-textarea" :class="{ 'is-invalid': createErrors.description }" rows="3"></textarea>
            <div v-if="createErrors.description" class="invalid-feedback">{{ createErrors.description }}</div>
          </div>
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px;">
            <div class="form-group">
              <label class="form-label">Плановое начало</label>
              <input v-model="createForm.plannedStartDate" type="datetime-local" class="form-input" />
            </div>
            <div class="form-group">
              <label class="form-label">Плановое завершение</label>
              <input v-model="createForm.plannedEndDate" type="datetime-local" class="form-input" />
            </div>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="showCreateModal = false">Отмена</button>
          <button class="btn btn-primary" @click="submitCreateOrder">Создать заявку</button>
        </div>
      </div>
    </div>

    <!-- Модалка Деталей (R159) -->
    <div v-if="showDetailModal" class="modal-overlay" @click.self="showDetailModal = false">
      <div class="modal-content" style="max-width: 800px;">
        <div class="modal-header">
          <div class="modal-title" v-if="selectedOrder">
            Заявка {{ selectedOrder.orderNumber }}
            <span class="badge" :class="selectedOrder.orderStatusId === 5 ? 'badge-success' : 'badge-warning'" style="margin-left: 8px;">
              {{ selectedOrder.statusName }}
            </span>
          </div>
          <button class="modal-close" @click="showDetailModal = false">✕</button>
        </div>
        <div v-if="isLoadingDetail" class="modal-body" style="text-align: center; padding: 30px;">Загрузка...</div>
        <div v-else-if="selectedOrder" class="modal-body">
          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px; margin-bottom: 16px;">
            <div><div class="detail-label">Оборудование:</div><div class="detail-val">{{ selectedOrder.equipmentName }} ({{ selectedOrder.inventoryNumber }})</div></div>
            <div><div class="detail-label">Цех:</div><div class="detail-val">{{ selectedOrder.workshopName }}</div></div>
            <div><div class="detail-label">Мастер:</div><div class="detail-val">{{ selectedOrder.masterName }}</div></div>
            <div><div class="detail-label">Простой:</div><div class="detail-val" style="color: #f87171;">{{ selectedOrder.downtimeHours }} ч.</div></div>
          </div>
          <div class="detail-box" style="margin-bottom: 16px;">
            <div class="detail-label">Дефект:</div><div>{{ selectedOrder.description }}</div>
          </div>

          <div style="display: grid; grid-template-columns: 1fr 1fr; gap: 14px; background: #0f172a; padding: 14px; border-radius: 8px; margin-bottom: 16px;">
            <div>
              <label class="form-label">Бригада</label>
              <div style="display: flex; gap: 6px;">
                <select v-model="assignTeamId" class="form-select">
                  <option v-for="t in teams" :key="t.teamId" :value="t.teamId">{{ t.teamName }}</option>
                </select>
                <button class="btn btn-secondary btn-sm" @click="handleAssignTeam">Ок</button>
              </div>
            </div>
            <div>
              <label class="form-label">Статус</label>
              <div style="display: flex; gap: 6px;">
                <select v-model="updateStatusId" class="form-select">
                  <option v-for="s in statuses" :key="s.orderStatusId" :value="s.orderStatusId">{{ s.statusName }}</option>
                </select>
                <button class="btn btn-primary btn-sm" @click="handleUpdateStatus">Сохранить</button>
              </div>
            </div>
          </div>

          <div v-if="updateStatusId === 5" style="margin-bottom: 16px;">
            <div class="form-group">
              <label class="form-label">Отчет о выполнении</label>
              <textarea v-model="closeReportText" class="form-textarea" rows="2" placeholder="Работы завершены, проведен пуск-наладка..."></textarea>
            </div>
            <div class="form-group">
              <label class="form-label">Фактический простой (часов)</label>
              <input v-model.number="closeDowntimeHours" type="number" step="0.5" class="form-input" style="max-width: 140px;" />
            </div>
          </div>

          <div>
            <div style="font-weight: 700; margin-bottom: 8px; font-size: 13px;">Материалы и запчасти</div>
            <table class="data-table" style="margin-bottom: 10px;">
              <thead><tr><th>Материал</th><th>Кол-во</th><th>Цена</th><th>Сумма</th></tr></thead>
              <tbody>
                <tr v-if="!selectedOrder.materials.length"><td colspan="4" style="text-align: center; color: #94a3b8;">Нет списанных материалов</td></tr>
                <tr v-for="m in selectedOrder.materials" :key="m.materialId">
                  <td>{{ m.materialName }}</td><td>{{ m.quantityUsed }} {{ m.unitOfMeasure }}</td>
                  <td>{{ m.historicalUnitPrice }} ₽</td><td>{{ m.totalCost }} ₽</td>
                </tr>
              </tbody>
            </table>
            <div style="display: flex; gap: 8px; background: #0f172a; padding: 10px; border-radius: 8px;">
              <select v-model="selectedMaterialId" class="form-select" style="flex: 1;">
                <option :value="null">-- Выбрать со склада --</option>
                <option v-for="mat in materialsList" :key="mat.materialId" :value="mat.materialId">{{ mat.materialName }} ({{ mat.currentStock }})</option>
              </select>
              <input v-model.number="materialQuantity" type="number" min="1" class="form-input" style="width: 70px;" />
              <button class="btn btn-secondary btn-sm" @click="handleAddMaterial">+ Списать</button>
            </div>
          </div>
        </div>
        <div class="modal-footer"><button class="btn btn-secondary" @click="showDetailModal = false">Закрыть</button></div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; flex-wrap: wrap; gap: 14px; }
.page-title { font-size: 22px; font-weight: 800; color: #f8fafc; }
.page-sub { font-size: 13px; color: #94a3b8; }
.order-num { color: #38bdf8; font-weight: 700; cursor: pointer; }
.order-num:hover { text-decoration: underline; }
.type-tag { font-size: 11px; padding: 2px 6px; border-radius: 4px; font-weight: 600; }
.tag-red { background: rgba(239, 68, 68, 0.2); color: #f87171; }
.tag-blue { background: rgba(56, 189, 248, 0.2); color: #38bdf8; }
.detail-label { font-size: 11px; color: #94a3b8; text-transform: uppercase; font-weight: 600; }
.detail-val { font-size: 14px; font-weight: 600; color: #f8fafc; }
.detail-box { background: #0f172a; padding: 10px; border-radius: 6px; font-size: 13px; color: #e2e8f0; }
</style>
