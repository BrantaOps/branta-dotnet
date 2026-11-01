namespace Branta;

internal class GuidSecretGenerator : ISecretGenerator
{
    public string Generate() => Guid.NewGuid().ToString();
    public bool DeterministicNonce => false;
}
