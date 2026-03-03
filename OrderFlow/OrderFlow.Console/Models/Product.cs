namespace OrderFlow.Console.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public ProductCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}
