using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderPipeline
{
    private readonly OrderValidator _validator;

    public event EventHandler<OrderStatusChangedEventArgs>? StatusChanged;
    public event EventHandler<OrderValidationEventArgs>? ValidationCompleted;

    public OrderPipeline(OrderValidator validator)
    {
        _validator = validator;
    }

    public bool ProcessOrder(Order order)
    {
        var (isValid, errors) = _validator.ValidateAll(order);
        ValidationCompleted?.Invoke(this, new OrderValidationEventArgs(order, isValid, errors));

        if (!isValid)
            return false;

        Transition(order, OrderStatus.Validated);
        Transition(order, OrderStatus.Processing);
        Transition(order, OrderStatus.Completed);

        return true;
    }

    private void Transition(Order order, OrderStatus newStatus)
    {
        var old = order.Status;
        order.Status = newStatus;
        StatusChanged?.Invoke(this, new OrderStatusChangedEventArgs(order, old, newStatus));
    }
}
