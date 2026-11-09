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
public sealed class InvalidCouponException:Exception
{
    public InvalidCouponException(string message) : base(message) { }
}
public sealed class DatabaseWriteException: Exception
{
    public DatabaseWriteException(string message) : base(message) { }
}
public sealed class SmtpUnavailableException : Exception
{
    public SmtpUnavailableException(string message) : base(message)
    {
    }
}
