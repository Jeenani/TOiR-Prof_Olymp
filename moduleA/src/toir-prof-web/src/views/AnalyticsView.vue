<script setup>
import { ref, onMounted, nextTick } from 'vue';
import { Chart, registerables } from 'chart.js';
import { api } from '../services/api';
import { exportTableToCSV, exportReportToPDF, exportChartToPNG } from '../services/export';
import { useToastStore } from '../stores/toastStore';

Chart.register(...registerables);
const toast = useToastStore();

const filterPeriod = ref('Q1'); // 'Q1', 'Q2', 'Q3', 'Q4', 'year'
const filterWorkshop = ref(0);
const filterOrderType = ref('all');
const workshops = ref([]);
const savedTemplates = ref([
  { id: 1, name: '1 Квартал 2027 (Все цеха)', period: 'Q1', workshop: 0, orderType: 'all' },
  { id: 2, name: 'ЦРМ: Аварийные простои', period: 'year', workshop: 1, orderType: 'emergency' }
]);

const summaryData = ref(null);
const isLoading = ref(false);

let downtimeChart = null;
let materialsChart = null;
let teamsChart = null;
let scheduleChart = null;

// Пример данных аналитики (сценарии R169 и R170)
const topMaterials = ref([
  { code: 'M-001', name: 'Масло индустриальное И-40А', used: 145, unit: 'л', cost: 40600 },
  { code: 'M-002', name: 'Подшипник 205 ГОСТ 8338', used: 28, unit: 'шт', cost: 12600 },
  { code: 'M-003', name: 'Ремень клиновой B-1800', used: 16, unit: 'шт', cost: 10400 },
  { code: 'M-005', name: 'Сальник 45x65x10', used: 22, unit: 'шт', cost: 5500 },
  { code: 'M-006', name: 'Фильтр гидравлический', used: 8, unit: 'шт', cost: 14400 }
]);

const teamsRating = ref([
  { id: 1, name: 'Бригада №1 (Механическая)', leader: 'Соколов В.М.', ordersDone: 14, avgHours: 3.8, efficiency: 96 },
  { id: 2, name: 'Бригада №2 (Электротехническая)', leader: 'Морозов Д.А.', ordersDone: 11, avgHours: 4.2, efficiency: 92 },
  { id: 3, name: 'Бригада №3 (Гидравлика и ЧПУ)', leader: 'Новиков К.С.', ordersDone: 9, avgHours: 5.1, efficiency: 88 }
]);

async function loadAnalytics() {
  isLoading.value = true;
  try {
    const ws = await api.get('/auth/workshops');
    workshops.value = ws;

    const kpi = await api.get('/analytics/dashboard', filterWorkshop.value > 0 ? { workshopId: filterWorkshop.value } : {});
    summaryData.value = kpi;

    await nextTick();
    renderAnalyticsCharts();
  } catch (e) {
    toast.error('Не удалось загрузить аналитику');
  } finally {
    isLoading.value = false;
  }
}

function renderAnalyticsCharts() {
  // 1. График динамики простоев (Line) - R154
  const dtCanvas = document.getElementById('anDowntimeChart');
  if (dtCanvas) {
    if (downtimeChart) downtimeChart.destroy();
    downtimeChart = new Chart(dtCanvas, {
      type: 'line',
      data: {
        labels: ['Нед 1', 'Нед 2', 'Нед 3', 'Нед 4', 'Нед 5', 'Нед 6', 'Нед 7', 'Нед 8', 'Нед 9', 'Нед 10', 'Нед 11', 'Нед 12'],
        datasets: [{
          label: 'Часы простоя оборудования',
          data: [4.5, 2.0, 8.5, 1.5, 6.0, 3.2, 5.0, 7.8, 2.4, 4.0, 1.8, 3.5],
          borderColor: '#ef4444',
          backgroundColor: 'rgba(239, 68, 68, 0.1)',
          fill: true,
          tension: 0.3
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: { ticks: { color: '#94a3b8' }, grid: { color: '#334155' } },
          y: { ticks: { color: '#94a3b8' }, grid: { color: '#334155' } }
        },
        plugins: { legend: { labels: { color: '#94a3b8' } } }
      }
    });
  }

  // 2. Расход материалов (Bar) - R155
  const matCanvas = document.getElementById('anMaterialsChart');
  if (matCanvas) {
    if (materialsChart) materialsChart.destroy();
    materialsChart = new Chart(matCanvas, {
      type: 'bar',
      data: {
        labels: topMaterials.value.map(m => m.name.slice(0, 15) + '...'),
        datasets: [{
          label: 'Сумма расхода (₽)',
          data: topMaterials.value.map(m => m.cost),
          backgroundColor: '#0284c7'
        }]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: { ticks: { color: '#94a3b8' }, grid: { display: false } },
          y: { ticks: { color: '#94a3b8' }, grid: { color: '#334155' } }
        },
        plugins: { legend: { labels: { color: '#94a3b8' } } }
      }
    });
  }

  // 3. Выполнение графика ТО по цехам (Bar) - R156, R170
  const schCanvas = document.getElementById('anScheduleChart');
  if (schCanvas) {
    if (scheduleChart) scheduleChart.destroy();
    scheduleChart = new Chart(schCanvas, {
      type: 'bar',
      data: {
        labels: ['ЦРМ (Цех 1)', 'Механический (Цех 2)', 'Литейный (Цех 3)'],
        datasets: [
          { label: 'План ТО', data: [12, 10, 8], backgroundColor: '#334155' },
          { label: 'Факт выполнено', data: [11, 9, 8], backgroundColor: '#10b981' }
        ]
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
          x: { ticks: { color: '#94a3b8' }, grid: { color: '#334155' } },
          y: { ticks: { color: '#94a3b8' }, grid: { color: '#334155' } }
        },
        plugins: { legend: { labels: { color: '#94a3b8' } } }
      }
    });
  }
}

// Сохранение и сброс шаблонов фильтров (R152)
function saveFilterTemplate() {
  const name = prompt('Введите наименование шаблона фильтра:');
  if (name) {
    savedTemplates.value.push({
      id: Date.now(),
      name,
      period: filterPeriod.value,
      workshop: filterWorkshop.value,
      orderType: filterOrderType.value
    });
    toast.success(`Шаблон "${name}" сохранен`);
  }
}

function applyTemplate(tpl) {
  filterPeriod.value = tpl.period;
  filterWorkshop.value = tpl.workshop;
  filterOrderType.value = tpl.orderType;
  loadAnalytics();
  toast.info(`Применен шаблон: ${tpl.name}`);
}

function resetFilters() {
  filterPeriod.value = 'Q1';
  filterWorkshop.value = 0;
  filterOrderType.value = 'all';
  loadAnalytics();
  toast.info('Фильтры сброшены');
}

// Экспорт аналитики (R158, R169)
function exportDowntimeCSV() {
  exportTableToCSV(
    `downtime-analysis-${filterPeriod.value}`,
    summaryData.value?.top5Downtime || [],
    { inventoryNumber: 'Инв. №', equipmentName: 'Оборудование', workshopName: 'Цех', downtimeHours: 'Простой (ч)' }
  );
  toast.success('Экспорт CSV завершен');
}

function exportAnalyticsPDF() {
  const tableData = (summaryData.value?.top5Downtime || []).map(t => [
    t.inventoryNumber, t.equipmentName, t.workshopName, `${t.downtimeHours} ч.`
  ]);
  exportReportToPDF(
    'Квартальный отчет по анализу простоев и надежности',
    `Период: ${filterPeriod.value} 2027 | Предприятие: ПМЗ | Система: ТОиР-Проф`,
    ['Инв. №', 'Оборудование', 'Цех', 'Время простоя'],
    tableData,
    `quarterly-downtime-report-${filterPeriod.value}`
  );
  toast.success('Экспорт PDF сформирован');
}

onMounted(() => { loadAnalytics(); });
</script>

<template>
  <div class="analytics-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">Аналитическая панель ТОиР</h1>
        <p class="page-sub">Глубокий анализ простоев, материальных затрат и эффективности бригад</p>
      </div>

      <div style="display: flex; gap: 10px;">
        <button class="btn btn-secondary btn-sm" @click="exportDowntimeCSV">📊 Экспорт CSV</button>
        <button class="btn btn-primary btn-sm" @click="exportAnalyticsPDF">📑 Экспорт отчета в PDF</button>
      </div>
    </div>

    <!-- Панель фильтров с шаблонами (R151, R152) -->
    <div class="filter-toolbar">
      <div class="filter-item">
        <label>Период:</label>
        <select v-model="filterPeriod" @change="loadAnalytics" class="form-select">
          <option value="Q1">1 Квартал 2027</option>
          <option value="Q2">2 Квартал 2027</option>
          <option value="year">Весь 2027 год</option>
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
          <option value="all">Все виды работ</option>
          <option value="emergency">Только аварийные</option>
          <option value="planned">Только плановые (ТО)</option>
        </select>
      </div>

      <div style="display: flex; gap: 8px; margin-left: auto;">
        <button class="btn btn-secondary btn-sm" @click="saveFilterTemplate" title="Сохранить текущую комбинацию фильтров">
          💾 Сохранить шаблон
        </button>
        <button class="btn btn-secondary btn-sm" @click="resetFilters">Сброс</button>
      </div>
    </div>

    <!-- Шаблоны фильтров (R152) -->
    <div v-if="savedTemplates.length > 0" class="templates-bar">
      <span style="font-size: 11px; color: #94a3b8; font-weight: 700;">ШАБЛОНЫ:</span>
      <button v-for="t in savedTemplates" :key="t.id" class="template-chip" @click="applyTemplate(t)">
        {{ t.name }}
      </button>
    </div>

    <!-- Графики Аналитики (R154, R155, R156) -->
    <div class="charts-grid">
      <!-- Блок анализа простоев (R154, R169) -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Динамика простоев оборудования за квартал (часов)</div>
          <button class="btn btn-secondary btn-sm" @click="exportChartToPNG('anDowntimeChart', 'downtime-dynamics')">📷 PNG</button>
        </div>
        <div class="card-body chart-wrapper">
          <canvas id="anDowntimeChart"></canvas>
        </div>
      </div>

      <!-- Блок расхода материалов (R155) -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Топ-5 затрат на материалы и запчасти (₽)</div>
          <button class="btn btn-secondary btn-sm" @click="exportChartToPNG('anMaterialsChart', 'materials-costs')">📷 PNG</button>
        </div>
        <div class="card-body chart-wrapper">
          <canvas id="anMaterialsChart"></canvas>
        </div>
      </div>
    </div>

    <!-- Выполнение графика ТО + Рейтинг бригад (R156, R157, R170) -->
    <div class="split-grid">
      <!-- График ТО по цехам -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Выполнение графика ТО по цехам (План vs Факт)</div>
          <button class="btn btn-secondary btn-sm" @click="exportChartToPNG('anScheduleChart', 'maintenance-compliance')">📷 PNG</button>
        </div>
        <div class="card-body chart-wrapper">
          <canvas id="anScheduleChart"></canvas>
        </div>
      </div>

      <!-- Рейтинг эффективности бригад (R157) -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">Рейтинг эффективности бригад</div>
        </div>
        <div class="card-body" style="padding: 0;">
          <table class="data-table">
            <thead>
              <tr>
                <th>Бригада</th>
                <th>Заявок</th>
                <th>Ср. время</th>
                <th>КПД</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="t in teamsRating" :key="t.id">
                <td>
                  <div style="font-weight: 600;">{{ t.name }}</div>
                  <div style="font-size: 11px; color: #94a3b8;">{{ t.leader }}</div>
                </td>
                <td>{{ t.ordersDone }}</td>
                <td>{{ t.avgHours }} ч.</td>
                <td>
                  <span class="badge" :class="t.efficiency >= 90 ? 'badge-success' : 'badge-warning'">
                    {{ t.efficiency }}%
                  </span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 20px; flex-wrap: wrap; gap: 14px; }
.page-title { font-size: 22px; font-weight: 800; color: #f8fafc; }
.page-sub { font-size: 13px; color: #94a3b8; }
.templates-bar { display: flex; align-items: center; gap: 8px; margin-bottom: 18px; flex-wrap: wrap; }
.template-chip { background: #1e293b; border: 1px solid #475569; color: #38bdf8; font-size: 12px; padding: 4px 10px; border-radius: 14px; cursor: pointer; }
.template-chip:hover { background: #334155; }
.chart-wrapper { position: relative; height: 260px; }
</style>
