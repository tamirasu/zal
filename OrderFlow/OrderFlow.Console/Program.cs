using OrderFlow.Console.Data;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;
using OrderFlow.Console.Services;
using OrderFlow.Console.Watchers;
using System.Diagnostics;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var products  = SampleData.Products;
var customers = SampleData.Customers;
var orders    = SampleData.GetOrders();

// =====================================================================
// LABORATORIUM 1
// =====================================================================

// ========== Zadanie 2: Walidacja ==========
Console.WriteLine("╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 2 — Walidacja zamówień      ║");
Console.WriteLine("╚══════════════════════════════════════╝");

var validator = new OrderValidator();

var goodOrder = orders.First(o => o.Id == 1);
var (isValid, errors) = validator.ValidateAll(goodOrder);
Console.WriteLine($"\nZamówienie #{goodOrder.Id} ({goodOrder.Customer.Name}): {(isValid ? "VALID ✓" : "INVALID ✗")}");
if (!isValid) errors.ForEach(e => Console.WriteLine($"  - {e}"));

var badOrder = new Order
{
    Id = 99,
    Customer = customers[1],
    CustomerId = customers[1].Id,
    Status = OrderStatus.Cancelled,
    CreatedAt = DateTime.Now.AddDays(5),
    Items = new List<OrderItem>
    {
        new OrderItem { Id = 99, Product = products[0], ProductId = products[0].Id,
            Quantity = -1, UnitPrice = products[0].Price }
    }
};

var (isValid2, errors2) = validator.ValidateAll(badOrder);
Console.WriteLine($"\nZamówienie #{badOrder.Id} (błędne): {(isValid2 ? "VALID ✓" : "INVALID ✗")}");
errors2.ForEach(e => Console.WriteLine($"  - {e}"));

// ========== Zadanie 3: OrderProcessor ==========
Console.WriteLine("\n╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 3 — Action/Func/Predicate   ║");
Console.WriteLine("╚══════════════════════════════════════╝");

var processor = new OrderProcessor(orders);
processor.DemoPredicates();
processor.DemoActions();
processor.DemoProjections();
processor.DemoAggregations();
processor.DemoChain();

// ========== Zadanie 4: LINQ ==========
Console.WriteLine("\n╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 4 — Zapytania LINQ           ║");
Console.WriteLine("╚══════════════════════════════════════╝");

LinqQueries.RunAll(orders, customers, products);

// =====================================================================
// LABORATORIUM 2
// =====================================================================
Console.WriteLine("\n\n╔══════════════════════════════════════════╗");
Console.WriteLine("║       LABORATORIUM 2                     ║");
Console.WriteLine("╚══════════════════════════════════════════╝");

// ========== Zadanie 1: Zdarzenia ==========
Console.WriteLine("\n╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 1 — Zdarzenia                ║");
Console.WriteLine("╚══════════════════════════════════════╝");

var pipeline = new OrderPipeline(new OrderValidator());

// Subskrybent 1: logger konsolowy — każda zmiana statusu
pipeline.StatusChanged += (_, e) =>
    Console.WriteLine($"  [LOG]   #{e.Order.Id} {e.OldStatus,-12} → {e.NewStatus,-12} @ {e.Timestamp:HH:mm:ss.fff}");

// Subskrybent 2: symulacja powiadomienia email — tylko przy finalizacji
pipeline.StatusChanged += (_, e) =>
{
    if (e.NewStatus == OrderStatus.Completed)
        Console.WriteLine($"  [EMAIL] Do: {e.Order.Customer.Email} — zamówienie #{e.Order.Id} zrealizowane!");
};

// Subskrybent 3: aktualizacja licznika przejść (statystyki)
int statusTransitions = 0;
pipeline.StatusChanged += (_, _) => Interlocked.Increment(ref statusTransitions);

// Logger walidacji
pipeline.ValidationCompleted += (_, e) =>
{
    var result = e.IsValid ? "OK ✓" : $"BŁĄD ✗ [{string.Join("; ", e.Errors)}]";
    Console.WriteLine($"  [VALID] #{e.Order.Id} → {result}");
};

// Przetwarzamy 3 zamówienia (świeże, status = New)
var pipelineOrders = SampleData.GetOrders().Take(3).ToList();
pipelineOrders.ForEach(o => o.Status = OrderStatus.New);

Console.WriteLine($"\nPrzetwarzanie {pipelineOrders.Count} zamówień przez pipeline:");
foreach (var o in pipelineOrders)
{
    Console.WriteLine($"\n--- Zamówienie #{o.Id} ({o.Customer.Name}, {o.TotalAmount:C}) ---");
    pipeline.ProcessOrder(o);
}
Console.WriteLine($"\nŁączna liczba przejść statusu: {statusTransitions}");

// ========== Zadanie 2: Async ==========
Console.WriteLine("\n╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 2 — Asynchroniczność         ║");
Console.WriteLine("╚══════════════════════════════════════╝");

var simulator      = new ExternalServiceSimulator();
var asyncProcessor = new AsyncOrderProcessor(simulator);
var demoOrder      = SampleData.GetOrders().First();

// Sekwencyjne przetwarzanie
Console.WriteLine("\n--- Przetwarzanie sekwencyjne (1 zamówienie) ---");
var seqMs = await asyncProcessor.ProcessOrderSequentialAsync(demoOrder);
Console.WriteLine($"  Czas sekwencyjny: {seqMs}ms");

// Równoległe przez Task.WhenAll
Console.WriteLine("\n--- Przetwarzanie równoległe Task.WhenAll (1 zamówienie) ---");
var swP = Stopwatch.StartNew();
await asyncProcessor.ProcessOrderAsync(demoOrder);
swP.Stop();
Console.WriteLine($"  Czas równoległy:  {swP.ElapsedMilliseconds}ms  (szybszy o ~{seqMs - swP.ElapsedMilliseconds}ms)");

// Wiele zamówień z SemaphoreSlim(3)
Console.WriteLine("\n--- Wiele zamówień równolegle (SemaphoreSlim max 3) ---");
var multiOrders = SampleData.GetOrders();
var swM = Stopwatch.StartNew();
await asyncProcessor.ProcessMultipleOrdersAsync(multiOrders);
swM.Stop();
Console.WriteLine($"\n  Łączny czas ({multiOrders.Count} zamówień, max 3 równolegle): {swM.ElapsedMilliseconds}ms");

// ========== Zadanie 3: Thread Safety ==========
Console.WriteLine("\n╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 3 — Thread Safety            ║");
Console.WriteLine("╚══════════════════════════════════════╝");

// 500 zamówień do stress testu
var baseOrders   = SampleData.GetOrders();
var stressOrders = Enumerable.Range(0, 500)
    .Select(i => baseOrders[i % baseOrders.Count])
    .ToList();

decimal expectedRevenue = stressOrders.Sum(o => o.TotalAmount);
int     expectedCount   = stressOrders.Count;
Console.WriteLine($"\nOczekiwana liczba: {expectedCount},  oczekiwany przychód: {expectedRevenue:C}");

// BEZ synchronizacji — wyniki powinny się różnić między przebiegami (lub wyjątek)
Console.WriteLine("\n[BEZ synchronizacji — 5 przebiegów (błędne dane)]:");
for (int i = 0; i < 5; i++)
{
    var bad = new OrderStatisticsUnsafe();
    try
    {
        Parallel.ForEach(stressOrders, order => bad.Update(order));
        bool countOk   = bad.TotalProcessed == expectedCount;
        bool revenueOk = bad.TotalRevenue   == expectedRevenue;
        Console.WriteLine($"  Run {i + 1}: count={bad.TotalProcessed,-5} {(countOk ? "✓" : "✗")}  " +
                          $"przychód={bad.TotalRevenue,14:C} {(revenueOk ? "✓" : "✗")}");
    }
    catch (AggregateException ex)
    {
        // Crash jest dowodem race condition — Dictionary nie jest thread-safe
        Console.WriteLine($"  Run {i + 1}: CRASH! {ex.InnerExceptions[0].Message}");
    }
}

// Z synchronizacją — wyniki zawsze identyczne
Console.WriteLine("\n[Z synchronizacją — 5 przebiegów (poprawne dane)]:");
for (int i = 0; i < 5; i++)
{
    var safe = new OrderStatistics();
    Parallel.ForEach(stressOrders, order => safe.Update(order));

    // Dodajemy błędy dla zamówień z wysoką kwotą (demonstracja AddError + lock)
    Parallel.ForEach(
        stressOrders.Where(o => o.TotalAmount > 10000m),
        order => safe.AddError($"Order #{order.Id}: kwota {order.TotalAmount:C} > próg"));

    bool countOk   = safe.TotalProcessed == expectedCount;
    bool revenueOk = safe.TotalRevenue   == expectedRevenue;
    Console.WriteLine($"  Run {i + 1}: count={safe.TotalProcessed,-5} {(countOk ? "✓" : "✗")}  " +
                      $"przychód={safe.TotalRevenue,14:C} {(revenueOk ? "✓" : "✗")}  " +
                      $"błędy={safe.ProcessingErrors.Count}");
}

// =====================================================================
// LABORATORIUM 3
// =====================================================================
Console.WriteLine("\n\n╔══════════════════════════════════════════╗");
Console.WriteLine("║       LABORATORIUM 3                     ║");
Console.WriteLine("╚══════════════════════════════════════════╝");

var repo    = new OrderRepository();
var builder = new XmlReportBuilder();
var lab3Orders = SampleData.GetOrders();

// ========== Zadanie 1: JSON + XML round-trip ==========
Console.WriteLine("\n╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 1 — JSON + XML round-trip    ║");
Console.WriteLine("╚══════════════════════════════════════╝");

const string jsonPath = "data/orders.json";
const string xmlPath  = "data/orders.xml";

// Zapis
await repo.SaveToJsonAsync(lab3Orders, jsonPath);
await repo.SaveToXmlAsync(lab3Orders, xmlPath);
Console.WriteLine($"\nZapisano {lab3Orders.Count} zamówień → {jsonPath}  i  {xmlPath}");

// Wczytaj z powrotem
var fromJson = await repo.LoadFromJsonAsync(jsonPath);
var fromXml  = await repo.LoadFromXmlAsync(xmlPath);

// Porównanie round-trip
decimal origTotal = lab3Orders.Sum(o => o.TotalAmount);
decimal jsonTotal = fromJson.Sum(o => o.TotalAmount);
decimal xmlTotal  = fromXml.Sum(o => o.TotalAmount);

Console.WriteLine($"\n  Oryginał:  count={lab3Orders.Count}  suma={origTotal:C}");
Console.WriteLine($"  Z JSON:    count={fromJson.Count}  suma={jsonTotal:C}  {(jsonTotal == origTotal ? "✓" : "✗")}");
Console.WriteLine($"  Z XML:     count={fromXml.Count}  suma={xmlTotal:C}  {(xmlTotal == origTotal ? "✓" : "✗")}");

// Test: brakujący plik → pusta lista (bez wyjątku)
var empty = await repo.LoadFromJsonAsync("data/nieistniejacy.json");
Console.WriteLine($"\n  Brakujący plik → lista: {empty.Count} elementów ✓");

// ========== Zadanie 2: LINQ to XML report ==========
Console.WriteLine("\n╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 2 — Raport XML (LINQ to XML) ║");
Console.WriteLine("╚══════════════════════════════════════╝");

const string reportPath = "data/report.xml";
var report = builder.BuildReport(lab3Orders);
await builder.SaveReportAsync(report, reportPath);
Console.WriteLine($"\nRaport zapisany → {reportPath}");

// Podgląd struktury (pierwsze 12 linii)
var reportLines = (await File.ReadAllTextAsync(reportPath)).Split('\n');
Console.WriteLine("  Podgląd (pierwsze 12 linii):");
foreach (var line in reportLines.Take(12))
    Console.WriteLine("  " + line.TrimEnd());

// FindHighValueOrderIds z pliku
var highValueIds = await builder.FindHighValueOrderIdsAsync(reportPath, 1000m);
Console.WriteLine($"\n  Zamówienia > 1000 zł (z pliku XML): [{string.Join(", ", highValueIds)}]");

// ========== Zadanie 3: InboxWatcher ==========
Console.WriteLine("\n╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 3 — InboxWatcher             ║");
Console.WriteLine("╚══════════════════════════════════════╝");

// Osobny pipeline z subskrybentami dla watcher demo
var inboxPipeline = new OrderPipeline(new OrderValidator());
inboxPipeline.StatusChanged += (_, e) =>
    Console.WriteLine($"  [PIPE]  #{e.Order.Id} {e.OldStatus} → {e.NewStatus}");
inboxPipeline.ValidationCompleted += (_, e) =>
    Console.WriteLine($"  [VALID] #{e.Order.Id} {(e.IsValid ? "OK ✓" : "FAIL ✗")}");

using var watcher = new InboxWatcher("inbox", inboxPipeline, repo);
Console.WriteLine("\n[INBOX] Watcher aktywny. Wrzucam 3 pliki testowe co 2 sekundy...\n");

for (int i = 1; i <= 3; i++)
{
    await Task.Delay(2000);

    // Każdy plik zawiera 2 zamówienia z unikalnymi Id
    var testOrders = SampleData.GetOrders()
        .Take(2)
        .Select((o, idx) => new Order
        {
            Id         = 1000 + i * 10 + idx,
            CustomerId = o.CustomerId,
            Customer   = o.Customer,
            Items      = o.Items,
            Status     = OrderStatus.New,
            CreatedAt  = DateTime.Now
        })
        .ToList();

    var inboxFile = Path.Combine("inbox", $"test_batch_{i}.json");
    await repo.SaveToJsonAsync(testOrders, inboxFile);
    Console.WriteLine($"[TEST]  Wrzucono: test_batch_{i}.json ({testOrders.Count} zamówienia)");
}

// Czekamy aż watcher przetworzy wszystkie pliki
await Task.Delay(4000);
Console.WriteLine("\n[INBOX] Demo zakończone.");

Console.WriteLine("\nDone.");
