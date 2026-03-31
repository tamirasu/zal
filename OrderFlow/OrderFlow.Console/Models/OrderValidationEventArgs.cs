namespace OrderFlow.Console.Models;

public class OrderValidationEventArgs : EventArgs
{
    public Order Order { get; }
    public bool IsValid { get; }
    public IReadOnlyList<string> Errors { get; }

    public OrderValidationEventArgs(Order order, bool isValid, List<string> errors)
    {
        Order = order;
        IsValid = isValid;
        Errors = errors.AsReadOnly();
    }
}
