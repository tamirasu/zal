using OrderFlow.Console.Models;
using System.Collections.Concurrent;

namespace OrderFlow.Console.Services;

// Wersja BEZ synchronizacji — demonstracja problemu wyścigu
public class OrderStatisticsUnsafe
{
    public int TotalProcessed;
    public decimal TotalRevenue;
    public Dictionary<OrderStatus, int> OrdersPerStatus = new();
    public List<string> ProcessingErrors = new();

    public void Update(Order order)
    {
        TotalProcessed++;                                                             // race condition: brak atomowości
        TotalRevenue += order.TotalAmount;                                            // race condition: read-modify-write
        var s = order.Status;
        OrdersPerStatus[s] = OrdersPerStatus.GetValueOrDefault(s) + 1;              // race condition: słownik nie jest thread-safe
    }
}

// Wersja BEZPIECZNA — lock + Interlocked + ConcurrentDictionary
public class OrderStatistics
{
    private int     _totalProcessed;
    private decimal _totalRevenue;

    private readonly object _revenueLock = new();
    private readonly object _errorsLock  = new();

    public ConcurrentDictionary<OrderStatus, int> OrdersPerStatus = new();
    public List<string> ProcessingErrors = new();

    public void Update(Order order)
    {
        // Interlocked.Increment — atomowy inkrement licznika
        Interlocked.Increment(ref _totalProcessed);

        // lock — decimal nie obsługuje Interlocked, więc używamy sekcji krytycznej
        lock (_revenueLock)
            _totalRevenue += order.TotalAmount;

        // ConcurrentDictionary — thread-safe aktualizacja słownika
        OrdersPerStatus.AddOrUpdate(order.Status, 1, (_, count) => count + 1);
    }

    public void AddError(string error)
    {
        // lock — List<T> nie jest thread-safe
        lock (_errorsLock)
            ProcessingErrors.Add(error);
    }

    public int     TotalProcessed => _totalProcessed;
    public decimal TotalRevenue   => _totalRevenue;
}
