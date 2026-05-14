using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderCurrencyConverter
{
    private readonly ICurrencyService _currency;

    public OrderCurrencyConverter(ICurrencyService currencyService)
    {
        _currency = currencyService;
    }

    /// <summary>
    /// Przelicza TotalAmount zamówienia (PLN) na podaną walutę.
    /// Zwraca null, jeśli waluta jest nieznana.
    /// </summary>
    public async Task<decimal?> ConvertOrderTotalAsync(Order order, string targetCurrency)
    {
        var rate = await _currency.GetRateAsync(targetCurrency);
        if (rate is null or 0m) return null;
        return Math.Round(order.TotalAmount / rate.Value, 2);
    }
}
