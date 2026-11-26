<script setup>
import { ref, onMounted } from 'vue';
import { api } from '../services/api';
import { useToastStore } from '../stores/toastStore';

const toast = useToastStore();
const activeTab = ref('materials'); // 'materials' | 'teams' | 'types' | 'workshops'

const materials = ref([]);
const teams = ref([]);
const types = ref([]);
const workshops = ref([]);
const isLoading = ref(false);

const adjustModal = ref(false);
const selectedMaterial = ref(null);
const adjustDelta = ref(0);

async function loadData() {
  isLoading.value = true;
  try {
    const [m, t, ty, w] = await Promise.all([
      api.get('/materials'),
      api.get('/auth/teams'),
      api.get('/schedules/types'),
      api.get('/auth/workshops')
    ]);
    materials.value = m || [];
    teams.value = t || [];
    types.value = ty || [];
    workshops.value = w || [];
  } catch (e) {
    toast.error('Не удалось загрузить справочники НСИ');
  } finally {
    isLoading.value = false;
  }
}

function openAdjustModal(m) {
  selectedMaterial.value = m;
  adjustDelta.value = 0;
  adjustModal.value = true;
}

async function submitAdjust() {
  if (!adjustDelta.value) return;
  try {
    const res = await api.post(`/materials/${selectedMaterial.value.materialId}/adjust`, {
      warehouseId: selectedMaterial.value.warehouseId,
      quantityDelta: adjustDelta.value
    });
    toast.success(res.message);
    adjustModal.value = false;
    loadData();
  } catch (e) {
    toast.error(e.message || 'Ошибка корректировки');
  }
}

onMounted(() => { loadData(); });
</script>

<template>
  <div class="catalogs-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">Справочники НСИ</h1>
        <p class="page-sub">Нормативно-справочная информация: материалы, бригады, виды ТО и цеха предприятия</p>
      </div>
    </div>

    <!-- Вкладки (R162) -->
    <div style="display: flex; gap: 8px; margin-bottom: 20px; border-bottom: 1px solid var(--border); padding-bottom: 8px;">
      <button class="btn btn-sm" :class="activeTab === 'materials' ? 'btn-primary' : 'btn-secondary'" @click="activeTab = 'materials'">
        📦 Материалы и запчасти ({{ materials.length }})
      </button>
      <button class="btn btn-sm" :class="activeTab === 'teams' ? 'btn-primary' : 'btn-secondary'" @click="activeTab = 'teams'">
        🛠 Ремонтные бригады ({{ teams.length }})
      </button>
      <button class="btn btn-sm" :class="activeTab === 'types' ? 'btn-primary' : 'btn-secondary'" @click="activeTab = 'types'">
        📋 Виды регламентов ТО ({{ types.length }})
      </button>
      <button class="btn btn-sm" :class="activeTab === 'workshops' ? 'btn-primary' : 'btn-secondary'" @click="activeTab = 'workshops'">
        🏭 Производственные цеха ({{ workshops.length }})
      </button>
    </div>

    <!-- 1. Материалы -->
    <div v-if="activeTab === 'materials'" class="card">
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th>Артикул</th>
              <th>Наименование</th>
              <th>Склад</th>
              <th>Остаток</th>
              <th>Мин. норма</th>
              <th>Ячейка</th>
              <th>Цена за ед.</th>
              <th>Статус запаса</th>
              <th>Действие</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="m in materials" :key="m.materialId">
              <td style="font-weight: 700; color: #38bdf8;">{{ m.materialCode }}</td>
              <td>{{ m.materialName }}</td>
              <td>{{ m.warehouseName }}</td>
              <td style="font-weight: 700;">{{ m.currentStock }} {{ m.unitOfMeasure }}</td>
              <td>{{ m.minimumStock }} {{ m.unitOfMeasure }}</td>
              <td>{{ m.storageCell || '—' }}</td>
              <td>{{ m.unitPrice }} ₽</td>
              <td>
                <span class="badge" :class="m.isLowStock ? 'badge-danger' : 'badge-success'">
                  {{ m.isLowStock ? 'Критический остаток' : 'В норме' }}
                </span>
              </td>
              <td>
                <button class="btn btn-secondary btn-sm" @click="openAdjustModal(m)">
                  Корректировка
                </button>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 2. Бригады -->
    <div v-if="activeTab === 'teams'" class="card">
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th>Шифр</th>
              <th>Наименование бригады</th>
              <th>Специализация</th>
              <th>Цех приписки</th>
              <th>Состав бригады</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="t in teams" :key="t.teamId">
              <td style="font-weight: 700; color: #38bdf8;">{{ t.teamCode }}</td>
              <td style="font-weight: 600;">{{ t.teamName }}</td>
              <td><span class="badge badge-info">{{ t.specialization }}</span></td>
              <td>{{ t.workshopName || 'Общезаводская' }}</td>
              <td>
                <div v-for="m in t.members" :key="m.userId" style="font-size: 12px; margin-bottom: 2px;">
                  • {{ m.fullName }} <span v-if="m.isTeamLeader" style="color: #38bdf8; font-weight: 700;">(Бригадир)</span>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 3. Виды регламентов ТО -->
    <div v-if="activeTab === 'types'" class="card">
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th>Код</th>
              <th>Наименование регламента</th>
              <th>Периодичность</th>
              <th>Норматив (час)</th>
              <th>Останов станка</th>
              <th>Описание регламента</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="ty in types" :key="ty.maintenanceTypeId">
              <td style="font-weight: 700; color: #38bdf8;">{{ ty.typeCode }}</td>
              <td style="font-weight: 600;">{{ ty.typeName }}</td>
              <td>{{ ty.periodicityDays }} дней</td>
              <td>{{ ty.standardDurationHours }} ч.</td>
              <td>
                <span class="badge" :class="ty.requiresStop ? 'badge-warning' : 'badge-success'">
                  {{ ty.requiresStop ? 'Требуется останов' : 'На ходу' }}
                </span>
              </td>
              <td>{{ ty.description }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- 4. Цеха -->
    <div v-if="activeTab === 'workshops'" class="card">
      <div class="table-wrapper">
        <table class="data-table">
          <thead>
            <tr>
              <th>Код цеха</th>
              <th>Наименование производственного цеха</th>
              <th>Описание подразделения</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="w in workshops" :key="w.workshopId">
              <td style="font-weight: 700; color: #38bdf8;">{{ w.workshopCode }}</td>
              <td style="font-weight: 600;">{{ w.workshopName }}</td>
              <td>{{ w.workshopDescription }}</td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Модалка Корректировки склада -->
    <div v-if="adjustModal && selectedMaterial" class="modal-overlay" @click.self="adjustModal = false">
      <div class="modal-content" style="max-width: 460px;">
        <div class="modal-header">
          <div class="modal-title">Складская корректировка</div>
          <button class="modal-close" @click="adjustModal = false">✕</button>
        </div>
        <div class="modal-body">
          <div style="margin-bottom: 14px;">
            <div style="font-weight: 700;">{{ selectedMaterial.materialName }}</div>
            <div style="font-size: 12px; color: #94a3b8;">Текущий остаток: {{ selectedMaterial.currentStock }} {{ selectedMaterial.unitOfMeasure }}</div>
          </div>
          <div class="form-group">
            <label class="form-label">Изменение количества (+ приход, - расход)</label>
            <input v-model.number="adjustDelta" type="number" step="1" class="form-input" />
          </div>
          <div style="font-size: 13px;">
            Новый остаток будет: <b>{{ selectedMaterial.currentStock + adjustDelta }} {{ selectedMaterial.unitOfMeasure }}</b>
          </div>
        </div>
        <div class="modal-footer">
          <button class="btn btn-secondary" @click="adjustModal = false">Отмена</button>
          <button class="btn btn-primary" @click="submitAdjust">Применить</button>
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
