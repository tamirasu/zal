using OrderFlow.Console.Models;

namespace OrderFlow.Console.Data;

public static class SampleData
{
    public static List<Product> Products => new()
    {
        new Product { Id = 1, Name = "Laptop Dell XPS 15", Price = 5999.00m, Category = ProductCategory.Electronics, Description = "Laptop do pracy i gier", StockQuantity = 10 },
        new Product { Id = 2, Name = "Koszulka sportowa Nike", Price = 89.99m, Category = ProductCategory.Clothing, Description = "Oddychający materiał", StockQuantity = 50 },
        new Product { Id = 3, Name = "Kawa ziarnista 1kg", Price = 45.00m, Category = ProductCategory.Food, Description = "Arabica 100%", StockQuantity = 100 },
        new Product { Id = 4, Name = "Czysty kod — R. Martin", Price = 69.90m, Category = ProductCategory.Books, Description = "Klasyka programowania", StockQuantity = 30 },
        new Product { Id = 5, Name = "Rower górski Trek", Price = 3200.00m, Category = ProductCategory.Sports, Description = "29 cali, aluminium", StockQuantity = 5 },
        new Product { Id = 6, Name = "Słuchawki Sony WH-1000XM5", Price = 1299.00m, Category = ProductCategory.Electronics, Description = "ANC, 30h baterii", StockQuantity = 15 },
        new Product { Id = 7, Name = "Buty do biegania Asics", Price = 349.00m, Category = ProductCategory.Sports, Description = "Amortyzacja Gel", StockQuantity = 25 },
    };

    public static List<Customer> Customers => new()
    {
        new Customer { Id = 1, Name = "Anna Kowalska", Email = "anna.k@example.com", City = "Warszawa", IsVip = true,  CreditLimit = 20000m },
        new Customer { Id = 2, Name = "Piotr Nowak",   Email = "p.nowak@example.com",  City = "Kraków",   IsVip = false, CreditLimit = 5000m  },
        new Customer { Id = 3, Name = "Maria Wiśniewska", Email = "m.wisn@example.com", City = "Gdańsk",  IsVip = false, CreditLimit = 8000m  },
        new Customer { Id = 4, Name = "Tomasz Zając",  Email = "t.zajac@example.com",  City = "Warszawa", IsVip = true,  CreditLimit = 15000m },
    };

    public static List<Order> GetOrders()
    {
        var products = Products;
        var customers = Customers;

        var p1 = products[0]; var p2 = products[1]; var p3 = products[2];
        var p4 = products[3]; var p5 = products[4]; var p6 = products[5]; var p7 = products[6];

        var c1 = customers[0]; var c2 = customers[1];
        var c3 = customers[2]; var c4 = customers[3];

        return new List<Order>
        {
            // Order 1 — VIP, duże zamówienie
            new Order
            {
                Id = 1, CustomerId = c1.Id, Customer = c1,
                Status = OrderStatus.Completed,
                CreatedAt = new DateTime(2026, 2, 10),
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 1, ProductId = p1.Id, Product = p1, Quantity = 1, UnitPrice = p1.Price },
                    new OrderItem { Id = 2, ProductId = p6.Id, Product = p6, Quantity = 2, UnitPrice = p6.Price },
                }
            },
            // Order 2 — zwykły klient, mix
            new Order
            {
                Id = 2, CustomerId = c2.Id, Customer = c2,
                Status = OrderStatus.Processing,
                CreatedAt = new DateTime(2026, 2, 15),
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 3, ProductId = p2.Id, Product = p2, Quantity = 3, UnitPrice = p2.Price },
                    new OrderItem { Id = 4, ProductId = p4.Id, Product = p4, Quantity = 2, UnitPrice = p4.Price },
                    new OrderItem { Id = 5, ProductId = p3.Id, Product = p3, Quantity = 5, UnitPrice = p3.Price },
                }
            },
            // Order 3 — sport, Gdańsk
            new Order
            {
                Id = 3, CustomerId = c3.Id, Customer = c3,
                Status = OrderStatus.Validated,
                CreatedAt = new DateTime(2026, 3, 1),
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 6, ProductId = p5.Id, Product = p5, Quantity = 1, UnitPrice = p5.Price },
                    new OrderItem { Id = 7, ProductId = p7.Id, Product = p7, Quantity = 2, UnitPrice = p7.Price },
                }
            },
            // Order 4 — VIP, nowe zamówienie
            new Order
            {
                Id = 4, CustomerId = c4.Id, Customer = c4,
                Status = OrderStatus.New,
                CreatedAt = new DateTime(2026, 3, 5),
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 8, ProductId = p1.Id, Product = p1, Quantity = 2, UnitPrice = p1.Price },
                    new OrderItem { Id = 9, ProductId = p5.Id, Product = p5, Quantity = 1, UnitPrice = p5.Price },
                }
            },
            // Order 5 — anulowane
            new Order
            {
                Id = 5, CustomerId = c2.Id, Customer = c2,
                Status = OrderStatus.Cancelled,
                CreatedAt = new DateTime(2026, 3, 8),
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 10, ProductId = p3.Id, Product = p3, Quantity = 10, UnitPrice = p3.Price },
                }
            },
            // Order 6 — Warszawa, książki + elektronika
            new Order
            {
                Id = 6, CustomerId = c1.Id, Customer = c1,
                Status = OrderStatus.Validated,
                CreatedAt = new DateTime(2026, 3, 10),
                Items = new List<OrderItem>
                {
                    new OrderItem { Id = 11, ProductId = p4.Id, Product = p4, Quantity = 4, UnitPrice = p4.Price },
                    new OrderItem { Id = 12, ProductId = p6.Id, Product = p6, Quantity = 1, UnitPrice = p6.Price },
                }
            },
        };
    }
}
