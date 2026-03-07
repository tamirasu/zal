using OrderFlow.Console.Data;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

Console.OutputEncoding = System.Text.Encoding.UTF8;

var products  = SampleData.Products;
var customers = SampleData.Customers;
var orders    = SampleData.GetOrders();

// ========== Zadanie 2: Walidacja ==========
Console.WriteLine("╔══════════════════════════════════════╗");
Console.WriteLine("║  ZADANIE 2 — Walidacja zamówień      ║");
Console.WriteLine("╚══════════════════════════════════════╝");

var validator = new OrderValidator();

// Zamówienie poprawne
var goodOrder = orders.First(o => o.Id == 1);
var (isValid, errors) = validator.ValidateAll(goodOrder);
Console.WriteLine($"\nZamówienie #{goodOrder.Id} ({goodOrder.Customer.Name}): {(isValid ? "VALID ✓" : "INVALID ✗")}");
if (!isValid) errors.ForEach(e => Console.WriteLine($"  - {e}"));

// Zamówienie łamiące reguły (ręcznie skonstruowane)
var badOrder = new Order
{
    Id = 99,
    Customer = customers[1],
    CustomerId = customers[1].Id,
    Status = OrderStatus.Cancelled,
    CreatedAt = DateTime.Now.AddDays(5),   // data z przyszłości
    Items = new List<OrderItem>
    {
        new OrderItem { Id = 99, Product = products[0], ProductId = products[0].Id,
            Quantity = -1, UnitPrice = products[0].Price }  // ujemna ilość
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

Console.WriteLine("\nDone.");
