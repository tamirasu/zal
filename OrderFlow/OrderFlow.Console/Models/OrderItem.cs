using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

[XmlType("orderItem")]
public class OrderItem
{
    [XmlAttribute("itemId")]
    public int Id { get; set; }

    public int ProductId { get; set; }

    [XmlElement("product")]
    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    [XmlIgnore]
    public decimal TotalPrice => Quantity * UnitPrice;
}
