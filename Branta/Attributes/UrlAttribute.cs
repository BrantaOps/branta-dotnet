namespace Branta;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
internal sealed class UrlAttribute(string url) : Attribute
{
    public string Url { get; } = url;
}