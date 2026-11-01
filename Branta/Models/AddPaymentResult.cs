namespace Branta;

public sealed record AddPaymentResult(Payment Payment, string Secret, string VerifyUrl);
