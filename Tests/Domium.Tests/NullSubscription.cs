namespace Domium.Tests;

internal sealed class NullSubscription : IDisposable
{
    public static readonly NullSubscription Instance = new();

    public void Dispose()
    {
    }
}
