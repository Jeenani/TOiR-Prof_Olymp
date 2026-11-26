using System;
using ToirProf.Domain;
using Xunit;

namespace ToirProf.Tests;

public sealed class DowntimeCalculationTests : IDisposable
{
    private DateTime _baseStart;

    public DowntimeCalculationTests()
    {
        // Setup начального состояния перед каждым тестом
        _baseStart = new DateTime(2026, 11, 20, 8, 0, 0, DateTimeKind.Utc);
    }

    public void Dispose()
    {
        // Сброс состояния после каждого теста
        _baseStart = default;
    }

    [Fact]
    public void CalculateDowntimeHours_ValidDates_ReturnsExactHours()
    {
        // Arrange
        var start = _baseStart;
        var end = _baseStart.AddHours(4).AddMinutes(30);

        // Act
        var result = ToirCalculations.CalculateDowntimeHours(start, end);

        // Assert
        Assert.Equal(4.50m, result);
    }

    [Fact]
    public void CalculateDowntimeHours_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        // Arrange
        var start = _baseStart;
        var end = _baseStart.AddHours(-2);

        // Act
        Action action = () => ToirCalculations.CalculateDowntimeHours(start, end);

        // Assert
        var ex = Assert.Throws<ArgumentException>(action);
        Assert.Contains("раньше даты начала", ex.Message);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void CalculateDowntimeHours_NullDates_ThrowsArgumentException(bool startIsNull, bool endIsNull)
    {
        // Arrange
        DateTime? start = startIsNull ? null : _baseStart;
        DateTime? end = endIsNull ? null : _baseStart.AddHours(1);

        // Act
        Action action = () => ToirCalculations.CalculateDowntimeHours(start, end);

        // Assert
        var ex = Assert.Throws<ArgumentException>(action);
        Assert.Contains("null", ex.Message);
    }

    [Fact]
    public void CalculateDowntimeHours_DefaultDates_ThrowsArgumentException()
    {
        // Arrange
        DateTime? start = default(DateTime);
        DateTime? end = _baseStart;

        // Act
        Action action = () => ToirCalculations.CalculateDowntimeHours(start, end);

        // Assert
        var ex = Assert.Throws<ArgumentException>(action);
        Assert.Contains("некорректная дата", ex.Message);
    }
}
