using Microsoft.EntityFrameworkCore;
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

// =====================================================================
// LABORATORIUM 4
// =====================================================================
Console.WriteLine("\n\n╔══════════════════════════════════════════╗");
Console.WriteLine("║       LABORATORIUM 4 — EF Core           ║");
Console.WriteLine("╚══════════════════════════════════════════╝");

// ─── Init: migracja + seeding ────────────────────────────────────────
await using (var db0 = new OrderFlowContext())
{
    await db0.Database.MigrateAsync();
    await new DatabaseSeeder().SeedAsync(db0);
    Console.WriteLine("\nMigracje zastosowane. Baza zaseedowana.");

    var migs = db0.Database.GetAppliedMigrations().ToList();
    Console.WriteLine($"Zastosowane migracje ({migs.Count}):");
    migs.ForEach(m => Console.WriteLine($"  ✓ {m}"));
}

// ========== Zadanie 2: CRUD ==========
Console.WriteLine("\n╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 2 — CRUD                     ║");
Console.WriteLine("╚══════════════════════════════════════╝");

await using var db = new OrderFlowContext();

// CREATE — nowe zamówienie
var firstCustomer = await db.Customers.FirstAsync();
var firstProduct  = await db.Products.FirstAsync();
var secondProduct = await db.Products.Skip(2).FirstAsync();

var newOrder = new Order
{
    CustomerId = firstCustomer.Id,
    Status     = OrderStatus.New,
    CreatedAt  = DateTime.Now,
    Items      = new List<OrderItem>
    {
        new() { ProductId = firstProduct.Id,  Quantity = 1, UnitPrice = firstProduct.Price },
        new() { ProductId = secondProduct.Id, Quantity = 3, UnitPrice = secondProduct.Price }
    }
};
db.Orders.Add(newOrder);
await db.SaveChangesAsync();
Console.WriteLine($"\n  [CREATE] Dodano zamówienie #{newOrder.Id} z {newOrder.Items.Count} pozycjami dla {firstCustomer.Name}");

// READ — z Include Customer + Items.Product
var loaded = await db.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items).ThenInclude(i => i.Product)
    .OrderByDescending(o => o.Id)
    .Take(3)
    .ToListAsync();

Console.WriteLine($"\n  [READ] Ostatnie {loaded.Count} zamówienia:");
foreach (var o in loaded)
    Console.WriteLine($"    #{o.Id} {o.Customer.Name,-18} {o.Status,-12} " +
                      $"{o.Items.Sum(i => i.Quantity * i.UnitPrice),8:C} ({o.Items.Count} poz.)");

// UPDATE — zmień status + Notes
var toUpdate = await db.Orders.FirstAsync(o => o.Id == newOrder.Id);
toUpdate.Status = OrderStatus.Processing;
toUpdate.Notes  = "Zaktualizowano po CRUD demo";
await db.SaveChangesAsync();
Console.WriteLine($"\n  [UPDATE] Zamówienie #{toUpdate.Id}: status={toUpdate.Status}, notes=\"{toUpdate.Notes}\"");

// DELETE — zamówienie Cancelled
var toDelete = await db.Orders.FirstOrDefaultAsync(o => o.Status == OrderStatus.Cancelled);
if (toDelete != null)
{
    db.Orders.Remove(toDelete);
    await db.SaveChangesAsync();
    Console.WriteLine($"\n  [DELETE] Usunięto zamówienie #{toDelete.Id} (Cancelled)");
}

// Restrict demo — próba usunięcia klienta z zamówieniami
Console.WriteLine("\n  [RESTRICT] Próba usunięcia klienta z zamówieniami...");
var busyCustomer = await db.Customers
    .Include(c => c.Orders)
    .FirstAsync(c => c.Orders.Any());
try
{
    db.Customers.Remove(busyCustomer);
    await db.SaveChangesAsync();
}
catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException)
{
    // EF Core rzuca przed wysłaniem SQL — relacja Restrict chroni spójność danych
    Console.WriteLine($"    → DeleteBehavior.Restrict zadziałał ✓");
    Console.WriteLine($"      ({ex.GetType().Name}: {ex.Message[..Math.Min(90, ex.Message.Length)]}...)");
    db.ChangeTracker.Clear();   // reset stanu change trackera
}

// ========== Zadanie 3: Zapytania + Transakcja ==========
Console.WriteLine("\n╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 3 — Zapytania + Transakcja   ║");
Console.WriteLine("╚══════════════════════════════════════╝");

// Logging włączone dla tej sekcji — widać SQL
await using var dbLog = new OrderFlowContext(enableLogging: false); // set true for verbose SQL

// Q1: Zamówienia klientów VIP > próg
// IQueryable: filtrowanie na DB (Where IsVip + Include), agregacja w C# po załadowaniu
decimal threshold = 1500m;
var vipOrders = await dbLog.Orders                         // IQueryable<Order>
    .Where(o => o.Customer.IsVip)                          // SQL: WHERE IsVip=1
    .Include(o => o.Customer)
    .Include(o => o.Items)
    .ToListAsync();

var vipHighValue = vipOrders
    .Select(o => new { o.Id, CustomerName = o.Customer.Name,
        Total = o.Items.Sum(i => i.Quantity * i.UnitPrice), o.Status })
    .Where(x => x.Total > threshold)
    .OrderByDescending(x => x.Total)
    .ToList();

Console.WriteLine($"\n  Q1 — Zamówienia VIP > {threshold:C}:");
vipHighValue.ForEach(x =>
    Console.WriteLine($"    #{x.Id} {x.CustomerName,-18} {x.Total,8:C} [{x.Status}]"));

// Q2: Ranking klientów wg łącznej wartości
// IQueryable: GroupBy w SQL przez nawigację Customer, Sum w C# po załadowaniu
var allOrders = await dbLog.Orders                         // IQueryable<Order>
    .Include(o => o.Customer)
    .Include(o => o.Items)
    .OrderBy(o => o.CustomerId)                            // SQL ORDER BY
    .ToListAsync();

var customerRanking = allOrders
    .GroupBy(o => new { o.Customer.Id, o.Customer.Name, o.Customer.IsVip })
    .Select(g => new { g.Key.Name, g.Key.IsVip,
        TotalSpent = g.Sum(o => o.Items.Sum(i => i.Quantity * i.UnitPrice)) })
    .OrderByDescending(x => x.TotalSpent)
    .ToList();

Console.WriteLine("\n  Q2 — Ranking klientów:");
customerRanking.ForEach(x =>
    Console.WriteLine($"    {x.Name,-20} VIP:{x.IsVip}  wydał: {x.TotalSpent,9:C}"));

// Q3: Średnia wartość zamówienia per miasto
// IQueryable: Include ładuje potrzebne dane, agregacja per miasto w C#
var avgPerCity = allOrders                                  // reuse loaded IEnumerable
    .GroupBy(o => o.Customer.City)
    .Select(g => new { City = g.Key,
        AvgValue = g.Average(o => o.Items.Sum(i => i.Quantity * i.UnitPrice)) })
    .OrderByDescending(x => x.AvgValue)
    .ToList();

Console.WriteLine("\n  Q3 — Średnia wartość zamówienia per miasto:");
avgPerCity.ForEach(x => Console.WriteLine($"    {x.City,-12} śr: {x.AvgValue,8:C}"));

// Q4: Produkty nigdy nie zamówione (anti-join)
var unordered = await dbLog.Products
    .Where(p => !dbLog.OrderItems.Any(oi => oi.ProductId == p.Id))
    .Select(p => new { p.Id, p.Name, p.Category })
    .ToListAsync();

Console.WriteLine("\n  Q4 — Produkty bez zamówień (anti-join):");
if (unordered.Any())
    unordered.ForEach(p => Console.WriteLine($"    #{p.Id} {p.Name} [{p.Category}]"));
else
    Console.WriteLine("    (wszystkie produkty mają co najmniej jedno zamówienie)");

// Q5: Dynamiczne zapytanie (opcjonalny filtr statusu + kwota)
// IQueryable: filtrowanie statusu na DB; filtr kwoty + projekcja w pamięci (SQLite nie tłumaczy Sum)
OrderStatus? dynStatus = OrderStatus.Completed;
decimal dynMinAmount   = 500m;

IQueryable<Order> dynQuery = dbLog.Orders               // IQueryable<Order>
    .Include(o => o.Customer)
    .Include(o => o.Items);
if (dynStatus.HasValue)
    dynQuery = dynQuery.Where(o => o.Status == dynStatus.Value); // SQL WHERE Status=X

var dynRaw = await dynQuery.ToListAsync();

var dynResult = dynRaw
    .Select(o => new { o.Id, Name = o.Customer.Name,
        Total = o.Items.Sum(i => i.Quantity * i.UnitPrice) })
    .Where(x => dynMinAmount <= 0 || x.Total > dynMinAmount)    // filtr kwoty w pamięci
    .OrderByDescending(x => x.Total)
    .ToList();

Console.WriteLine($"\n  Q5 — Dynamiczne: status={dynStatus}, min={dynMinAmount:C}:");
dynResult.ForEach(x => Console.WriteLine($"    #{x.Id} {x.Name,-18} {x.Total,8:C}"));

// Transakcja — sukces (tworzymy świeże zamówienie w statusie New)
Console.WriteLine("\n  [TXN] Scenariusz sukcesu:");
var txOrder = new Order
{
    CustomerId = firstCustomer.Id,
    Status     = OrderStatus.New,
    CreatedAt  = DateTime.Now,
    Items      = new List<OrderItem>
    {
        new() { ProductId = firstProduct.Id,  Quantity = 1, UnitPrice = firstProduct.Price },
        new() { ProductId = secondProduct.Id, Quantity = 1, UnitPrice = secondProduct.Price }
    }
};
db.Orders.Add(txOrder);
await db.SaveChangesAsync();
await ProcessOrderWithTransactionAsync(db, txOrder.Id);

// Transakcja — porażka (niski stock)
Console.WriteLine("\n  [TXN] Scenariusz porażki (celowo zerujemy stock):");
var productToDeplete = await db.Products.FirstAsync();
productToDeplete.StockQuantity = 0;
await db.SaveChangesAsync();

// Nowe zamówienie do testu
var failOrder = new Order
{
    CustomerId = firstCustomer.Id,
    Status     = OrderStatus.New,
    CreatedAt  = DateTime.Now,
    Items      = new List<OrderItem>
    {
        new() { ProductId = productToDeplete.Id, Quantity = 5, UnitPrice = productToDeplete.Price }
    }
};
db.Orders.Add(failOrder);
await db.SaveChangesAsync();

try
{
    await ProcessOrderWithTransactionAsync(db, failOrder.Id);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"    → Rollback: {ex.Message}");
}

Console.WriteLine("\nDone.");

// ─── Lokalna metoda transakcji ────────────────────────────────────────
static async Task ProcessOrderWithTransactionAsync(OrderFlowContext ctx, int orderId)
{
    await using var tx = await ctx.Database.BeginTransactionAsync();
    try
    {
        var order = await ctx.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Include(o => o.Customer)
            .FirstOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException($"Brak zamówienia #{orderId}");

        if (order.Status != OrderStatus.New)
            throw new InvalidOperationException($"Zamówienie #{orderId} nie jest w statusie New");

        // New → Processing
        order.Status = OrderStatus.Processing;
        await ctx.SaveChangesAsync();

        // Sprawdź stany magazynowe
        foreach (var item in order.Items)
        {
            if (item.Product.StockQuantity < item.Quantity)
                throw new InvalidOperationException(
                    $"Brak towaru: {item.Product.Name} (dostępne: {item.Product.StockQuantity}, wymagane: {item.Quantity})");
        }

        // Zmniejsz stany
        foreach (var item in order.Items)
            item.Product.StockQuantity -= item.Quantity;

        // Processing → Completed
        order.Status = OrderStatus.Completed;
        await ctx.SaveChangesAsync();

        await tx.CommitAsync();
        Console.WriteLine($"    → Zamówienie #{orderId} przetworzone (Completed). Stany zaktualizowane.");
    }
    catch
    {
        await tx.RollbackAsync();
        throw;
    }
}
