using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderProcessor
{
    private readonly List<Order> _orders;

    public OrderProcessor(List<Order> orders)
    {
        _orders = orders;
    }

    // --- Predicate<Order> --- filtrowanie

    public List<Order> Filter(Predicate<Order> predicate)
        => _orders.FindAll(predicate);

    public void DemoPredicates()
    {
        System.Console.WriteLine("\n=== Predicate<Order> — filtry ===");

        // Predykat 1: tylko aktywne (nie anulowane)
        Predicate<Order> isActive = o => o.Status != OrderStatus.Cancelled;
        var active = Filter(isActive);
        System.Console.WriteLine($"Aktywne zamówienia: {active.Count}");

        // Predykat 2: duże zamówienia (> 2000 zł)
        Predicate<Order> isHighValue = o => o.TotalAmount > 2000m;
        var highValue = Filter(isHighValue);
        System.Console.WriteLine($"Zamówienia powyżej 2000 zł: {highValue.Count}");

        // Predykat 3: zamówienia klientów VIP
        Predicate<Order> isVipOrder = o => o.Customer.IsVip;
        var vipOrders = Filter(isVipOrder);
        System.Console.WriteLine($"Zamówienia VIP: {vipOrders.Count}");
    }

    // --- Action<Order> --- akcje na zamówieniach

    public void Apply(IEnumerable<Order> orders, Action<Order> action)
    {
        foreach (var o in orders) action(o);
    }

    public void DemoActions()
    {
        System.Console.WriteLine("\n=== Action<Order> ===");

        // Action 1: wypisanie zamówienia
        Action<Order> printOrder = o =>
            System.Console.WriteLine($"  [#{o.Id}] Klient: {o.Customer.Name,-20} Status: {o.Status,-12} Kwota: {o.TotalAmount,8:C}");

        System.Console.WriteLine("Wszystkie zamówienia:");
        Apply(_orders, printOrder);

        // Action 2: zmiana statusu New → Validated
        Action<Order> validateOrder = o =>
        {
            if (o.Status == OrderStatus.New)
            {
                o.Status = OrderStatus.Validated;
                System.Console.WriteLine($"  Zamówienie #{o.Id} zmieniono status: New → Validated");
            }
        };

        System.Console.WriteLine("\nWalidacja zamówień New:");
        Apply(_orders.Where(o => o.Status == OrderStatus.New).ToList(), validateOrder);
    }

    // --- Func<Order, T> --- projekcja na dowolny typ

    public List<TResult> Project<TResult>(Func<Order, TResult> selector)
        => _orders.Select(selector).ToList();

    public void DemoProjections()
    {
        System.Console.WriteLine("\n=== Func<Order, T> — projekcje ===");

        // Projekcja na typ anonimowy (przez object z boxing)
        var summaries = _orders.Select(o => new
        {
            OrderId = o.Id,
            CustomerName = o.Customer.Name,
            Total = o.TotalAmount,
            ItemCount = o.Items.Count
        }).ToList();

        System.Console.WriteLine("Podsumowanie zamówień:");
        foreach (var s in summaries)
            System.Console.WriteLine($"  #{s.OrderId} {s.CustomerName,-20} | {s.ItemCount} poz. | {s.Total,8:C}");

        // Projekcja na decimal (kwota)
        Func<Order, decimal> toAmount = o => o.TotalAmount;
        var amounts = Project(toAmount);
        System.Console.WriteLine($"\nKwoty: {string.Join(", ", amounts.Select(a => a.ToString("C")))}");
    }

    // --- Agregacja z Func<IEnumerable<Order>, decimal> ---

    public decimal Aggregate(Func<IEnumerable<Order>, decimal> aggregator)
        => aggregator(_orders);

    public void DemoAggregations()
    {
        System.Console.WriteLine("\n=== Agregacje ===");

        Func<IEnumerable<Order>, decimal> sum   = orders => orders.Sum(o => o.TotalAmount);
        Func<IEnumerable<Order>, decimal> avg   = orders => orders.Average(o => o.TotalAmount);
        Func<IEnumerable<Order>, decimal> max   = orders => orders.Max(o => o.TotalAmount);

        System.Console.WriteLine($"  Suma:    {Aggregate(sum),10:C}");
        System.Console.WriteLine($"  Średnia: {Aggregate(avg),10:C}");
        System.Console.WriteLine($"  Max:     {Aggregate(max),10:C}");
    }

    // --- Łańcuch: filtruj → sortuj → top N → wypisz ---

    public void DemoChain()
    {
        System.Console.WriteLine("\n=== Łańcuch: filtruj → sortuj → top 3 → wypisz ===");

        Predicate<Order> activeAndValuable = o =>
            o.Status != OrderStatus.Cancelled && o.TotalAmount > 500m;

        Func<Order, decimal> sortKey = o => o.TotalAmount;

        Action<Order> print = o =>
            System.Console.WriteLine($"  #{o.Id} {o.Customer.Name,-20} {o.TotalAmount,8:C} [{o.Status}]");

        var top3 = Filter(activeAndValuable)
            .OrderByDescending(sortKey)
            .Take(3)
            .ToList();

        System.Console.WriteLine($"Top 3 aktywne zamówienia powyżej 500 zł:");
        Apply(top3, print);
    }
}
