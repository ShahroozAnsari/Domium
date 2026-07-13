using System.Reflection;

namespace Domium.Configuration;

public sealed class DomiumOptions
{
    private readonly List<Assembly> _applicationAssemblies = new();
    private readonly List<string> _applicationAssemblyNamePrefixes = new();

    public bool LoadedAssemblyScanningEnabled { get; private set; } = true;

    public bool LoggingEnabled { get; private set; }

    public bool ObservabilityEnabled { get; private set; }

    public bool ValidationEnabled { get; private set; }

    public bool TransactionsEnabled { get; private set; }

    public bool CachingEnabled { get; private set; }

    public bool IdempotencyEnabled { get; private set; }

    public DomiumCachingOptions CachingOptions { get; } = new();

    public DomiumIdempotencyOptions IdempotencyOptions { get; } = new();

    public IReadOnlyCollection<Assembly> ApplicationAssemblies => _applicationAssemblies.AsReadOnly();

    public IReadOnlyCollection<string> ApplicationAssemblyNamePrefixes =>
        _applicationAssemblyNamePrefixes.AsReadOnly();

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

    /// <summary>Adds the activity/metrics pipeline behaviors (outermost) for commands and queries.</summary>
    public DomiumOptions UseObservability(bool enabled = true)
    {
        ObservabilityEnabled = enabled;
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

    public DomiumOptions UseIdempotency(Action<DomiumIdempotencyOptions>? configure = null, bool enabled = true)
    {
        IdempotencyEnabled = enabled;

        if (enabled)
        {
            configure?.Invoke(IdempotencyOptions);
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

    public DomiumOptions AddApplicationAssemblyNamePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Application assembly name prefix cannot be empty.", nameof(prefix));
        }

        var normalizedPrefix = prefix.Trim();

        if (!_applicationAssemblyNamePrefixes.Contains(normalizedPrefix, StringComparer.Ordinal))
        {
            _applicationAssemblyNamePrefixes.Add(normalizedPrefix);
        }

        return this;
    }

    public DomiumOptions AddApplicationAssemblyNamePrefixes(params string[] prefixes)
    {
        if (prefixes == null)
        {
            throw new ArgumentNullException(nameof(prefixes));
        }

        foreach (var prefix in prefixes)
        {
            AddApplicationAssemblyNamePrefix(prefix);
        }

        return this;
    }
}
