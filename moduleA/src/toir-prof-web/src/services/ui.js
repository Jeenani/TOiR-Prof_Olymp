import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

export function drawChart(canvasId, prevChart, config) {
  const el = document.getElementById(canvasId);
  if (!el) return prevChart;
  if (prevChart) prevChart.destroy();

  const isPie = config.type === 'doughnut' || config.type === 'pie';
  const baseOptions = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: isPie ? 'right' : 'top',
        labels: { color: '#94a3b8', font: { size: 11 } },
        ...(config.options?.plugins?.legend || {})
      },
      ...(config.options?.plugins || {})
    }
  };

  if (!isPie) {
    baseOptions.scales = {
      x: { ticks: { color: '#94a3b8' }, grid: { color: '#334155' } },
      y: { ticks: { color: '#94a3b8' }, grid: { color: '#334155' } },
      ...(config.options?.scales || {})
    };
  }

  return new Chart(el, {
    type: config.type,
    data: config.data,
    options: { ...baseOptions, ...(config.options || {}), plugins: baseOptions.plugins }
  });
}

export const fmtDateTime = (s) => (s ? String(s).slice(0, 16).replace('T', ' ') : '—');
export const orderBadge = (id) => (id >= 5 ? 'badge-success' : id === 3 ? 'badge-warning' : 'badge-info');
export const equipBadge = (id) => (id === 1 ? 'badge-success' : id === 2 ? 'badge-warning' : 'badge-danger');
