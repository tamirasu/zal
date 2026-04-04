using OrderFlow.Console.Models;
using System.Diagnostics;

namespace OrderFlow.Console.Services;

public class AsyncOrderProcessor
{
    private readonly ExternalServiceSimulator _sim;

    public AsyncOrderProcessor(ExternalServiceSimulator sim)
    {
        _sim = sim;
    }

    // Równoległe wywołanie wszystkich serwisów przez Task.WhenAll
    public async Task ProcessOrderAsync(Order order)
    {
        var sw = Stopwatch.StartNew();

        var inventoryTask = _sim.CheckInventoryAsync(order.Items.First().Product);
        var paymentTask   = _sim.ValidatePaymentAsync(order);
        var shippingTask  = _sim.CalculateShippingAsync(order);

        await Task.WhenAll(inventoryTask, paymentTask, shippingTask);
        sw.Stop();

        var (payOk, payMsg) = paymentTask.Result;
        System.Console.WriteLine(
            $"  #{order.Id,-2} [{order.Customer.Name,-18}] " +
            $"magazyn={inventoryTask.Result,-5} " +
            $"płatność={payOk} ({payMsg}), " +
            $"wysyłka={shippingTask.Result:C} | {sw.ElapsedMilliseconds}ms");
    }

    // Sekwencyjne wywołanie — do porównania czasu
    public async Task<long> ProcessOrderSequentialAsync(Order order)
    {
        var sw = Stopwatch.StartNew();
        await _sim.CheckInventoryAsync(order.Items.First().Product);
        await _sim.ValidatePaymentAsync(order);
        await _sim.CalculateShippingAsync(order);
        sw.Stop();
        return sw.ElapsedMilliseconds;
    }

    // Przetwarza wiele zamówień równolegle, max 3 jednocześnie (SemaphoreSlim)
    public async Task ProcessMultipleOrdersAsync(List<Order> orders)
    {
        var semaphore = new SemaphoreSlim(3);
        int processed  = 0;
        int total      = orders.Count;

        var tasks = orders.Select(async order =>
        {
            await semaphore.WaitAsync();
            try
            {
                await ProcessOrderAsync(order);
                int count = Interlocked.Increment(ref processed);
                System.Console.WriteLine($"  >>> Przetworzono {count}/{total} zamówień");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}
