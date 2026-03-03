namespace OrderFlow.Console.Models;

public enum OrderStatus
{
    New,
    Validated,
    Processing,
    Completed,
    Cancelled
}

public enum ProductCategory
{
    Electronics,
    Clothing,
    Food,
    Books,
    Sports
}
