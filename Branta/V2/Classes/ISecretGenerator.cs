namespace Branta.V2.Classes;

public interface ISecretGenerator
{
    string Generate();
    bool DeterministicNonce { get; }
}
