namespace Branta.Attributes;

[AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
public sealed class UrlAttribute(string url) : Attribute
{
    public string Url { get; } = url;
}