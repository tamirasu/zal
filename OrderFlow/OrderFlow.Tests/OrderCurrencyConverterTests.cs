using Moq;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;
using Xunit;

namespace OrderFlow.Tests;

public class OrderCurrencyConverterTests
{
    private static Order MakeOrder(decimal totalAmount)
    {
        var product = new Product { Name = "Produkt", Price = totalAmount };
        return new Order
        {
            CustomerId = 1,
            Customer   = new Customer { Name = "Test" },
            Status     = OrderStatus.New,
            CreatedAt  = DateTime.Now,
            Items      = new List<OrderItem>
            {
                new() { Quantity = 1, UnitPrice = totalAmount, Product = product }
            }
        };
    }

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertOrderTotalAsync_USD_ReturnsConvertedAmount()
    {
        // Arrange — mockowanie ICurrencyService przez Moq (nie przez HttpMessageHandler!)
        var mockCurrency = new Mock<ICurrencyService>();
        mockCurrency.Setup(s => s.GetRateAsync("USD")).ReturnsAsync(4.0m);

        var converter = new OrderCurrencyConverter(mockCurrency.Object);
        var order     = MakeOrder(totalAmount: 800m);

        // Act
        var result = await converter.ConvertOrderTotalAsync(order, "USD");

        // Assert — 800 PLN / 4.0 = 200 USD
        Assert.Equal(200m, result);
    }

    // ─── Nieznana waluta zwraca null ──────────────────────────────────────────

    [Fact]
    public async Task ConvertOrderTotalAsync_UnknownCurrency_ReturnsNull()
    {
        // Arrange
        var mockCurrency = new Mock<ICurrencyService>();
        mockCurrency.Setup(s => s.GetRateAsync("XYZ")).ReturnsAsync((decimal?)null);

        var converter = new OrderCurrencyConverter(mockCurrency.Object);
        var order     = MakeOrder(totalAmount: 500m);

        // Act
        var result = await converter.ConvertOrderTotalAsync(order, "XYZ");

        // Assert
        Assert.Null(result);
    }

    // ─── Weryfikacja że serwis był wywołany z właściwą walutą ────────────────

    [Fact]
    public async Task ConvertOrderTotalAsync_EUR_CallsCurrencyServiceWithEUR()
    {
        // Arrange
        var mockCurrency = new Mock<ICurrencyService>();
        mockCurrency.Setup(s => s.GetRateAsync(It.IsAny<string>())).ReturnsAsync(4.3m);

        var converter = new OrderCurrencyConverter(mockCurrency.Object);
        var order     = MakeOrder(totalAmount: 430m);

        // Act
        await converter.ConvertOrderTotalAsync(order, "EUR");

        // Assert — upewnij się, że serwis był wywołany z "EUR"
        mockCurrency.Verify(s => s.GetRateAsync("EUR"), Times.Once);
    }
}
