import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import html2canvas from 'html2canvas';
import * as XLSX from 'xlsx';

function triggerDownload(url, filename) {
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

export function exportTableToCSV(filename, rows, headers) {
  const keys = Object.keys(headers);
  const lines = [Object.values(headers).join(';')];
  for (const row of rows) {
    lines.push(keys.map(k => {
      const v = String(row[k] ?? '');
      return v.includes(';') ? `"${v}"` : v;
    }).join(';'));
  }
  const blob = new Blob(['\uFEFF' + lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
  triggerDownload(URL.createObjectURL(blob), `${filename}.csv`);
}

export function exportTableToExcel(filename, rows, headers) {
  const keys = Object.keys(headers);
  const data = rows.map(r => Object.fromEntries(keys.map(k => [headers[k], r[k] ?? ''])));
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, XLSX.utils.json_to_sheet(data), 'Отчет');
  XLSX.writeFile(wb, `${filename}.xlsx`);
}

export function exportChartToPNG(canvasId, filename) {
  const canvas = document.getElementById(canvasId);
  if (canvas) triggerDownload(canvas.toDataURL('image/png'), `${filename}.png`);
}

export function exportReportToPDF(title, subtitle, tableHeaders, tableData, filename) {
  const doc = new jsPDF();
  doc.setFontSize(15);
  doc.text(title, 14, 18);
  if (subtitle) {
    doc.setFontSize(10);
    doc.setTextColor(100);
    doc.text(subtitle, 14, 26);
  }
  autoTable(doc, {
    startY: subtitle ? 32 : 24,
    head: [tableHeaders],
    body: tableData,
    theme: 'grid',
    styles: { fontSize: 9, cellPadding: 3 },
    headStyles: { fillColor: [2, 132, 199], textColor: 255 }
  });
  doc.save(`${filename}.pdf`);
}

export async function exportPageToPDF(selector, filename) {
  const el = document.querySelector(selector);
  if (!el) return;
  const canvas = await html2canvas(el, { backgroundColor: '#0f172a', scale: 1.5 });
  const doc = new jsPDF('l', 'mm', 'a4');
  const w = doc.internal.pageSize.getWidth();
  const h = (canvas.height * w) / canvas.width;
  doc.addImage(canvas.toDataURL('image/png'), 'PNG', 0, 5, w, Math.min(h, 200));
  doc.save(`${filename}.pdf`);
}

