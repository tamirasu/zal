using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    public decimal CalculateDiscount(Order order)
        => Math.Min(CalculateRate(order), 0.25m) * order.TotalAmount;

    protected virtual decimal CalculateRate(Order order)
    {
        var rate = 0m;
        if (order.Customer.IsVip)
            rate += 0.10m;
        if (order.TotalAmount > 1000m)
            rate += 0.05m;
        if (order.Customer.IsVip && order.TotalAmount > 5000m)
            rate += 0.05m;
        return rate;
    }
}
