using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    public const  decimal MaxDiscountRate          = 0.25m;
    private const decimal VipDiscountRate          = 0.10m;
    private const decimal HighValueDiscountRate    = 0.05m;
    private const decimal VipHighValueDiscountRate = 0.05m;
    private const decimal HighValueThreshold       = 1_000m;
    private const decimal VipHighValueThreshold    = 5_000m;

    public decimal CalculateDiscount(Order order)
        => Math.Min(CalculateRate(order), MaxDiscountRate) * order.TotalAmount;

    protected virtual decimal CalculateRate(Order order)
        => VipBonus(order.Customer)
         + HighValueBonus(order)
         + VipHighValueBonus(order);

    private static decimal VipBonus(Customer c)
        => c.IsVip ? VipDiscountRate : 0m;

    private static decimal HighValueBonus(Order o)
        => o.TotalAmount > HighValueThreshold ? HighValueDiscountRate : 0m;

    private static decimal VipHighValueBonus(Order o)
        => o.Customer.IsVip && o.TotalAmount > VipHighValueThreshold
            ? VipHighValueDiscountRate : 0m;
}
