namespace OrderProcessing.Shared.Exceptions;

public sealed class DataFileCorruptedException : Exception
{
    public DataFileCorruptedException(string fileName,Exception innerException): base($"The JSON file '{fileName}' is corrupted.",innerException)
    {

    }
}
public class ShippingProviderUnavailableException : Exception
{
    public ShippingProviderUnavailableException(string message)
        : base(message)
    {
    }
}
public sealed class CustomerNotFoundException : Exception
{
    public CustomerNotFoundException(string message) : base(message) { }
}