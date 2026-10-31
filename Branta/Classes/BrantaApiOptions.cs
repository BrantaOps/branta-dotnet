using Branta.Enums;

namespace Branta.Classes;

public class BrantaApiOptions
{
    public BrantaEnvironment? Environment { get; set; }

    public string? ApiKey { get; set; }

    public string? HmacSecret { get; set; }
}
