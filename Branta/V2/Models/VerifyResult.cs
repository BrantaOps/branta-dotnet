namespace Branta.V2.Models;

public enum VerifyStatus
{
    Match,
    Unknown,
    Tampered,
    Incomplete
}

public class VerifyResult
{
    public required VerifyStatus Status { get; init; }

    public IReadOnlyList<Payment>? Payments { get; init; }

    public required string VerifyUrl { get; init; }
}
