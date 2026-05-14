namespace OrderFlow.Console.Services;

public interface ICurrencyService
{
    /// <summary>
    /// Zwraca kurs średni waluty do PLN lub null jeśli waluta nie istnieje (404).
    /// GetRateAsync("PLN") zawsze zwraca 1.0m bez wywołania API.
    /// </summary>
    Task<decimal?> GetRateAsync(string currencyCode);

    /// <summary>
    /// Przelicza kwotę z jednej waluty na drugą przez PLN jako pośrednik.
    /// </summary>
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
}
