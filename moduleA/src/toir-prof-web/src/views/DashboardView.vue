<script setup>
import { ref, onMounted, onUnmounted, nextTick } from 'vue';
import { useRouter } from 'vue-router';
import { api } from '../services/api';
import { exportChartToPNG } from '../services/export';
import { drawChart, orderBadge } from '../services/ui';
import { useToastStore } from '../stores/toastStore';

const router = useRouter();
const toast = useToastStore();

const kpi = ref(null);
const dynamics = ref([]);
const workshops = ref([]);
const selectedWorkshop = ref(0);
const isLoading = ref(true);
const autoRefresh = ref(true);

let refreshTimer = null;
let charts = {};

async function loadData() {
  isLoading.value = true;
  try {
    const params = selectedWorkshop.value > 0 ? { workshopId: selectedWorkshop.value } : {};
    const [kpiData, detData, wsList] = await Promise.all([
      api.get('/analytics/dashboard', params),
      api.get('/analytics/details', params),
      workshops.value.length ? workshops.value : api.get('/auth/workshops')
    ]);
    kpi.value = kpiData;
    dynamics.value = detData.dynamics || [];
    if (!workshops.value.length) workshops.value = wsList;
    await nextTick();
    renderCharts();
  } catch {
    toast.error('Не удалось загрузить данные дашборда');
  } finally {
    isLoading.value = false;
  }
}

function renderCharts() {
  if (!kpi.value) return;

  charts.status = drawChart('statusChart', charts.status, {
    type: 'doughnut',
    data: {
      labels: kpi.value.equipmentByStatus.map(s => s.statusName),
      datasets: [{ data: kpi.value.equipmentByStatus.map(s => s.count), backgroundColor: ['#10b981', '#f59e0b', '#ef4444', '#64748b'], borderWidth: 0 }]
    },
    options: { onClick: (_, el) => el.length && router.push(`/equipment?search=${encodeURIComponent(kpi.value.equipmentByStatus[el[0].index].statusName)}`) }
  });

  charts.ws = drawChart('workshopChart', charts.ws, {
    type: 'bar',
    data: {
      labels: kpi.value.ordersByWorkshop.map(w => w.workshopName.replace('Цех', 'Ц')),
      datasets: [
        { label: 'В работе', data: kpi.value.ordersByWorkshop.map(w => w.activeOrders), backgroundColor: '#f59e0b' },
        { label: 'Выполнено', data: kpi.value.ordersByWorkshop.map(w => w.completedOrders), backgroundColor: '#10b981' }
      ]
    },
    options: { onClick: (_, el) => el.length && router.push(`/work-orders?workshopId=${kpi.value.ordersByWorkshop[el[0].index].workshopId}`) }
  });

  charts.dyn = drawChart('dynamicsChart', charts.dyn, {
    type: 'line',
    data: {
      labels: dynamics.value.map(d => d.label),
      datasets: [{ label: 'Простой по дням (ч)', data: dynamics.value.map(d => d.downtimeHours), borderColor: '#38bdf8', backgroundColor: 'rgba(56,189,248,0.12)', fill: true, tension: 0.3 }]
    },
    options: { onClick: () => router.push('/analytics') }
  });

  charts.top = drawChart('topDowntimeChart', charts.top, {
    type: 'bar',
    data: {
      labels: kpi.value.top5Downtime.map(t => `${t.inventoryNumber} (${t.equipmentName.slice(0, 14)})`),
      datasets: [{ label: 'Простой (ч)', data: kpi.value.top5Downtime.map(t => t.downtimeHours), backgroundColor: '#ef4444' }]
    },
    options: {
      indexAxis: 'y',
      plugins: { legend: { display: false } },
      onClick: (_, el) => el.length && router.push(`/equipment?search=${encodeURIComponent(kpi.value.top5Downtime[el[0].index].inventoryNumber)}`)
    }
  });
}

function toggleAutoRefresh() {
  if (refreshTimer) clearInterval(refreshTimer);
  if (autoRefresh.value) refreshTimer = setInterval(loadData, 60000);
}

onMounted(() => { loadData(); toggleAutoRefresh(); });
onUnmounted(() => {
  if (refreshTimer) clearInterval(refreshTimer);
  Object.values(charts).forEach(c => c?.destroy());
});
</script>

<template>
  <div class="dashboard-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">Сводный дашборд KPI</h1>
        <p class="page-sub">Оперативные показатели надежности и технического состояния оборудования</p>
      </div>

      <div class="header-actions">
        <div class="filter-item">
          <label>Цех:</label>
          <select v-model="selectedWorkshop" @change="loadData" class="form-select form-select-sm">
            <option :value="0">Все цеха предприятия</option>
            <option v-for="ws in workshops" :key="ws.workshopId" :value="ws.workshopId">
              {{ ws.workshopName }}
            </option>
          </select>
        </div>

        <label class="filter-item" style="cursor: pointer;">
          <input type="checkbox" v-model="autoRefresh" @change="toggleAutoRefresh" />
          <span>Автообновление (60с)</span>
        </label>
        <button class="btn btn-secondary btn-sm" @click="loadData">🔄 Обновить</button>
      </div>
    </div>

    <div v-if="isLoading && !kpi" class="kpi-grid">
      <div v-for="i in 6" :key="i" class="kpi-card skeleton" style="height: 110px;"></div>
    </div>

    <template v-else-if="kpi">
      <div class="kpi-grid">
        <div class="kpi-card"><div class="kpi-title">Парк оборудования</div><div class="kpi-value">{{ kpi.totalEquipment }}<span class="kpi-unit">ед.</span></div><div class="kpi-trend trend-neutral">● 100% на учете</div></div>
        <div class="kpi-card"><div class="kpi-title">Активные заявки</div><div class="kpi-value text-warning">{{ kpi.activeWorkOrders }}<span class="kpi-unit">в работе</span></div><div class="kpi-trend trend-down">▼ В процессе ремонта</div></div>
        <div class="kpi-card"><div class="kpi-title">Выполнено ремонтов</div><div class="kpi-value text-success">{{ kpi.completedWorkOrders }}<span class="kpi-unit">заявок</span></div><div class="kpi-trend trend-up">▲ Приняты ОТК</div></div>
        <div class="kpi-card"><div class="kpi-title">Суммарный простой</div><div class="kpi-value text-danger">{{ kpi.totalDowntimeHours }}<span class="kpi-unit">ч.</span></div><div class="kpi-trend trend-down">▼ Учет простоя</div></div>
        <div class="kpi-card"><div class="kpi-title">Средний ремонт (MTTR)</div><div class="kpi-value text-blue">{{ kpi.averageRepairHours }}<span class="kpi-unit">ч.</span></div><div class="kpi-trend trend-up">▲ Норматив: 6.0 ч.</div></div>
        <div class="kpi-card"><div class="kpi-title">Выполнение графика ТО</div><div class="kpi-value" :class="kpi.maintenanceComplianceRate >= 80 ? 'text-success' : 'text-warning'">{{ kpi.maintenanceComplianceRate }}<span class="kpi-unit">%</span></div><div class="kpi-trend trend-up">▲ Регламент ППР</div></div>
      </div>

      <div class="charts-grid">
        <div class="card">
          <div class="card-header"><div class="card-title">Распределение оборудования по статусам</div><button class="btn btn-secondary btn-sm" @click="exportChartToPNG('statusChart', 'statuses')">📷 PNG</button></div>
          <div class="card-body chart-wrapper"><canvas id="statusChart"></canvas></div>
        </div>
        <div class="card">
          <div class="card-header"><div class="card-title">Заявки по цехам предприятия</div><button class="btn btn-secondary btn-sm" @click="exportChartToPNG('workshopChart', 'workshops')">📷 PNG</button></div>
          <div class="card-body chart-wrapper"><canvas id="workshopChart"></canvas></div>
        </div>
        <div class="card">
          <div class="card-header"><div class="card-title">Динамика заявок и простоев (30 дней)</div><button class="btn btn-secondary btn-sm" @click="exportChartToPNG('dynamicsChart', 'dynamics')">📷 PNG</button></div>
          <div class="card-body chart-wrapper"><canvas id="dynamicsChart"></canvas></div>
        </div>
        <div class="card">
          <div class="card-header"><div class="card-title">Топ-5 оборудования по простоям</div><button class="btn btn-secondary btn-sm" @click="exportChartToPNG('topDowntimeChart', 'top-downtime')">📷 PNG</button></div>
          <div class="card-body chart-wrapper"><canvas id="topDowntimeChart"></canvas></div>
        </div>
      </div>

    <!-- Нижний ряд (R148, R149) -->
    <div v-if="kpi" class="split-grid">
      <div class="card">
        <div class="card-header">
          <div class="card-title">Последние заявки на ремонт</div>
          <router-link to="/work-orders" class="btn btn-secondary btn-sm">Все заявки →</router-link>
        </div>
        <div class="card-body" style="padding: 0;">
          <div class="table-wrapper" style="border: none;">
            <table class="data-table">
              <thead>
                <tr>
                  <th>№ Заявки</th>
                  <th>Оборудование</th>
                  <th>Цех</th>
                  <th>Статус</th>
                  <th>Простой</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="wo in kpi.recentWorkOrders" :key="wo.workOrderId">
                  <td>
                    <router-link :to="`/work-orders?search=${wo.orderNumber}`" class="order-link">
                      {{ wo.orderNumber }}
                    </router-link>
                  </td>
                  <td>{{ wo.equipmentName }}</td>
                  <td>{{ wo.workshopName }}</td>
                  <td>
                    <span class="badge" :class="wo.orderStatusId === 5 ? 'badge-success' : wo.orderStatusId === 3 ? 'badge-warning' : 'badge-info'">
                      {{ wo.statusName }}
                    </span>
                  </td>
                  <td>{{ wo.downtimeHours }} ч.</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

        <div class="card">
          <div class="card-header"><div class="card-title">Нарушения и просрочки</div><span class="badge badge-warning">{{ kpi.violations.length }}</span></div>
          <div class="card-body" style="display: flex; flex-direction: column; gap: 8px;">
            <div v-if="!kpi.violations.length" class="detail-box text-success" style="text-align: center;">✓ Нарушений не зафиксировано</div>
            <div v-for="(v, i) in kpi.violations" :key="i" class="detail-box" :style="{ borderLeft: `3px solid ${v.severity === 'danger' ? '#ef4444' : '#f59e0b'}` }">
              <div style="font-weight: 700;">{{ v.title }}</div>
              <div style="font-size: 11px; color: var(--text-muted);">{{ v.description }}</div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>
