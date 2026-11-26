<script setup>
import { ref } from 'vue';
import { api } from '../services/api';
import { exportTableToExcel, exportReportToPDF } from '../services/export';
import { useToastStore } from '../stores/toastStore';

const toast = useToastStore();

const reportType = ref('downtime'); // 'downtime' | 'orders' | 'materials' | 'compliance'
const selectedWorkshop = ref(0);
const dateStart = ref('2027-01-01');
const dateEnd = ref('2027-03-31');
const isGenerating = ref(false);
const reportData = ref(null);

async function generateReport() {
  isGenerating.value = true;
  try {
    if (reportType.value === 'downtime') {
      const kpi = await api.get('/analytics/dashboard', selectedWorkshop.value > 0 ? { workshopId: selectedWorkshop.value } : {});
      reportData.value = {
        title: 'Отчет по анализу простоев и надежности оборудования',
        headers: { inventoryNumber: 'Инв. №', equipmentName: 'Оборудование', workshopName: 'Цех', downtimeHours: 'Простой (ч)' },
        rows: kpi.top5Downtime || []
      };
    } else if (reportType.value === 'materials') {
      const mats = await api.get('/materials');
      reportData.value = {
        title: 'Ведомость остатков и расхода ТМЦ и запасных частей',
        headers: { materialCode: 'Артикул', materialName: 'Наименование', warehouseName: 'Склад', currentStock: 'Остаток', unitPrice: 'Цена (₽)' },
        rows: mats || []
      };
    } else {
      const res = await api.get('/work-orders', { pageSize: 50 });
      reportData.value = {
        title: reportType.value === 'compliance' ? 'Отчет по выполнению графика планово-предупредительного ТО' : 'Сводный реестр заявок ТОиР',
        headers: { orderNumber: '№ Заявки', equipmentName: 'Оборудование', workshopName: 'Цех', statusName: 'Статус', downtimeHours: 'Простой (ч)' },
        rows: res.items || []
      };
    }
    toast.success('Отчет успешно сформирован');
  } catch (e) {
    toast.error('Не удалось сформировать отчет');
  } finally {
    isGenerating.value = false;
  }
}

function handleExportExcel() {
  if (!reportData.value) return;
  exportTableToExcel(`toir-report-${reportType.value}`, reportData.value.rows, reportData.value.headers);
  toast.success('Экспорт Excel завершен');
}

function handleExportPDF() {
  if (!reportData.value) return;
  const headKeys = Object.keys(reportData.value.headers);
  const headNames = Object.values(reportData.value.headers);
  const body = reportData.value.rows.map(r => headKeys.map(k => String(r[k] ?? '')));

  exportReportToPDF(
    reportData.value.title,
    `Период: ${dateStart.value} — ${dateEnd.value} | Предприятие: ПМЗ | Система ТОиР-Проф`,
    headNames,
    body,
    `toir-report-${reportType.value}`
  );
  toast.success('Экспорт PDF сформирован');
}
</script>

<template>
  <div class="reports-page">
    <div class="page-header">
      <div>
        <h1 class="page-title">Генератор производственных отчётов</h1>
        <p class="page-sub">Формирование официальной регламентной документации и аналитических выгрузок</p>
      </div>
    </div>

    <!-- Параметры отчета (R163) -->
    <div class="card" style="margin-bottom: 24px;">
      <div class="card-header"><div class="card-title">Параметры формирования отчета</div></div>
      <div class="card-body">
        <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 16px; align-items: flex-end;">
          <div class="form-group" style="margin: 0;">
            <label class="form-label">Тип отчета</label>
            <select v-model="reportType" class="form-select">
              <option value="downtime">Анализ простоев оборудования</option>
              <option value="orders">Реестр выполнения заявок</option>
              <option value="materials">Складские остатки и ТМЦ</option>
              <option value="compliance">Выполнение графика ТО</option>
            </select>
          </div>
          <div class="form-group" style="margin: 0;">
            <label class="form-label">Дата начала</label>
            <input v-model="dateStart" type="date" class="form-input" />
          </div>
          <div class="form-group" style="margin: 0;">
            <label class="form-label">Дата окончания</label>
            <input v-model="dateEnd" type="date" class="form-input" />
          </div>
          <div>
            <button class="btn btn-primary" style="width: 100%;" @click="generateReport" :disabled="isGenerating">
              <span v-if="isGenerating" class="spinner"></span>
              <span>{{ isGenerating ? 'Формирование...' : '⚡ Сформировать отчет' }}</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- Результаты отчета -->
    <div v-if="reportData" class="card">
      <div class="card-header">
        <div class="card-title">{{ reportData.title }}</div>
        <div style="display: flex; gap: 8px;">
          <button class="btn btn-success btn-sm" @click="handleExportExcel">📥 Excel (.xlsx)</button>
          <button class="btn btn-danger btn-sm" @click="handleExportPDF">📑 PDF (.pdf)</button>
        </div>
      </div>
      <div class="card-body" style="padding: 0;">
        <div class="table-wrapper" style="border: none;">
          <table class="data-table">
            <thead>
              <tr>
                <th v-for="(name, key) in reportData.headers" :key="key">{{ name }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, idx) in reportData.rows" :key="idx">
                <td v-for="(name, key) in reportData.headers" :key="key">{{ row[key] }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>
  </div>
</template>
