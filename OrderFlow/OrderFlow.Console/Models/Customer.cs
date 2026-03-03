namespace OrderFlow.Console.Models;

public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public bool IsVip { get; set; }
    public decimal CreditLimit { get; set; }
}
