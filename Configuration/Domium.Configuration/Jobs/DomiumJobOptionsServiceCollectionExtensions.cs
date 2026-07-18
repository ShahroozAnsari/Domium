using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Domium.Configuration.Jobs;

public static class DomiumJobOptionsServiceCollectionExtensions
{
    public static IServiceCollection AddDomiumJobOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = DomiumJobOptions.SectionName)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        if (string.IsNullOrWhiteSpace(sectionName))
        {
            throw new ArgumentException("Job options section name cannot be empty.", nameof(sectionName));
        }

        services.Configure<DomiumJobOptions>(options =>
            options.Jobs = configuration.GetSection(sectionName).Get<List<JobOptions>>() ?? new List<JobOptions>());

        return services;
    }
}
