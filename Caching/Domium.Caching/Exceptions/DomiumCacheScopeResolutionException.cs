using System;

namespace Domium.Caching.Exceptions;

/// <summary>
/// Represents an error that occurs when a cache scope cannot be resolved.
/// </summary>
public sealed class DomiumCacheScopeResolutionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomiumCacheScopeResolutionException"/> class.
    /// </summary>
    /// <param name="message">
    /// The exception message.
    /// </param>
    public DomiumCacheScopeResolutionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomiumCacheScopeResolutionException"/> class.
    /// </summary>
    /// <param name="message">
    /// The exception message.
    /// </param>
    /// <param name="innerException">
    /// The inner exception.
    /// </param>
    public DomiumCacheScopeResolutionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}