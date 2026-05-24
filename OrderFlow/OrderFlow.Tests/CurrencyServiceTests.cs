using System.Net;
using OrderFlow.Console.Services;
using OrderFlow.Tests.Helpers;
using Xunit;

namespace OrderFlow.Tests;

public class CurrencyServiceTests
{
    private static CurrencyService MakeService(TestHttpMessageHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.nbp.pl") });

    // ─── Happy path ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_ValidCurrency_ReturnsMidRate()
    {
        // Arrange
        var json    = TestHttpMessageHandler.BuildNbpJson("USD", 4.0m);
        var handler = TestHttpMessageHandler.RespondWithJson(json);
        var service = MakeService(handler);

        // Act
        var rate = await service.GetRateAsync("USD");

        // Assert
        Assert.Equal(4.0m, rate);
    }

    // ─── PLN specjalny przypadek ──────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_PLN_ReturnsOneWithoutCallingApi()
    {
        // Arrange
        var handler = TestHttpMessageHandler.RespondWith(HttpStatusCode.OK); // nie powinien być wywołany
        var service = MakeService(handler);

        // Act
        var rate = await service.GetRateAsync("PLN");

        // Assert
        Assert.Equal(1.0m, rate);
        Assert.Empty(handler.SentRequests); // API nie było wywołane
    }

    // ─── 404 nieznana waluta ──────────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_UnknownCurrency_ReturnsNull()
    {
        // Arrange
        var handler = TestHttpMessageHandler.RespondWith(HttpStatusCode.NotFound);
        var service = MakeService(handler);

        // Act
        var rate = await service.GetRateAsync("XYZ");

        // Assert
        Assert.Null(rate);
    }

    // ─── 500 Server Error ────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_ServerError_ThrowsCurrencyServiceException()
    {
        // Arrange
        var handler = TestHttpMessageHandler.RespondWith(HttpStatusCode.InternalServerError);
        var service = MakeService(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CurrencyServiceException>(
            () => service.GetRateAsync("USD"));
        Assert.Equal(500, ex.StatusCode);
    }

    // ─── Weryfikacja URL ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_ValidCurrency_SendsRequestToCorrectUrl()
    {
        // Arrange
        var json    = TestHttpMessageHandler.BuildNbpJson("EUR", 4.3m);
        var handler = TestHttpMessageHandler.RespondWithJson(json);
        var service = MakeService(handler);

        // Act
        await service.GetRateAsync("EUR");

        // Assert
        var sentUri = handler.SentRequests[0].RequestUri!.ToString();
        Assert.Contains("/api/exchangerates/rates/A/EUR/", sentUri);
    }

    // ─── ConvertAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ConvertAsync_UsdToEur_ReturnsCorrectAmount()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(req =>
        {
            var uri = req.RequestUri!.ToString();
            var json = uri.Contains("USD")
                ? TestHttpMessageHandler.BuildNbpJson("USD", 4.0m)
                : TestHttpMessageHandler.BuildNbpJson("EUR", 4.5m);
            return new System.Net.Http.HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new System.Net.Http.StringContent(
                    json, System.Text.Encoding.UTF8, "application/json")
            };
        });
        var service = MakeService(handler);

        // Act — 100 USD → PLN → EUR
        var result = await service.ConvertAsync(100m, "USD", "EUR");

        // Assert — 100 * 4.0 / 4.5 ≈ 88.89
        Assert.Equal(100m * 4.0m / 4.5m, result, precision: 6);
    }

    // ─── Bonus: cache ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_SameCurrencyTwice_ApiCalledOnlyOnce()
    {
        // Arrange
        var json    = TestHttpMessageHandler.BuildNbpJson("USD", 4.0m);
        var handler = TestHttpMessageHandler.RespondWithJson(json);
        var service = MakeService(handler);

        // Act
        await service.GetRateAsync("USD");
        await service.GetRateAsync("USD");

        // Assert — pomimo dwóch wywołań, tylko jeden request HTTP
        Assert.Single(handler.SentRequests);
    }
}
