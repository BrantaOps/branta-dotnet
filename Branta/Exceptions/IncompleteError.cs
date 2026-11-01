namespace Branta;

internal sealed class IncompleteError(string? message) : Exception(message)
{
}
