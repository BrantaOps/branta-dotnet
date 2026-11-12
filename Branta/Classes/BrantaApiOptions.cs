namespace Branta;

public class BrantaApiOptions
{
    public BrantaEnvironment? Environment { get; set; }

    public string? ApiKey { get; set; }

    /// <summary>
    /// Deprecated parent-platform HMAC. Prefer a shared <see cref="ApiKey"/> with
    /// <see cref="PaymentBuilder.SetChildPlatform"/>.
    /// </summary>
    [Obsolete("Prefer a shared ApiKey with PaymentBuilder.SetChildPlatform.")]
    public string? HmacSecret { get; set; }
}
