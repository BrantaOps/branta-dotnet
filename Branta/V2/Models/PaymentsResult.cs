namespace Branta.V2.Models;

public class PaymentsResult
{
    public required List<Payment> Payments { get; set; } = [];

    public required string VerifyUrl { get; set; }
}
