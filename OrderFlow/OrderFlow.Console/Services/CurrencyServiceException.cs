namespace OrderFlow.Console.Services;

public class CurrencyServiceException : Exception
{
    public int? StatusCode { get; }

    public CurrencyServiceException(string message, int? statusCode = null)
        : base(message)
    {
        StatusCode = statusCode;
    }
}
