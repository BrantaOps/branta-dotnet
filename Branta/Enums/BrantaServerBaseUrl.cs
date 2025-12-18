using Branta.Attributes;

namespace Branta.Enums;

public enum BrantaServerBaseUrl
{
    [Url("https://staging.branta.pro")]
    Staging = 0,

    [Url("https://guardrail.branta.pro")]
    Production = 1,

    [Url("http://localhost:3000")]
    Localhost = 2,
}
