namespace Domium.Caching.Abstractions.Models;

public sealed class DomiumCacheKeyDescriptor
{
    public DomiumCacheKeyDescriptor(
        string prefix,
        string category,
        string name,
        string scope,
        object? payload)
    {
        Prefix = prefix;
        Category = category;
        Name = name;
        Scope = scope;
        Payload = payload;
    }

    public string Prefix { get; }

    public string Category { get; }

    public string Name { get; }

    public string Scope { get; }

    public object? Payload { get; }
}
