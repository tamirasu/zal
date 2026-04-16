using System.Globalization;
using System.Xml.Linq;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public class XmlReportBuilder
{
    // Buduje raport wyłącznie przez LINQ to XML — bez XmlSerializer, bez sklejania stringów
    public XDocument BuildReport(IEnumerable<Order> orders)
    {
        var list = orders.ToList();

        var byStatus = list
            .GroupBy(o => o.Status)
            .OrderByDescending(g => g.Sum(o => o.TotalAmount))
            .Select(g => new XElement("status",
                new XAttribute("name", g.Key.ToString()),
                new XAttribute("count", g.Count()),
                new XAttribute("revenue", g.Sum(o => o.TotalAmount).ToString("F2", CultureInfo.InvariantCulture))));

        var byCustomer = list
            .GroupBy(o => o.Customer, o => o, (c, os) => (Customer: c, Orders: os.ToList()))
            .OrderByDescending(x => x.Orders.Sum(o => o.TotalAmount))
            .Select(x => new XElement("customer",
                new XAttribute("id", x.Customer.Id),
                new XAttribute("name", x.Customer.Name),
                new XAttribute("isVip", x.Customer.IsVip.ToString().ToLower()),
                new XElement("orderCount", x.Orders.Count),
                new XElement("totalSpent",
                    x.Orders.Sum(o => o.TotalAmount).ToString("F2", CultureInfo.InvariantCulture)),
                new XElement("orders",
                    x.Orders.Select(o => new XElement("orderRef",
                        new XAttribute("id", o.Id),
                        new XAttribute("total",
                            o.TotalAmount.ToString("F2", CultureInfo.InvariantCulture)))))));

        return new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("report",
                new XAttribute("generated", DateTime.Now.ToString("s")),
                new XElement("summary",
                    new XAttribute("totalOrders", list.Count),
                    new XAttribute("totalRevenue",
                        list.Sum(o => o.TotalAmount).ToString("F2", CultureInfo.InvariantCulture))),
                new XElement("byStatus", byStatus),
                new XElement("byCustomer", byCustomer)));
    }

    public async Task SaveReportAsync(XDocument report, string path)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
            bufferSize: 4096, useAsync: true);
        await report.SaveAsync(stream, SaveOptions.None, CancellationToken.None);
    }

    // Czyta raport z pliku XML i zwraca Id zamówień powyżej progu — tylko przez LINQ to XML
    public async Task<IEnumerable<int>> FindHighValueOrderIdsAsync(string reportPath, decimal threshold)
    {
        await using var stream = new FileStream(reportPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            bufferSize: 4096, useAsync: true);
        var doc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);

        return doc.Descendants("orderRef")
            .Where(e => decimal.Parse(
                e.Attribute("total")!.Value, CultureInfo.InvariantCulture) > threshold)
            .Select(e => int.Parse(e.Attribute("id")!.Value))
            .Distinct()
            .OrderBy(id => id)
            .ToList();
    }
}
