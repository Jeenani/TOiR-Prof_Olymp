using System;
using ToirProf.Domain;
using Xunit;

namespace ToirProf.Tests;

public sealed class WorkOrderValidationTests : IDisposable
{
    private DateTime _now;
    private WorkOrderValidationInput _validOrder;

    public WorkOrderValidationTests()
    {
        // Setup чистого эталонного объекта заявки
        _now = new DateTime(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);
        _validOrder = new WorkOrderValidationInput
        {
            EquipmentId = 1,
            OrderType = "planned",
            PriorityId = 2,
            Description = "Плановое техническое обслуживание ТО-1 токарного станка",
            PlannedStartDate = _now.AddHours(1),
            PlannedEndDate = _now.AddHours(5)
        };
    }

    public void Dispose()
    {
        _validOrder = null!;
    }

    [Fact]
    public void ValidateWorkOrder_AllFieldsValid_ReturnsIsValidTrueWithNoErrors()
    {
        // Arrange
        var order = _validOrder;

        // Act
        var result = ToirCalculations.ValidateWorkOrder(order, _now);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateWorkOrder_MissingEquipmentOrEmptyDescription_ReturnsIsValidFalseWithErrors()
    {
        // Arrange
        _validOrder.EquipmentId = 0;
        _validOrder.Description = "   ";

        // Act
        var result = ToirCalculations.ValidateWorkOrder(_validOrder, _now);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("оборудование", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Errors, e => e.Contains("Описание", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateWorkOrder_EmergencyWithoutFailureReason_ReturnsRequiredReasonError()
    {
        // Arrange
        _validOrder.OrderType = "emergency";
        _validOrder.FailureReason = null;

        // Act
        var result = ToirCalculations.ValidateWorkOrder(_validOrder, _now);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Причина обязательна для аварийных заявок", result.Errors);
    }

    [Fact]
    public void ValidateWorkOrder_EndDateBeforeStartDate_ReturnsDateChronologyError()
    {
        // Arrange
        _validOrder.PlannedStartDate = _now.AddHours(5);
        _validOrder.PlannedEndDate = _now.AddHours(2);

        // Act
        var result = ToirCalculations.ValidateWorkOrder(_validOrder, _now);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("раньше даты начала", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidateWorkOrder_StartDateInPast_ReturnsPastDateError()
    {
        // Arrange
        _validOrder.PlannedStartDate = _now.AddHours(-2);
        _validOrder.PlannedEndDate = _now.AddHours(2);

        // Act
        var result = ToirCalculations.ValidateWorkOrder(_validOrder, _now);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("в прошлом", StringComparison.OrdinalIgnoreCase));
    }
}
