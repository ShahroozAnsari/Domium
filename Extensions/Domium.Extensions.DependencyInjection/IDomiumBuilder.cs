using Microsoft.Extensions.DependencyInjection;

namespace Domium.Extensions.DependencyInjection;

public interface IDomiumBuilder
{
    IServiceCollection Services { get; }
    DomiumOptions Options { get; }
}