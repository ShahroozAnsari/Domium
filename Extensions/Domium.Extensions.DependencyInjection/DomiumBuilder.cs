using Domium.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Extensions.DependencyInjection;

public sealed class DomiumBuilder(IServiceCollection services, DomiumOptions options) : IDomiumBuilder
{
    public IServiceCollection Services { get; } = services ?? throw new ArgumentNullException(nameof(services));

    public DomiumOptions Options { get; } = options ?? throw new ArgumentNullException(nameof(options));
}
