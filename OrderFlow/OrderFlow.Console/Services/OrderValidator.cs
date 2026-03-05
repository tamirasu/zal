using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

// Własny delegat dla reguły walidacyjnej
public delegate bool ValidationRule(Order order, out string errorMessage);

public class OrderValidator
{
    private const decimal MaxOrderAmount = 50000m;

    private readonly List<ValidationRule> _namedRules = new();
    private readonly List<Func<Order, bool>> _lambdaRules = new();
    private readonly List<string> _lambdaRuleNames = new();

    public OrderValidator()
    {
        // Reguły jako named methods
        _namedRules.Add(HasItems);
        _namedRules.Add(AmountUnderLimit);
        _namedRules.Add(AllQuantitiesPositive);

        // Reguły jako lambdy z Func<Order, bool>
        _lambdaRules.Add(o => o.CreatedAt <= DateTime.Now);
        _lambdaRuleNames.Add("Data zamówienia nie może być z przyszłości");

        _lambdaRules.Add(o => o.Status != OrderStatus.Cancelled);
        _lambdaRuleNames.Add("Zamówienie nie może być w statusie Cancelled");
    }

    // Named method #1 — zamówienie musi mieć pozycje
    private static bool HasItems(Order order, out string errorMessage)
    {
        if (order.Items == null || order.Items.Count == 0)
        {
            errorMessage = "Zamówienie nie zawiera żadnych pozycji.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    // Named method #2 — kwota nie przekracza limitu
    private static bool AmountUnderLimit(Order order, out string errorMessage)
    {
        if (order.TotalAmount > MaxOrderAmount)
        {
            errorMessage = $"Kwota zamówienia {order.TotalAmount:C} przekracza limit {MaxOrderAmount:C}.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    // Named method #3 — wszystkie ilości > 0
    private static bool AllQuantitiesPositive(Order order, out string errorMessage)
    {
        var invalid = order.Items?.FirstOrDefault(i => i.Quantity <= 0);
        if (invalid != null)
        {
            errorMessage = $"Pozycja '{invalid.Product?.Name}' ma nieprawidłową ilość: {invalid.Quantity}.";
            return false;
        }
        errorMessage = string.Empty;
        return true;
    }

    // Łączy wyniki obu mechanizmów walidacji
    public (bool IsValid, List<string> Errors) ValidateAll(Order order)
    {
        var errors = new List<string>();

        foreach (var rule in _namedRules)
        {
            if (!rule(order, out var msg))
                errors.Add(msg);
        }

        for (int i = 0; i < _lambdaRules.Count; i++)
        {
            if (!_lambdaRules[i](order))
                errors.Add(_lambdaRuleNames[i]);
        }

        return (errors.Count == 0, errors);
    }
}
