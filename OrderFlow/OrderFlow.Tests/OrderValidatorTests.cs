using OrderFlow.Console.Models;
using OrderFlow.Console.Services;
using Xunit;

namespace OrderFlow.Tests;

public class OrderValidatorTests
{
    private readonly OrderValidator _validator = new();

    // ─── Pomocnik ────────────────────────────────────────────────────────────
    private static Order MakeValidOrder() => new()
    {
        Id         = 1,
        CustomerId = 1,
        Customer   = new Customer { Name = "Jan Kowalski", CreditLimit = 99_999m },
        Status     = OrderStatus.New,
        CreatedAt  = DateTime.Now.AddHours(-1),
        Items      = new List<OrderItem>
        {
            new() { Quantity = 2, UnitPrice = 100m,
                    Product = new Product { Name = "Widget", Price = 100m } }
        }
    };

    // ─── Named method: HasItems ───────────────────────────────────────────────

    [Fact]
    public void ValidateAll_NoItems_ReturnsInvalidWithHasItemsError()
    {
        // Arrange
        var order = MakeValidOrder();
        order.Items.Clear();

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("żadnych pozycji"));
    }

    // ─── Named method: AmountUnderLimit ──────────────────────────────────────

    [Fact]
    public void ValidateAll_AmountOverLimit_ReturnsInvalidWithLimitError()
    {
        // Arrange
        var order = MakeValidOrder();
        order.Items[0].UnitPrice = 60_000m; // TotalAmount = 120_000 > 50_000 limit

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("przekracza limit"));
    }

    // ─── Named method: AllQuantitiesPositive ─────────────────────────────────

    [Fact]
    public void ValidateAll_NegativeQuantity_ReturnsInvalidWithQuantityError()
    {
        // Arrange
        var order = MakeValidOrder();
        order.Items[0].Quantity = -1;

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("nieprawidłową ilość"));
    }

    // ─── Lambda rule: date not in future ─────────────────────────────────────

    [Fact]
    public void ValidateAll_FutureCreatedAt_ReturnsInvalidWithDateError()
    {
        // Arrange
        var order = MakeValidOrder();
        order.CreatedAt = DateTime.Now.AddDays(1);

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("przyszłości"));
    }

    // ─── Lambda rule: status not Cancelled ───────────────────────────────────

    [Fact]
    public void ValidateAll_CancelledStatus_ReturnsInvalidWithStatusError()
    {
        // Arrange
        var order = MakeValidOrder();
        order.Status = OrderStatus.Cancelled;

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Cancelled"));
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public void ValidateAll_ValidOrder_ReturnsValidWithNoErrors()
    {
        // Arrange
        var order = MakeValidOrder();

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.True(isValid);
        Assert.Empty(errors);
    }

    // ─── Multiple violations ──────────────────────────────────────────────────

    [Fact]
    public void ValidateAll_MultipleViolations_ReturnsAllErrors()
    {
        // Arrange
        var order = MakeValidOrder();
        order.Items.Clear();                           // HasItems fails
        order.Status     = OrderStatus.Cancelled;      // lambda fails
        order.CreatedAt  = DateTime.Now.AddDays(2);    // lambda fails

        // Act
        var (isValid, errors) = _validator.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.True(errors.Count >= 3, $"Oczekiwano ≥3 błędów, otrzymano {errors.Count}");
    }

    // ─── Theory: różne statusy ────────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.New,        true)]
    [InlineData(OrderStatus.Validated,  true)]
    [InlineData(OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Completed,  true)]
    [InlineData(OrderStatus.Cancelled,  false)]
    public void ValidateAll_VariousStatuses_ReturnsExpectedValidity(
        OrderStatus status, bool expectedValid)
    {
        // Arrange
        var order = MakeValidOrder();
        order.Status = status;

        // Act
        var (isValid, _) = _validator.ValidateAll(order);

        // Assert
        Assert.Equal(expectedValid, isValid);
    }
}
