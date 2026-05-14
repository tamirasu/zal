using OrderFlow.Console.Models;
using OrderFlow.Console.Services;
using Xunit;

namespace OrderFlow.Tests;

public class DiscountCalculatorTests
{
    private readonly DiscountCalculator _calc = new();

    // ─── Pomocnik ────────────────────────────────────────────────────────────
    private static Order CreateOrder(bool isVip, decimal totalAmount) => new()
    {
        CustomerId = 1,
        Status     = OrderStatus.New,
        CreatedAt  = DateTime.Now,
        Customer   = new Customer { Name = "Testowy", IsVip = isVip, CreditLimit = 99_999m },
        Items      = new List<OrderItem>
        {
            new() { ProductId = 1, Quantity = 1, UnitPrice = totalAmount,
                    Product   = new Product { Name = "Produkt", Price = totalAmount } }
        }
    };

    // ─── Reguła 1: klient standardowy, mała kwota → 0% ───────────────────────

    [Fact]
    public void CalculateDiscount_StandardCustomerSmallAmount_ReturnsZero()
    {
        // Arrange
        var order = CreateOrder(isVip: false, totalAmount: 500m);

        // Act
        var discount = _calc.CalculateDiscount(order);

        // Assert
        Assert.Equal(0m, discount);
    }

    // ─── Reguła 2: klient VIP → 10% ──────────────────────────────────────────

    [Fact]
    public void CalculateDiscount_VipCustomerSmallAmount_ReturnsTenPercent()
    {
        // Arrange
        var order = CreateOrder(isVip: true, totalAmount: 500m);

        // Act
        var discount = _calc.CalculateDiscount(order);

        // Assert — 10% z 500 = 50
        Assert.Equal(50m, discount);
    }
}
