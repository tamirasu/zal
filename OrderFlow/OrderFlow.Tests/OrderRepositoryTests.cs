using OrderFlow.Console.Data;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;
using Xunit;

namespace OrderFlow.Tests;

public class OrderRepositoryTests : IDisposable
{
    private readonly string _tmpDir = Path.Combine(Path.GetTempPath(), "orderflow_tests_" + Guid.NewGuid().ToString("N"));

    public OrderRepositoryTests() => Directory.CreateDirectory(_tmpDir);

    public void Dispose()
    {
        if (Directory.Exists(_tmpDir))
            Directory.Delete(_tmpDir, recursive: true);
    }

    // ─── JSON round-trip ──────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAndLoadJson_RoundTrip_PreservesOrderCountAndIds()
    {
        // Arrange
        var repo   = new OrderRepository();
        var orders = SampleData.GetOrders();
        var path   = Path.Combine(_tmpDir, "orders.json");

        // Act
        await repo.SaveToJsonAsync(orders, path);
        var loaded = await repo.LoadFromJsonAsync(path);

        // Assert
        Assert.Equal(orders.Count, loaded.Count);
        Assert.Equal(orders[0].Id, loaded[0].Id);
    }

    [Fact]
    public async Task LoadFromJson_MissingFile_ReturnsEmptyList()
    {
        // Arrange
        var repo = new OrderRepository();
        var path = Path.Combine(_tmpDir, "nonexistent.json");

        // Act
        var result = await repo.LoadFromJsonAsync(path);

        // Assert
        Assert.Empty(result);
    }

    // ─── XML round-trip ───────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAndLoadXml_RoundTrip_PreservesOrderCount()
    {
        // Arrange
        var repo   = new OrderRepository();
        var orders = SampleData.GetOrders();
        var path   = Path.Combine(_tmpDir, "orders.xml");

        // Act
        await repo.SaveToXmlAsync(orders, path);
        var loaded = await repo.LoadFromXmlAsync(path);

        // Assert
        Assert.Equal(orders.Count, loaded.Count);
    }

    [Fact]
    public async Task LoadFromXml_MissingFile_ReturnsEmptyList()
    {
        // Arrange
        var repo = new OrderRepository();
        var path = Path.Combine(_tmpDir, "nonexistent.xml");

        // Act
        var result = await repo.LoadFromXmlAsync(path);

        // Assert
        Assert.Empty(result);
    }
}
