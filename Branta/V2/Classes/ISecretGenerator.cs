namespace Branta.V2.Classes;

public interface ISecretGenerator
{
    string Generate();
    bool DeterministicNonce { get; }
}

public class GuidSecretGenerator : ISecretGenerator
{
    public string Generate() => Guid.NewGuid().ToString();
    public bool DeterministicNonce => false;
}
