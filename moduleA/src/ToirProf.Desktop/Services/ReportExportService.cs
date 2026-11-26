using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;
using ToirProf.Desktop.Models;

namespace ToirProf.Desktop.Services;

public static class ReportExportService
{
    public static void ExportExcel(
        string filePath,
        string reportTitle,
        string subtitle,
        string[] headers,
        List<ReportRowItem> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("Отчёт ТОиР-Проф");

        ws.Cell(1, 1).Value = reportTitle;
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 14;
        ws.Cell(2, 1).Value = subtitle;
        ws.Cell(2, 1).Style.Font.Italic = true;

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(4, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#1E3A8A");
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        }

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r];
            var vals = new[] { row.Col1, row.Col2, row.Col3, row.Col4 };
            for (int c = 0; c < headers.Length && c < vals.Length; c++)
            {
                var cell = ws.Cell(5 + r, c + 1);
                cell.Value = vals[c];
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
        }

        ws.Columns().AdjustToContents();
        wb.SaveAs(filePath);
    }

    public static void ExportPdf(
        string filePath,
        string reportTitle,
        string subtitle,
        string[] headers,
        List<ReportRowItem> rows)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text("АИС «ТОиР-Проф» — Отчётный документ").FontSize(9).FontColor(Colors.Grey.Darken1);
                    col.Item().Text(reportTitle).FontSize(16).Bold().FontColor(Colors.Blue.Darken3);
                    col.Item().Text(subtitle).FontSize(10).Italic().FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingVertical(12).Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        for (int i = 0; i < headers.Length; i++)
                            cols.RelativeColumn(i == 0 ? 2 : 1);
                    });

                    table.Header(header =>
                    {
                        foreach (var h in headers)
                        {
                            header.Cell().Background(Colors.Blue.Darken3).Padding(6)
                                .Text(h).Bold().FontColor(Colors.White);
                        }
                    });

                    foreach (var row in rows)
                    {
                        var vals = new[] { row.Col1, row.Col2, row.Col3, row.Col4 };
                        for (int c = 0; c < headers.Length && c < vals.Length; c++)
                        {
                            table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).Padding(6)
                                .Text(vals[c]);
                        }
                    }
                });

                page.Footer().AlignRight().Text(x =>
                {
                    x.Span($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm} | Стр. ");
                    x.CurrentPageNumber();
                });
            });
        }).GeneratePdf(filePath);
    }
}
