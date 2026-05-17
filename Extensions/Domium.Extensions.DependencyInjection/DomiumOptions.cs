using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Domium.Extensions.DependencyInjection;

public sealed class DomiumOptions
{
    private readonly List<Assembly> _applicationAssemblies = new();

    internal bool LoggingEnabled { get; private set; }

    internal bool ValidationEnabled { get; private set; }

    internal bool TransactionsEnabled { get; private set; }

    internal bool CachingEnabled { get; private set; }

    internal DomiumCachingOptions CachingOptions { get; } = new();

    internal IReadOnlyCollection<Assembly> ApplicationAssemblies => _applicationAssemblies.AsReadOnly();

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

    public DomiumOptions AddApplicationAssembly(Assembly assembly)
    {
        if (assembly == null)
        {
            throw new ArgumentNullException(nameof(assembly));
        }

        if (!_applicationAssemblies.Contains(assembly))
        {
            _applicationAssemblies.Add(assembly);
        }

        return this;
    }

    public DomiumOptions AddApplicationAssemblies(params Assembly[] assemblies)
    {
        if (assemblies == null)
        {
            throw new ArgumentNullException(nameof(assemblies));
        }

        foreach (var assembly in assemblies)
        {
            AddApplicationAssembly(assembly);
        }

        return this;
    }
}
