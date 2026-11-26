<script setup>
import { ref, onMounted, onUnmounted, nextTick } from 'vue';
import { useRouter } from 'vue-router';
import { api } from '../services/api';
import { exportTableToCSV, exportReportToPDF, exportChartToPNG, exportPageToPDF } from '../services/export';
import { drawChart, orderBadge } from '../services/ui';
import { useToastStore } from '../stores/toastStore';

const router = useRouter();
const toast = useToastStore();

const filterPeriod = ref('Q1');
const filterWorkshop = ref(0);
const filterOrderType = ref('all');
const workshops = ref([]);
const savedTemplates = ref([
  { id: 1, name: '1 Квартал 2027 (Все цеха)', period: 'Q1', workshop: 0, orderType: 'all' },
  { id: 2, name: 'ЦРМ: Аварийные простои', period: 'year', workshop: 1, orderType: 'emergency' }
]);

const summaryData = ref(null);
const details = ref(null);
const teamsRating = ref([]);
let charts = {};

async function loadAnalytics() {
  try {
    const [ws, kpi, det] = await Promise.all([
      workshops.value.length ? workshops.value : api.get('/auth/workshops'),
      api.get('/analytics/dashboard', filterWorkshop.value > 0 ? { workshopId: filterWorkshop.value } : {}),
      api.get('/analytics/details', { workshopId: filterWorkshop.value || undefined, orderType: filterOrderType.value })
    ]);
    if (!workshops.value.length) workshops.value = ws;
    summaryData.value = kpi;
    details.value = det;
    teamsRating.value = (det.teamsRating || []).map(t => ({
      ...t, avgHours: Number(t.avgHours || 0).toFixed(1), efficiency: Math.min(100, 80 + t.ordersDone * 5)
    }));
    await nextTick();
    renderCharts();
  } catch {
    toast.error('Не удалось загрузить аналитику');
  }
}

function renderCharts() {
  const dyn = details.value?.dynamics || [];
  const mats = details.value?.topMaterials || [];
  const sch = details.value?.schByWorkshop || [];
  const prios = details.value?.ordersByPriority || [];

  charts.dt = drawChart('anDowntimeChart', charts.dt, {
    type: 'line',
    data: { labels: dyn.map(d => d.label), datasets: [{ label: 'Простои (ч)', data: dyn.map(d => d.downtimeHours), borderColor: '#ef4444', backgroundColor: 'rgba(239,68,68,0.1)', fill: true, tension: 0.3 }] }
  });
  charts.mat = drawChart('anMaterialsChart', charts.mat, {
    type: 'bar',
    data: { labels: mats.map(m => m.name.slice(0, 15)), datasets: [{ label: 'Расход (₽)', data: mats.map(m => m.cost), backgroundColor: '#0284c7' }] },
    options: { onClick: () => router.push('/catalogs') }
  });
  charts.sch = drawChart('anScheduleChart', charts.sch, {
    type: 'bar',
    data: { labels: sch.map(s => s.workshopName), datasets: [{ label: 'План ТО', data: sch.map(s => s.planned), backgroundColor: '#475569' }, { label: 'Выполнено', data: sch.map(s => s.completed), backgroundColor: '#10b981' }] }
  });
  charts.prio = drawChart('anPriorityChart', charts.prio, {
    type: 'doughnut',
    data: { labels: prios.map(p => p.priorityName), datasets: [{ data: prios.map(p => p.count), backgroundColor: ['#ef4444', '#f59e0b', '#38bdf8', '#64748b'], borderWidth: 0 }] },
    options: { onClick: () => router.push('/work-orders') }
  });
}

function saveFilterTemplate() {
  const name = prompt('Название шаблона фильтра:');
  if (!name) return;
  savedTemplates.value.push({ id: Date.now(), name, period: filterPeriod.value, workshop: filterWorkshop.value, orderType: filterOrderType.value });
  toast.success(`Шаблон "${name}" сохранен`);
}

function applyTemplate(t) {
  filterPeriod.value = t.period; filterWorkshop.value = t.workshop; filterOrderType.value = t.orderType;
  loadAnalytics();
}

function resetFilters() {
  filterPeriod.value = 'Q1'; filterWorkshop.value = 0; filterOrderType.value = 'all';
  loadAnalytics();
}

const exportDowntimeCSV = () => exportTableToCSV(`downtime-${filterPeriod.value}`, summaryData.value?.top5Downtime || [], { inventoryNumber: 'Инв. №', equipmentName: 'Оборудование', workshopName: 'Цех', downtimeHours: 'Простой (ч)' });
const exportAnalyticsPDF = () => exportReportToPDF('Анализ простоев оборудования', `Период: ${filterPeriod.value} 2027`, ['Инв. №', 'Оборудование', 'Цех', 'Простой'], (summaryData.value?.top5Downtime || []).map(t => [t.inventoryNumber, t.equipmentName, t.workshopName, `${t.downtimeHours} ч.`]), `downtime-${filterPeriod.value}`);

onMounted(loadAnalytics);
onUnmounted(() => Object.values(charts).forEach(c => c?.destroy()));
</script>

<template>
  <div id="analytics-root">
    <div class="page-header">
      <div>
        <h1 class="page-title">Аналитическая панель ТОиР</h1>
        <p class="page-sub">Анализ простоев, заявок, затрат на ТМЦ и эффективности бригад</p>
      </div>
      <div class="header-actions">
        <button class="btn btn-secondary btn-sm" @click="exportDowntimeCSV">📊 CSV</button>
        <button class="btn btn-secondary btn-sm" @click="exportAnalyticsPDF">📑 Блок PDF</button>
        <button class="btn btn-primary btn-sm" @click="exportPageToPDF('#analytics-root', 'analytics-full')">📄 Страница PDF</button>
      </div>
    </div>

    <div class="filter-toolbar">
      <div class="filter-item">
        <label>Период:</label>
        <select v-model="filterPeriod" @change="loadAnalytics" class="form-select">
          <option value="Q1">1 Квартал 2027</option><option value="Q2">2 Квартал 2027</option><option value="year">Весь 2027 год</option>
        </select>
      </div>
      <div class="filter-item">
        <label>Цех:</label>
        <select v-model="filterWorkshop" @change="loadAnalytics" class="form-select">
          <option :value="0">Все цеха</option>
          <option v-for="w in workshops" :key="w.workshopId" :value="w.workshopId">{{ w.workshopName }}</option>
        </select>
      </div>
      <div class="filter-item">
        <label>Тип заявок:</label>
        <select v-model="filterOrderType" @change="loadAnalytics" class="form-select">
          <option value="all">Все виды</option><option value="emergency">Аварийные</option><option value="planned">Плановые (ТО)</option>
        </select>
      </div>
      <div style="display: flex; gap: 8px; margin-left: auto;">
        <button class="btn btn-secondary btn-sm" @click="saveFilterTemplate">💾 Шаблон</button>
        <button class="btn btn-secondary btn-sm" @click="resetFilters">Сброс</button>
      </div>
    </div>

    <div v-if="savedTemplates.length" class="templates-bar">
      <span class="detail-label">ШАБЛОНЫ:</span>
      <button v-for="t in savedTemplates" :key="t.id" class="template-chip" @click="applyTemplate(t)">{{ t.name }}</button>
    </div>

    <div v-if="details" class="charts-grid">
      <div class="card">
        <div class="card-header"><div class="card-title">Сводка заявок по статусам</div></div>
        <table class="data-table">
          <thead><tr><th>Статус</th><th>Кол-во</th><th>Простой</th></tr></thead>
          <tbody>
            <tr v-for="s in details.ordersByStatus" :key="s.orderStatusId" style="cursor: pointer;" @click="router.push('/work-orders')">
              <td><span class="badge" :class="orderBadge(s.orderStatusId)">{{ s.statusName }}</span></td>
              <td>{{ s.count }}</td><td>{{ s.downtime }} ч.</td>
            </tr>
          </tbody>
        </table>
      </div>
      <div class="card">
        <div class="card-header"><div class="card-title">Заявки по приоритетам</div><button class="btn btn-secondary btn-sm" @click="exportChartToPNG('anPriorityChart', 'priorities')">📷 PNG</button></div>
        <div class="card-body chart-wrapper"><canvas id="anPriorityChart"></canvas></div>
      </div>
    </div>

    <div class="charts-grid">
      <div class="card">
        <div class="card-header"><div class="card-title">Динамика простоев (ч)</div><button class="btn btn-secondary btn-sm" @click="exportChartToPNG('anDowntimeChart', 'downtime')">📷 PNG</button></div>
        <div class="card-body chart-wrapper"><canvas id="anDowntimeChart"></canvas></div>
      </div>
      <div class="card">
        <div class="card-header"><div class="card-title">Топ затрат на материалы (₽)</div><button class="btn btn-secondary btn-sm" @click="exportChartToPNG('anMaterialsChart', 'materials')">📷 PNG</button></div>
        <div class="card-body chart-wrapper"><canvas id="anMaterialsChart"></canvas></div>
      </div>
    </div>

    <div class="split-grid">
      <div class="card">
        <div class="card-header"><div class="card-title">Выполнение графика ТО по цехам</div><button class="btn btn-secondary btn-sm" @click="exportChartToPNG('anScheduleChart', 'schedules')">📷 PNG</button></div>
        <div class="card-body chart-wrapper"><canvas id="anScheduleChart"></canvas></div>
      </div>
      <div class="card">
        <div class="card-header"><div class="card-title">Рейтинг эффективности бригад</div></div>
        <table class="data-table">
          <thead><tr><th>Бригада</th><th>Заявок</th><th>Ср. время</th><th>КПД</th></tr></thead>
          <tbody>
            <tr v-for="t in teamsRating" :key="t.id" style="cursor: pointer;" @click="router.push('/catalogs')">
              <td><div style="font-weight: 600;">{{ t.name }}</div><div style="font-size: 11px; color: var(--text-muted);">{{ t.leader }}</div></td>
              <td>{{ t.ordersDone }}</td><td>{{ t.avgHours }} ч.</td>
              <td><span class="badge" :class="t.efficiency >= 90 ? 'badge-success' : 'badge-warning'">{{ t.efficiency }}%</span></td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>
