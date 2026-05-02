namespace Branta.Enums;

/// <summary>
/// Controls the privacy posture for on-chain address lookups.
/// <list type="bullet">
///   <item>
///     <term><see cref="Strict"/></term>
///     <description>
///       Only ZK (zero-knowledge / encrypted) on-chain lookups are permitted.
///       Calling <c>GetPaymentsAsync</c> directly will throw a <c>BrantaPaymentException</c>;
///       plain-address branches inside <c>GetPaymentsByQrCodeAsync</c> will silently return an
///       empty list. POST operations (<c>AddPaymentAsync</c>) are also restricted: all destinations
///       must have <c>IsZk = true</c>, otherwise a <c>BrantaPaymentException</c> is thrown.
///     </description>
///   </item>
///   <item>
///     <term><see cref="Loose"/></term>
///     <description>
///       Both plain and ZK on-chain lookups are allowed. No restrictions are enforced.
///     </description>
///   </item>
/// </list>
/// </summary>
public enum PrivacyMode
{
    /// <summary>
    /// Only ZK (zero-knowledge / encrypted) on-chain lookups are permitted.
    /// </summary>
    Strict = 0,

    /// <summary>
    /// Both plain and ZK on-chain lookups are allowed.
    /// </summary>
    Loose = 1,
}
