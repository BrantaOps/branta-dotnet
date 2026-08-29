namespace Branta.Exceptions;

public enum BrantaPaymentExceptionReason
{
    Tampered
}

public class BrantaPaymentException(string message, BrantaPaymentExceptionReason? reason = null) : Exception(message)
{
    public BrantaPaymentExceptionReason? Reason { get; } = reason;
}
