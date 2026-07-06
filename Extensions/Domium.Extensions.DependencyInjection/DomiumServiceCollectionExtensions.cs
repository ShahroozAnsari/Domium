using System.Runtime.CompilerServices;
using Domium.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Extensions.DependencyInjection;

public static class DomiumServiceCollectionExtensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IServiceCollection AddDomium(
        this IServiceCollection services,
        Action<DomiumOptions>? configure = null)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var options = new DomiumOptions();

        configure?.Invoke(options);

        return DomiumConfiguration.Register(services, options);
    }
}
