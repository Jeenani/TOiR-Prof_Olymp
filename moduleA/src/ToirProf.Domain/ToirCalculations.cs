using System;
using System.Collections.Generic;
using System.Linq;

namespace ToirProf.Domain;

public class WorkOrderValidationInput
{
    public int EquipmentId { get; set; }
    public string OrderType { get; set; } = "planned";
    public int PriorityId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? FailureReason { get; set; }
    public DateTime PlannedStartDate { get; set; }
    public DateTime PlannedEndDate { get; set; }
}

public class WorkOrderValidationResult
{
    public bool IsValid => Errors.Count == 0;
    public List<string> Errors { get; } = new();
}

public class MaterialCostItem
{
    public string Name { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public static class ToirCalculations
{
    public static decimal CalculateDowntimeHours(DateTime? startDate, DateTime? endDate)
    {
        if (!startDate.HasValue || !endDate.HasValue)
            throw new ArgumentException("Дата начала и дата окончания простоя не могут быть null");

        if (startDate.Value == default || endDate.Value == default)
            throw new ArgumentException("Указана некорректная дата простоя");

        if (endDate.Value < startDate.Value)
            throw new ArgumentException("Дата окончания не может быть раньше даты начала");

        var hours = (decimal)(endDate.Value - startDate.Value).TotalHours;
        return Math.Round(hours, 2);
    }

    public static WorkOrderValidationResult ValidateWorkOrder(WorkOrderValidationInput? order, DateTime? referenceNow = null)
    {
        var result = new WorkOrderValidationResult();
        if (order == null)
        {
            result.Errors.Add("Объект заявки не может быть пустым");
            return result;
        }

        if (order.EquipmentId <= 0)
            result.Errors.Add("Необходимо выбрать оборудование");

        if (order.PriorityId <= 0)
            result.Errors.Add("Необходимо указать приоритет заявки");

        if (string.IsNullOrWhiteSpace(order.Description))
            result.Errors.Add("Описание дефекта обязательно для заполнения");

        if (string.Equals(order.OrderType, "emergency", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(order.FailureReason))
        {
            result.Errors.Add("Причина обязательна для аварийных заявок");
        }

        var now = referenceNow ?? DateTime.UtcNow;
        if (order.PlannedStartDate < now.AddMinutes(-5))
            result.Errors.Add("Плановая дата начала не может быть в прошлом");

        if (order.PlannedEndDate < order.PlannedStartDate)
            result.Errors.Add("Плановая дата окончания не может быть раньше даты начала");

        return result;
    }

    public static decimal CalculateTotalMaterialCost(IEnumerable<MaterialCostItem>? items)
    {
        if (items == null)
            return 0m;

        var itemList = items.ToList();
        if (itemList.Count == 0)
            return 0m;

        decimal total = 0m;
        foreach (var item in itemList)
        {
            if (item.Quantity < 0)
                throw new ArgumentException($"Количество материала '{item.Name}' не может быть отрицательным");
            if (item.UnitPrice < 0)
                throw new ArgumentException($"Цена материала '{item.Name}' не может быть отрицательной");

            total += item.Quantity * item.UnitPrice;
        }

        return Math.Round(total, 2);
    }
}
