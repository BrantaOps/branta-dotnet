namespace Branta.Exceptions;

internal sealed class IncompleteError(string? message) : Exception(message)
{
}
