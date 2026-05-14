using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OrderFlow.Console.Services;

public class CurrencyService : ICurrencyService
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    // Bonus: cache kursów pobranych w danej sesji
    private readonly Dictionary<string, decimal> _cache =
        new(StringComparer.OrdinalIgnoreCase);

    public CurrencyService(HttpClient httpClient)
    {
        _http = httpClient;
    }

    public async Task<decimal?> GetRateAsync(string currencyCode)
    {
        if (string.Equals(currencyCode, "PLN", StringComparison.OrdinalIgnoreCase))
            return 1.0m;

        if (_cache.TryGetValue(currencyCode, out var cached))
            return cached;

        var url = $"/api/exchangerates/rates/A/{currencyCode}/?format=json";
        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url);
        }
        catch (HttpRequestException ex)
        {
            throw new CurrencyServiceException($"Błąd połączenia z NBP API: {ex.Message}");
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new CurrencyServiceException(
                $"NBP API zwróciło błąd: {(int)response.StatusCode} {response.ReasonPhrase}",
                (int)response.StatusCode);

        var json = await response.Content.ReadAsStringAsync();
        var dto = JsonSerializer.Deserialize<NbpResponse>(json, JsonOpts)
            ?? throw new CurrencyServiceException("Nieprawidłowa odpowiedź z NBP API");

        var rate = dto.Rates[0].Mid;
        _cache[currencyCode] = rate;
        return rate;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        var fromRate = await GetRateAsync(fromCurrency)
            ?? throw new CurrencyServiceException($"Nieznana waluta źródłowa: {fromCurrency}");
        var toRate = await GetRateAsync(toCurrency)
            ?? throw new CurrencyServiceException($"Nieznana waluta docelowa: {toCurrency}");

        // amount w fromCurrency → PLN → toCurrency
        return amount * fromRate / toRate;
    }

    private sealed record NbpResponse(
        [property: JsonPropertyName("rates")] NbpRateEntry[] Rates);

    private sealed record NbpRateEntry(
        [property: JsonPropertyName("mid")] decimal Mid);
}
