using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    public decimal CalculateDiscount(Order order)
        => CalculateRate(order) * order.TotalAmount;

    protected virtual decimal CalculateRate(Order order) => 0m;
}
