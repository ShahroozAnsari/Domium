using System.Reflection;

namespace Domium.Configuration;

public sealed class DomiumOptions
{
    private readonly List<Assembly> _applicationAssemblies = new();

    public bool LoadedAssemblyScanningEnabled { get; private set; } = true;

    public bool LoggingEnabled { get; private set; }

    public bool ValidationEnabled { get; private set; }

    public bool TransactionsEnabled { get; private set; }

    public bool CachingEnabled { get; private set; }

    public DomiumCachingOptions CachingOptions { get; } = new();

    public IReadOnlyCollection<Assembly> ApplicationAssemblies => _applicationAssemblies.AsReadOnly();

    public DomiumOptions UseLoadedAssemblyScanning(bool enabled = true)
    {
        LoadedAssemblyScanningEnabled = enabled;
        return this;
    }

    public DomiumOptions UseLogging(bool enabled = true)
    {
        LoggingEnabled = enabled;
        return this;
    }

    public DomiumOptions UseValidation(bool enabled = true)
    {
        ValidationEnabled = enabled;
        return this;
    }

    public DomiumOptions UseTransactions(bool enabled = true)
    {
        TransactionsEnabled = enabled;
        return this;
    }

    public DomiumOptions UseCaching(Action<DomiumCachingOptions>? configure = null, bool enabled = true)
    {
        CachingEnabled = enabled;

        if (enabled)
        {
            configure?.Invoke(CachingOptions);
        }

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
