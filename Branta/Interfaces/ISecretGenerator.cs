namespace Branta;

internal interface ISecretGenerator
{
    string Generate();
    bool DeterministicNonce { get; }
}
