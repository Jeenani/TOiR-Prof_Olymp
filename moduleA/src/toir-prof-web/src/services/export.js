import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import * as XLSX from 'xlsx';

export function exportTableToCSV(filename, rows, headers) {
  const headerKeys = Object.keys(headers);
  const headerNames = Object.values(headers);

  let csvContent = '\uFEFF' + headerNames.join(';') + '\n';

  rows.forEach(row => {
    const values = headerKeys.map(k => {
      let val = row[k] ?? '';
      if (typeof val === 'string' && val.includes(';')) {
        val = `"${val}"`;
      }
      return val;
    });
    csvContent += values.join(';') + '\n';
  });

  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  link.href = URL.createObjectURL(blob);
  link.setAttribute('download', `${filename}.csv`);
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

export function exportTableToExcel(filename, rows, headers) {
  const headerKeys = Object.keys(headers);
  const data = rows.map(r => {
    const obj = {};
    for (const k of headerKeys) {
      obj[headers[k]] = r[k] ?? '';
    }
    return obj;
  });

  const ws = XLSX.utils.json_to_sheet(data);
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Отчет');
  XLSX.writeFile(wb, `${filename}.xlsx`);
}

export function exportChartToPNG(canvasId, filename) {
  const canvas = document.getElementById(canvasId);
  if (!canvas) {
    console.error(`Canvas с id '${canvasId}' не найден`);
    return;
  }
  const imgUrl = canvas.toDataURL('image/png');
  const link = document.createElement('a');
  link.href = imgUrl;
  link.download = `${filename}.png`;
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
}

export function exportReportToPDF(title, subtitle, tableHeaders, tableData, filename) {
  const doc = new jsPDF();

  doc.setFontSize(16);
  doc.text(title, 14, 20);

  if (subtitle) {
    doc.setFontSize(10);
    doc.setTextColor(100);
    doc.text(subtitle, 14, 28);
  }

  autoTable(doc, {
    startY: subtitle ? 34 : 26,
    head: [tableHeaders],
    body: tableData,
    theme: 'grid',
    styles: { fontSize: 9, cellPadding: 3 },
    headStyles: { fillColor: [41, 128, 185], textColor: 255 }
  });

  doc.save(`${filename}.pdf`);
}
