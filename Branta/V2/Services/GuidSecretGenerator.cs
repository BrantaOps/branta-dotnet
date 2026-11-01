using Branta.V2.Classes;

namespace Branta.V2.Services;

internal class GuidSecretGenerator : ISecretGenerator
{
    public string Generate() => Guid.NewGuid().ToString();
    public bool DeterministicNonce => false;
}
