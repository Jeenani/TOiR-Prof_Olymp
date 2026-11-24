<script setup>
import { ref, onMounted, onUnmounted, nextTick } from 'vue';
import { useRouter } from 'vue-router';
import { Chart, registerables } from 'chart.js';
import { api } from '../services/api';
import { exportChartToPNG } from '../services/export';
import { useToastStore } from '../stores/toastStore';

Chart.register(...registerables);

const router = useRouter();
const toast = useToastStore();

const kpi = ref(null);
const isLoading = ref(true);
const selectedWorkshop = ref(0);
const workshops = ref([]);
const autoRefresh = ref(true);
let refreshTimer = null;

let statusChart = null;
let workshopChart = null;
let topDowntimeChart = null;

async function loadData() {
  isLoading.value = true;
  try {
    const params = selectedWorkshop.value > 0 ? { workshopId: selectedWorkshop.value } : {};
    const [kpiData, wsList] = await Promise.all([
      api.get('/analytics/dashboard', params),
      workshops.value.length ? Promise.resolve(workshops.value) : api.get('/auth/workshops')
    ]);

    kpi.value = kpiData;
    if (!workshops.value.length) workshops.value = wsList;

    await nextTick();
    renderCharts();
  } catch (err) {
    console.error('Ошибка загрузки дашборда:', err);
    toast.error('Не удалось загрузить данные дашборда');
  } finally {
    isLoading.value = false;
  }
}

function renderCharts() {
  if (!kpi.value) return;

  // 1. Статусы оборудования (Doughnut) - R147
  const statusCanvas = document.getElementById('statusChart');
  if (statusCanvas) {
    if (statusChart) statusChart.destroy();
    const labels = kpi.value.equipmentByStatus.map(s => s.statusName);
    const data = kpi.value.equipmentByStatus.map(s => s.count);
    const colors = ['#10b981', '#f59e0b', '#ef4444', '#64748b', '#8b5cf6'];

    statusChart = new Chart(statusCanvas, {
      type: 'doughnut',
      data: {
        labels,
        datasets: [{ data, backgroundColor: colors, borderWidth: 0 }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'right', labels: { color: '#94a3b8', font: { size: 11 } } }
        },
        onClick: (evt, elements) => {
          if (elements.length > 0) {
            const index = elements[0].index;
            const status = kpi.value.equipmentByStatus[index];
            router.push(`/equipment?search=${encodeURIComponent(status.statusName)}`);
          }
        }
      }
    });
  }

  // 2. Заявки по цехам (Bar) - R147
  const wsCanvas = document.getElementById('workshopChart');
  if (wsCanvas) {
    if (workshopChart) workshopChart.destroy();
    const wsLabels = kpi.value.ordersByWorkshop.map(w => w.workshopName.replace('Цех', 'Ц'));
    const activeData = kpi.value.ordersByWorkshop.map(w => w.activeOrders);
    const completedData = kpi.value.ordersByWorkshop.map(w => w.completedOrders);

    workshopChart = new Chart(wsCanvas, {
      type: 'bar',
      data: {
        labels: wsLabels,
        datasets: [
          { label: 'В работе', data: activeData, backgroundColor: '#f59e0b' },
          { label: 'Выполнено', data: completedData, backgroundColor: '#10b981' }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: { ticks: { color: '#94a3b8' }, grid: { color: '#334155' } },
          y: { ticks: { color: '#94a3b8' }, grid: { color: '#334155' } }
        },
        plugins: {
          legend: { labels: { color: '#94a3b8', font: { size: 11 } } }
        },
        onClick: (evt, elements) => {
          if (elements.length > 0) {
            const index = elements[0].index;
            const ws = kpi.value.ordersByWorkshop[index];
            router.push(`/work-orders?workshopId=${ws.workshopId}`);
          }
        }
      }
    });
  }

  // 3. Топ-5 простоев (Horizontal Bar) - R148
  const topCanvas = document.getElementById('topDowntimeChart');
  if (topCanvas) {
    if (topDowntimeChart) topDowntimeChart.destroy();
    const labels = kpi.value.top5Downtime.map(t => `${t.inventoryNumber} (${t.equipmentName.slice(0, 15)}...)`);
    const data = kpi.value.top5Downtime.map(t => t.downtimeHours);

    topDowntimeChart = new Chart(topCanvas, {
      type: 'bar',
      data: {
        labels,
        datasets: [{ label: 'Простой (часов)', data, backgroundColor: '#ef4444' }]
      },
      options: {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: { ticks: { color: '#94a3b8' }, grid: { color: '#334155' } },
          y: { ticks: { color: '#94a3b8' }, grid: { display: false } }
        },
        plugins: {
          legend: { display: false }
        },
        onClick: (evt, elements) => {
          if (elements.length > 0) {
            const index = elements[0].index;
            const eq = kpi.value.top5Downtime[index];
            router.push(`/equipment?search=${encodeURIComponent(eq.inventoryNumber)}`);
          }
        }
      }
    });
  }
}

function handleAutoRefreshChange() {
  if (refreshTimer) clearInterval(refreshTimer);
  if (autoRefresh.value) {
    refreshTimer = setInterval(() => {
      loadData();
    }, 60000); // 60 секунд (R150)
  }
}

onMounted(() => {
  loadData();
  handleAutoRefreshChange();
});

onUnmounted(() => {
  if (refreshTimer) clearInterval(refreshTimer);
  if (statusChart) statusChart.destroy();
  if (workshopChart) workshopChart.destroy();
  if (topDowntimeChart) topDowntimeChart.destroy();
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

        <label class="auto-refresh-toggle" title="Автоматическое обновление каждые 60 сек">
          <input type="checkbox" v-model="autoRefresh" @change="handleAutoRefreshChange" />
          <span>Автообновление (60с)</span>
        </label>

        <button class="btn btn-secondary btn-sm" @click="loadData">🔄 Обновить</button>
      </div>
    </div>

    <div v-if="isLoading && !kpi" class="kpi-grid">
      <div v-for="i in 6" :key="i" class="kpi-card skeleton" style="height: 110px;"></div>
    </div>

    <!-- 6 Карточек KPI (R146) -->
    <div v-else-if="kpi" class="kpi-grid">
      <div class="kpi-card">
        <div class="kpi-title">Парк оборудования</div>
        <div class="kpi-value">{{ kpi.totalEquipment }} <span class="kpi-unit">ед.</span></div>
        <div class="kpi-trend trend-neutral">● 100% на учете в ТОиР</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-title">Активные заявки</div>
        <div class="kpi-value" :class="kpi.activeWorkOrders > 0 ? 'text-warning' : ''">
          {{ kpi.activeWorkOrders }} <span class="kpi-unit">в работе</span>
        </div>
        <div class="kpi-trend trend-down">▼ В процессе ремонта</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-title">Выполненные ремонты</div>
        <div class="kpi-value text-success">{{ kpi.completedWorkOrders }} <span class="kpi-unit">заявок</span></div>
        <div class="kpi-trend trend-up">▲ 100% приняты ОТК</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-title">Суммарный простой</div>
        <div class="kpi-value text-danger">{{ kpi.totalDowntimeHours }} <span class="kpi-unit">ч.</span></div>
        <div class="kpi-trend trend-down">▼ Учет простоя станков</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-title">Среднее время ремонта (MTTR)</div>
        <div class="kpi-value text-blue">{{ kpi.averageRepairHours }} <span class="kpi-unit">ч.</span></div>
        <div class="kpi-trend trend-up">▲ Норматив: до 6.0 ч.</div>
      </div>

      <div class="kpi-card">
        <div class="kpi-title">Выполнение графика ТО</div>
        <div class="kpi-value" :class="kpi.maintenanceComplianceRate >= 80 ? 'text-success' : 'text-warning'">
          {{ kpi.maintenanceComplianceRate }}<span class="kpi-unit">%</span>
        </div>
        <div class="kpi-trend trend-up">▲ Регламент ППР соблюден</div>
      </div>
    </div>

    <!-- Графики (R147) -->
    <div v-if="kpi" class="charts-grid">
      <div class="card">
        <div class="card-header">
          <div class="card-title">Распределение оборудования по статусам</div>
          <button class="btn btn-secondary btn-sm" @click="exportChartToPNG('statusChart', 'equipment-statuses')">📷 PNG</button>
        </div>
        <div class="card-body chart-wrapper">
          <canvas id="statusChart"></canvas>
        </div>
      </div>

      <div class="card">
        <div class="card-header">
          <div class="card-title">Заявки по цехам предприятия</div>
          <button class="btn btn-secondary btn-sm" @click="exportChartToPNG('workshopChart', 'orders-by-workshop')">📷 PNG</button>
        </div>
        <div class="card-body chart-wrapper">
          <canvas id="workshopChart"></canvas>
        </div>
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

      <div style="display: flex; flex-direction: column; gap: 20px;">
        <div class="card">
          <div class="card-header">
            <div class="card-title">Топ-5 оборудования по простоям</div>
          </div>
          <div class="card-body chart-wrapper-sm">
            <canvas id="topDowntimeChart"></canvas>
          </div>
        </div>

        <div class="card">
          <div class="card-header">
            <div class="card-title">Нарушения и предупреждения</div>
            <span class="badge badge-warning">{{ kpi.violations.length }}</span>
          </div>
          <div class="card-body" style="padding: 12px;">
            <div v-if="kpi.violations.length === 0" class="no-violations">
              ✓ Нарушений регламентов и критических остатков не зафиксировано
            </div>
            <div v-else class="violations-list">
              <div v-for="(v, idx) in kpi.violations" :key="idx" class="violation-card" :class="`viol-${v.severity}`">
                <div class="viol-title">{{ v.title }}</div>
                <div class="viol-desc">{{ v.description }}</div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 24px; flex-wrap: wrap; gap: 16px; }
.page-title { font-size: 22px; font-weight: 800; color: #f8fafc; }
.page-sub { font-size: 13px; color: #94a3b8; margin-top: 4px; }
.header-actions { display: flex; align-items: center; gap: 14px; flex-wrap: wrap; }
.form-select-sm { padding: 6px 12px; font-size: 12px; width: auto; }
.auto-refresh-toggle { display: flex; align-items: center; gap: 8px; font-size: 12px; color: #94a3b8; cursor: pointer; }
.kpi-unit { font-size: 14px; font-weight: 500; color: #94a3b8; margin-left: 4px; }
.text-success { color: #34d399 !important; }
.text-warning { color: #fbbf24 !important; }
.text-danger { color: #f87171 !important; }
.text-blue { color: #38bdf8 !important; }
.chart-wrapper { position: relative; height: 260px; }
.chart-wrapper-sm { position: relative; height: 180px; }
.order-link { color: #38bdf8; text-decoration: none; font-weight: 600; }
.order-link:hover { text-decoration: underline; }
.no-violations { padding: 12px; font-size: 12px; color: #34d399; text-align: center; background: rgba(16, 185, 129, 0.1); border-radius: 6px; }
.violations-list { display: flex; flex-direction: column; gap: 8px; }
.violation-card { padding: 10px 12px; border-radius: 6px; font-size: 12px; }
.viol-danger { background: rgba(239, 68, 68, 0.15); border-left: 3px solid #ef4444; color: #f87171; }
.viol-warning { background: rgba(245, 158, 11, 0.15); border-left: 3px solid #f59e0b; color: #fbbf24; }
.viol-title { font-weight: 700; margin-bottom: 2px; }
.viol-desc { font-size: 11px; opacity: 0.9; }
</style>
