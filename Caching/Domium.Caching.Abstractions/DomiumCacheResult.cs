namespace Domium.Caching.Abstractions;

/// <summary>A cache lookup outcome that distinguishes "miss" from "cached null".</summary>
public readonly struct DomiumCacheResult<T>
{
    private DomiumCacheResult(bool found, T? value)
    {
        Found = found;
        Value = value;
    }

    public bool Found { get; }

    public T? Value { get; }

    public static DomiumCacheResult<T> Hit(T? value) => new(true, value);

    public static DomiumCacheResult<T> Miss() => new(false, default);
}
