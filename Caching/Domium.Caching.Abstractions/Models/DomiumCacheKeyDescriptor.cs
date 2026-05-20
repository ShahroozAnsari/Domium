namespace Domium.Caching.Abstractions.Models;

public sealed class DomiumCacheKeyDescriptor(
    string prefix,
    string category,
    string name,
    string scope,
    object? payload)
{
    public string Prefix { get; } = prefix;

    public string Category { get; } = category;

    public string Name { get; } = name;

    public string Scope { get; } = scope;

    public object? Payload { get; } = payload;
}
