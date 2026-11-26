using System;
using System.Collections.Generic;
using ToirProf.Domain;
using Xunit;

namespace ToirProf.Tests;

public sealed class MaterialCostCalculationTests : IDisposable
{
    private List<MaterialCostItem> _materials;

    public MaterialCostCalculationTests()
    {
        // Setup тестового набора материалов (подшипник, ремень, масло = 7300.00)
        _materials = new List<MaterialCostItem>
        {
            new() { Name = "Подшипник SKF 6205", Quantity = 2m, UnitPrice = 1500.00m },
            new() { Name = "Ремень клиновой B-1250", Quantity = 3m, UnitPrice = 800.00m },
            new() { Name = "Масло индустриальное И-40А", Quantity = 1m, UnitPrice = 1900.00m }
        };
    }

    public void Dispose()
    {
        _materials.Clear();
    }

    [Fact]
    public void CalculateTotalMaterialCost_MultipleMaterials_ReturnsExactTotal7300()
    {
        // Arrange
        var items = _materials;

        // Act
        var total = ToirCalculations.CalculateTotalMaterialCost(items);

        // Assert
        Assert.Equal(7300.00m, total);
    }

    [Fact]
    public void CalculateTotalMaterialCost_EmptyList_ReturnsZero()
    {
        // Arrange
        var items = new List<MaterialCostItem>();

        // Act
        var total = ToirCalculations.CalculateTotalMaterialCost(items);

        // Assert
        Assert.Equal(0m, total);
    }

    [Fact]
    public void CalculateTotalMaterialCost_ItemWithZeroQuantity_DoesNotAffectSum()
    {
        // Arrange
        _materials.Add(new MaterialCostItem { Name = "Сальник армированный", Quantity = 0m, UnitPrice = 950.00m });

        // Act
        var total = ToirCalculations.CalculateTotalMaterialCost(_materials);

        // Assert
        Assert.Equal(7300.00m, total);
    }

    [Theory]
    [InlineData(-1, 500)]
    [InlineData(2, -100)]
    public void CalculateTotalMaterialCost_NegativeQuantityOrPrice_ThrowsArgumentException(decimal qty, decimal price)
    {
        // Arrange
        var items = new List<MaterialCostItem>
        {
            new() { Name = "Тестовый расходник", Quantity = qty, UnitPrice = price }
        };

        // Act
        Action action = () => ToirCalculations.CalculateTotalMaterialCost(items);

        // Assert
        var ex = Assert.Throws<ArgumentException>(action);
        Assert.Contains("отрицательным", ex.Message.Replace("отрицательной", "отрицательным"));
    }
}
