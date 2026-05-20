using Domium.Caching.Abstractions.Models;

namespace Domium.Caching.Abstractions.Providers;

public interface IDomiumCacheKeyFactory
{
    string CreateKey(DomiumCacheKeyDescriptor descriptor);
}
