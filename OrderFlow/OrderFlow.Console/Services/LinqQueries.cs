using OrderFlow.Console.Data;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public static class LinqQueries
{
    public static void RunAll(List<Order> orders, List<Customer> customers, List<Product> products)
    {
        Query1_JoinOrdersWithCustomersByCity(orders, customers);
        Query2_SelectManyItemsToProducts(orders);
        Query3_GroupByCustomerTopAmount(orders);
        Query4_AvgValueByCategory(orders);
        Query5_GroupJoinLeftPattern(customers, orders);
        Query6_MixedSyntaxFavoriteCategory(orders, customers);
    }

    // Query 1 (method syntax) — Join zamówień z klientami, grupowanie po mieście
    // Method syntax wybrany bo join + group w jednym łańcuchu jest czytelniejszy niż query syntax
    private static void Query1_JoinOrdersWithCustomersByCity(List<Order> orders, List<Customer> customers)
    {
        System.Console.WriteLine("\n--- Q1: Zamówienia pogrupowane po mieście klienta (method syntax) ---");

        var result = orders
            .Join(customers,
                o => o.CustomerId,
                c => c.Id,
                (o, c) => new { Order = o, City = c.City })
            .GroupBy(x => x.City)
            .Select(g => new
            {
                City = g.Key,
                OrderCount = g.Count(),
                TotalAmount = g.Sum(x => x.Order.TotalAmount)
            })
            .OrderByDescending(x => x.TotalAmount);

        foreach (var r in result)
            System.Console.WriteLine($"  {r.City,-12} | zamówień: {r.OrderCount} | łącznie: {r.TotalAmount:C}");
    }

    // Query 2 (method syntax) — SelectMany: Order → OrderItems → informacja o produkcie
    // SelectMany spłaszcza kolekcje zagnieżdżone, method syntax bardziej naturalny
    private static void Query2_SelectManyItemsToProducts(List<Order> orders)
    {
        System.Console.WriteLine("\n--- Q2: Wszystkie pozycje zamówień (SelectMany, method syntax) ---");

        var items = orders
            .SelectMany(o => o.Items, (o, item) => new
            {
                OrderId = o.Id,
                CustomerName = o.Customer.Name,
                ProductName = item.Product.Name,
                Category = item.Product.Category,
                Qty = item.Quantity,
                LineTotal = item.TotalPrice
            })
            .OrderBy(x => x.OrderId);

        foreach (var i in items)
            System.Console.WriteLine($"  Zam.#{i.OrderId} | {i.CustomerName,-18} | {i.ProductName,-25} | {i.Qty}x | {i.LineTotal:C}");
    }

    // Query 3 (query syntax) — GroupBy: top klientów według sumarycznej kwoty zamówień
    // Query syntax wybrany bo group...by...into jest bardziej czytelny przy agregacji per klucz
    private static void Query3_GroupByCustomerTopAmount(List<Order> orders)
    {
        System.Console.WriteLine("\n--- Q3: Top klienci wg kwoty (GroupBy, query syntax) ---");

        var result =
            from o in orders
            where o.Status != OrderStatus.Cancelled
            group o by o.Customer.Name into g
            let total = g.Sum(o => o.TotalAmount)
            orderby total descending
            select new { Customer = g.Key, TotalSpent = total, OrderCount = g.Count() };

        foreach (var r in result)
            System.Console.WriteLine($"  {r.Customer,-20} | zamówień: {r.OrderCount} | wydał: {r.TotalSpent:C}");
    }

    // Query 4 (query syntax) — GroupBy: średnia wartość zamówienia per kategoria produktu
    // Query syntax — let clause pozwala nazwać pośredni krok agregacji
    private static void Query4_AvgValueByCategory(List<Order> orders)
    {
        System.Console.WriteLine("\n--- Q4: Średnia wartość pozycji per kategoria (query syntax) ---");

        var result =
            from o in orders
            from item in o.Items
            group item by item.Product.Category into g
            let avg = g.Average(i => i.TotalPrice)
            orderby avg descending
            select new { Category = g.Key, AvgLineValue = avg, Count = g.Count() };

        foreach (var r in result)
            System.Console.WriteLine($"  {r.Category,-15} | śr. wartość pozycji: {r.AvgLineValue:C} | pozycji: {r.Count}");
    }

    // Query 5 (method syntax) — GroupJoin (left join): wszyscy klienci + ich zamówienia (lub brak)
    // GroupJoin w method syntax jest bezpośrednim odpowiednikiem LEFT JOIN z SQL
    private static void Query5_GroupJoinLeftPattern(List<Customer> customers, List<Order> orders)
    {
        System.Console.WriteLine("\n--- Q5: Wszyscy klienci + ich zamówienia (GroupJoin / left join) ---");

        var result = customers
            .GroupJoin(orders,
                c => c.Id,
                o => o.CustomerId,
                (c, customerOrders) => new
                {
                    CustomerName = c.Name,
                    City = c.City,
                    IsVip = c.IsVip,
                    OrderCount = customerOrders.Count(),
                    TotalSpent = customerOrders.Sum(o => o.TotalAmount)
                })
            .OrderByDescending(x => x.TotalSpent);

        foreach (var r in result)
            System.Console.WriteLine($"  {r.CustomerName,-20} [{r.City,-10}] VIP:{r.IsVip} | zam.: {r.OrderCount} | wydał: {r.TotalSpent:C}");
    }

    // Query 6 (mixed syntax) — raport per klient z ulubioną kategorią produktów
    // Zewnętrzna pętla w query syntax, wewnętrzna agregacja (favourite category) w method syntax
    private static void Query6_MixedSyntaxFavoriteCategory(List<Order> orders, List<Customer> customers)
    {
        System.Console.WriteLine("\n--- Q6: Ulubiona kategoria per klient (mixed syntax) ---");

        // Query syntax — iteracja po klientach z grupowaniem zamówień
        var report =
            from c in customers
            join o in orders on c.Id equals o.CustomerId into customerOrders
            where customerOrders.Any()
            let allItems = customerOrders.SelectMany(o => o.Items)
            // Method syntax — najczęstsza kategoria wewnątrz query syntax
            let favoriteCategory = allItems
                .GroupBy(i => i.Product.Category)
                .OrderByDescending(g => g.Sum(i => i.Quantity))
                .First().Key
            select new
            {
                CustomerName = c.Name,
                IsVip = c.IsVip,
                TotalOrders = customerOrders.Count(),
                FavoriteCategory = favoriteCategory,
                TotalSpent = customerOrders.Sum(o => o.TotalAmount)
            };

        foreach (var r in report)
            System.Console.WriteLine($"  {r.CustomerName,-20} VIP:{r.IsVip} | ulub. kat.: {r.FavoriteCategory,-15} | wydał: {r.TotalSpent:C}");
    }
}
