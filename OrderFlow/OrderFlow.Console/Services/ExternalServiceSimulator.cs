using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class ExternalServiceSimulator
{
    // Symulacja sprawdzenia stanu magazynowego (500–1500ms)
    public async Task<bool> CheckInventoryAsync(Product product)
    {
        await Task.Delay(Random.Shared.Next(500, 1501));
        return product.StockQuantity > 0;
    }

    // Symulacja walidacji płatności (1000–2000ms)
    public async Task<(bool IsValid, string Message)> ValidatePaymentAsync(Order order)
    {
        await Task.Delay(Random.Shared.Next(1000, 2001));
        bool ok = order.TotalAmount <= order.Customer.CreditLimit;
        return (ok, ok
            ? "Płatność zatwierdzona"
            : $"Przekroczony limit kredytowy {order.Customer.CreditLimit:C}");
    }

    // Symulacja wyliczenia kosztów wysyłki (300–800ms)
    public async Task<decimal> CalculateShippingAsync(Order order)
    {
        await Task.Delay(Random.Shared.Next(300, 801));
        return order.TotalAmount >= 500m ? 0m : 15m + order.Items.Count * 3m;
    }
}
