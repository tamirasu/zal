namespace OrderFlow.Console.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public List<OrderItem> Items { get; set; } = new();
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? Notes { get; set; }

    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
}
