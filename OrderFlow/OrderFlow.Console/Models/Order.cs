using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

[XmlType("order")]
[XmlRoot("order")]
public class Order
{
    [JsonPropertyName("orderId")]
    [XmlAttribute("orderId")]
    public int Id { get; set; }

    public int CustomerId { get; set; }

    [XmlElement("customer")]
    public Customer Customer { get; set; } = null!;

    [XmlArray("items")]
    [XmlArrayItem("item")]
    public List<OrderItem> Items { get; set; } = new();

    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }

    [JsonIgnore]
    [XmlIgnore]
    public string? Notes { get; set; }

    [XmlIgnore]
    [JsonIgnore]
    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
}
