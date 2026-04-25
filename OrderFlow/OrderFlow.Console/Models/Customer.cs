using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

[XmlType("customer")]
[XmlRoot("customer")]
public class Customer
{
    [XmlAttribute("id")]
    public int Id { get; set; }

    [XmlElement("fullName")]
    public string Name { get; set; } = string.Empty;

    public string? Email { get; set; }
    public string City { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    public decimal CreditLimit { get; set; }

    // Nawigacja EF Core — ignorowana w JSON/XML (brak circular ref)
    [JsonIgnore]
    [XmlIgnore]
    public List<Order> Orders { get; set; } = new();
}
