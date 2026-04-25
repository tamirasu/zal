using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Data;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public class DatabaseSeeder
{
    public async Task SeedAsync(OrderFlowContext db)
    {
        if (await db.Products.AnyAsync()) return;  // już zaseedowane

        // 1. Produkty i klienci najpierw — żeby dostały Id z bazy
        var products = SampleData.Products;
        db.Products.AddRange(products);
        await db.SaveChangesAsync();

        var customers = SampleData.Customers;
        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();

        // 2. Zamówienia — tylko FK (bez nawigacji, żeby nie duplikować encji)
        var sampleOrders = SampleData.GetOrders();
        foreach (var o in sampleOrders)
        {
            var order = new Order
            {
                Id         = o.Id,
                CustomerId = o.CustomerId,
                Status     = o.Status,
                CreatedAt  = o.CreatedAt,
                Items      = o.Items.Select(i => new OrderItem
                {
                    ProductId = i.ProductId,
                    Quantity  = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };
            db.Orders.Add(order);
        }
        await db.SaveChangesAsync();
    }
}
