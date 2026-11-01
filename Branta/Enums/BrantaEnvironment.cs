namespace Branta;

public enum BrantaEnvironment
{
    [Url("https://staging.guardrail.branta.pro")]
    Staging = 0,

    [Url("https://guardrail.branta.pro")]
    Production = 1,

    [Url("http://localhost:3000")]
    Localhost = 2,
}
