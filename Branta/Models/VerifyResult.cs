namespace Branta;

public enum VerifyStatus
{
    Match,
    Unknown,
    Tampered,
    Incomplete
}

public sealed record VerifyResult
{
    public required VerifyStatus Status { get; init; }

    /// <summary>Counterparty payment record(s). May still include sibling rails that did not bind.</summary>
    public IReadOnlyList<Payment>? Payments { get; init; }

    /// <summary>
    /// Destinations this scan actually bound — plaintext, with <see cref="Destination.Type"/>.
    /// Use these for what to send; do not dig through <see cref="Payments"/> destinations.
    /// Present only on <see cref="VerifyStatus.Match"/>.
    /// </summary>
    public IReadOnlyList<Destination>? MatchedDestinations { get; init; }

    public required string VerifyUrl { get; init; }
}
