using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

[XmlType("product")]
[XmlRoot("product")]
public class Product
{
    [JsonPropertyName("productId")]
    [XmlAttribute("id")]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ProductCategory Category { get; set; }

    [JsonIgnore]
    [XmlIgnore]
    public string Description { get; set; } = string.Empty;

    public int StockQuantity { get; set; }
}
