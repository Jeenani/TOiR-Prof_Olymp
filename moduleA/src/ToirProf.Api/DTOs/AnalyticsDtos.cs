namespace ToirProf.Api.DTOs;

public class DashboardKpiDto
{
    // 6 ключевых карточек KPI (по критерию Web: R146 и Desktop: R106)
    public int TotalEquipment { get; set; }
    public int ActiveWorkOrders { get; set; }
    public int CompletedWorkOrders { get; set; }
    public decimal TotalDowntimeHours { get; set; }
    public decimal AverageRepairHours { get; set; }
    public decimal MaintenanceComplianceRate { get; set; } // % выполнения графика ТО

    // Круговые / столбчатые диаграммы
    public List<EquipmentStatusStatDto> EquipmentByStatus { get; set; } = new();
    public List<WorkshopWorkOrderStatDto> OrdersByWorkshop { get; set; } = new();
    
    // Топ-5 простоев
    public List<TopDowntimeEquipmentDto> Top5Downtime { get; set; } = new();

    // Таблица 10 последних заявок и блок нарушений
    public List<WorkOrderItemDto> RecentWorkOrders { get; set; } = new();
    public List<ViolationAlertDto> Violations { get; set; } = new();
}

public class EquipmentStatusStatDto
{
    public string StatusName { get; set; } = string.Empty;
    public string StatusCode { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class WorkshopWorkOrderStatDto
{
    public int WorkshopId { get; set; }
    public string WorkshopName { get; set; } = string.Empty;
    public int ActiveOrders { get; set; }
    public int CompletedOrders { get; set; }
}

public class TopDowntimeEquipmentDto
{
    public int EquipmentId { get; set; }
    public string InventoryNumber { get; set; } = string.Empty;
    public string EquipmentName { get; set; } = string.Empty;
    public string WorkshopName { get; set; } = string.Empty;
    public decimal DowntimeHours { get; set; }
}

public class ViolationAlertDto
{
    public string ViolationType { get; set; } = string.Empty; // OverdueSchedule, SLAExceeded, LowStock
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "warning"; // warning, danger
}

public class WorkshopSummaryReportDto
{
    public string MetricName { get; set; } = string.Empty;
    public decimal MetricValue { get; set; }
    public string Unit { get; set; } = string.Empty;
}
