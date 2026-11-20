namespace Branta;

public class BrantaApiOptions
{
    public BrantaEnvironment? Environment { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>
    /// Secret from the Secrets tab in Guardrail. Signs <c>POST /v2/payments</c> with
    /// <c>X-HMAC-Signature</c> and <c>X-HMAC-Timestamp</c>. Needed when your platform has Require HMAC on.
    /// Server-side only. See https://developer.branta.pro/setup/receive/hmac-signing
    /// </summary>
    public string? HmacSecret { get; set; }
}
