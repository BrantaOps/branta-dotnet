namespace Branta.V2.Classes;

internal interface ISecretGenerator
{
    string Generate();
    bool DeterministicNonce { get; }
}
