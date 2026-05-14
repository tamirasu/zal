using OrderFlow.Console.Data;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;
using Xunit;

namespace OrderFlow.Tests;

public class OrderProcessorTests
{
    private static List<Order> SampleOrders() => SampleData.GetOrders();

    // ─── Filter ───────────────────────────────────────────────────────────────

    [Fact]
    public void Filter_CancelledPredicate_ReturnsOnlyCancelledOrders()
    {
        // Arrange
        var processor = new OrderProcessor(SampleOrders());
        Predicate<Order> isCancelled = o => o.Status == OrderStatus.Cancelled;

        // Act
        var result = processor.Filter(isCancelled);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, o => Assert.Equal(OrderStatus.Cancelled, o.Status));
    }

    [Fact]
    public void Filter_VipPredicate_ReturnsOnlyVipOrders()
    {
        // Arrange
        var processor = new OrderProcessor(SampleOrders());
        Predicate<Order> isVip = o => o.Customer.IsVip;

        // Act
        var result = processor.Filter(isVip);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, o => Assert.True(o.Customer.IsVip));
    }

    // ─── Aggregate ────────────────────────────────────────────────────────────

    [Fact]
    public void Aggregate_SumFunction_ReturnsTotalSumOfAllOrders()
    {
        // Arrange
        var orders = SampleOrders();
        var processor = new OrderProcessor(orders);
        Func<IEnumerable<Order>, decimal> sumFn = os => os.Sum(o => o.TotalAmount);
        var expected = orders.Sum(o => o.TotalAmount);

        // Act
        var result = processor.Aggregate(sumFn);

        // Assert
        Assert.Equal(expected, result);
    }

    // ─── Project ──────────────────────────────────────────────────────────────

    [Fact]
    public void Project_AmountSelector_ReturnsAmountForEveryOrder()
    {
        // Arrange
        var orders = SampleOrders();
        var processor = new OrderProcessor(orders);
        Func<Order, decimal> toAmount = o => o.TotalAmount;

        // Act
        var result = processor.Project(toAmount);

        // Assert
        Assert.Equal(orders.Count, result.Count);
        Assert.Equal(orders[0].TotalAmount, result[0]);
        Assert.All(result, a => Assert.True(a >= 0m));
    }
}
