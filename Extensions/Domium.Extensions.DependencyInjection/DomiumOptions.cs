using System;

namespace Domium.Extensions.DependencyInjection;

public sealed class DomiumOptions
{
    internal bool LoggingEnabled { get; private set; }

    internal bool ValidationEnabled { get; private set; }

    internal bool TransactionsEnabled { get; private set; }

    internal bool CachingEnabled { get; private set; }

    internal DomiumCachingOptions CachingOptions { get; } = new();

    public DomiumOptions UseLogging()
    {
        LoggingEnabled = true;
        return this;
    }

    public DomiumOptions UseValidation()
    {
        ValidationEnabled = true;
        return this;
    }

    public DomiumOptions UseTransactions()
    {
        TransactionsEnabled = true;
        return this;
    }

    public DomiumOptions UseCaching(Action<DomiumCachingOptions>? configure = null)
    {
        CachingEnabled = true;

        configure?.Invoke(CachingOptions);

        return this;
    }
}