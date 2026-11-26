using System;
using System.Collections.Generic;

namespace ToirProf.Desktop.Models;

public class DashboardKpiDto
{
    public int TotalEquipment { get; set; }
    public int ActiveWorkOrders { get; set; }
    public int CompletedWorkOrders { get; set; }
    public decimal TotalDowntimeHours { get; set; }
    public decimal AverageRepairHours { get; set; }
    public decimal MaintenanceComplianceRate { get; set; }
    public List<EquipmentStatusStatDto> EquipmentByStatus { get; set; } = new();
    public List<WorkshopWorkOrderStatDto> OrdersByWorkshop { get; set; } = new();
    public List<TopDowntimeEquipmentDto> Top5Downtime { get; set; } = new();
    public List<WorkOrderItemDto> RecentWorkOrders { get; set; } = new();
    public List<ViolationAlertDto> Violations { get; set; } = new();
}

public class EquipmentStatusStatDto
{
    public string StatusName { get; set; } = "";
    public string StatusCode { get; set; } = "";
    public int Count { get; set; }
}

public class WorkshopWorkOrderStatDto
{
    public int WorkshopId { get; set; }
    public string WorkshopName { get; set; } = "";
    public int ActiveOrders { get; set; }
    public int CompletedOrders { get; set; }
}

public class TopDowntimeEquipmentDto
{
    public int EquipmentId { get; set; }
    public string InventoryNumber { get; set; } = "";
    public string EquipmentName { get; set; } = "";
    public string WorkshopName { get; set; } = "";
    public decimal DowntimeHours { get; set; }
}

public class ViolationAlertDto
{
    public string ViolationType { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "warning";
}

public class WorkshopSummaryReportDto
{
    public string MetricName { get; set; } = "";
    public decimal MetricValue { get; set; }
    public string Unit { get; set; } = "";
}

public class DesktopSettings
{
    public string ApiBaseUrl { get; set; } = "http://localhost:5000";
    public bool RememberMe { get; set; }
    public string? SavedToken { get; set; }
    public string? SavedEmail { get; set; }
    public string ThemeMode { get; set; } = "Light";
    public double FontSize { get; set; } = 14;
    public bool ShowSerialColumn { get; set; } = true;
    public bool ShowManufacturerColumn { get; set; } = true;
    public bool ShowTeamColumn { get; set; } = true;
    public List<ReportTemplate> ReportTemplates { get; set; } = new();
}

public class ReportTemplate
{
    public string Name { get; set; } = "";
    public string ReportType { get; set; } = "downtime";
    public int WorkshopId { get; set; }
    public int PeriodDays { get; set; } = 30;
}

public class OfflineWorkOrderDraft
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public CreateWorkOrderDto Payload { get; set; } = new();
    public string EquipmentLabel { get; set; } = "";
    public string? LastError { get; set; }
}

public class ReportRowItem
{
    public string Col1 { get; set; } = "";
    public string Col2 { get; set; } = "";
    public string Col3 { get; set; } = "";
    public string Col4 { get; set; } = "";
    public double NumericValue { get; set; }
}
