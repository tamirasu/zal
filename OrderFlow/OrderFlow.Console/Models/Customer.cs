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

    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    public decimal CreditLimit { get; set; }
}
