using System.Text;
using Domium.Application.Abstractions.Job;
using Domium.Configuration.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Domium.Tests.DependencyInjection;

public sealed class JobOptionsTests
{
    private const string AppSettings = """
        {
          "DomiumJobs": [
            {
              "JobName": "ConfiguredJob",
              "CronExpression": "*/10 * * * * *"
            },
            {
              "JobName": "NightlyJob",
              "CronExpression": "0 0 * * *",
              "Enabled": false
            }
          ]
        }
        """;

    [Fact]
    public void Job_options_are_bound_from_the_json_section()
    {
        var options = BuildOptions(AppSettings);

        Assert.Equal(2, options.Jobs.Count);
        Assert.Contains(options.Jobs, job => job.JobName == "ConfiguredJob");
        Assert.Contains(options.Jobs, job => job.JobName == "NightlyJob");
    }

    [Fact]
    public void Get_returns_the_options_matching_the_exact_job_name()
    {
        var options = BuildOptions(AppSettings);

        Assert.Equal("*/10 * * * * *", options.Get("ConfiguredJob").CronExpression);
        Assert.Equal("0 0 * * *", options.Get("NightlyJob").CronExpression);
    }

    [Fact]
    public void Get_resolves_the_job_name_from_the_job_type()
    {
        var options = BuildOptions(AppSettings);

        Assert.Equal("*/10 * * * * *", options.Get<ConfiguredJob>().CronExpression);
    }

    [Fact]
    public void Enabled_defaults_to_true_when_absent_from_the_json()
    {
        var options = BuildOptions(AppSettings);

        Assert.True(options.Get("ConfiguredJob").Enabled);
    }

    [Fact]
    public void Enabled_is_bound_when_the_job_is_switched_off()
    {
        var options = BuildOptions(AppSettings);

        Assert.False(options.Get("NightlyJob").Enabled);
    }

    [Fact]
    public void Get_still_returns_a_disabled_job()
    {
        var options = BuildOptions(AppSettings);

        var nightly = options.Get("NightlyJob");

        Assert.Equal("0 0 * * *", nightly.CronExpression);
        Assert.False(nightly.Enabled);
    }

    [Fact]
    public void Get_throws_when_the_job_is_not_configured()
    {
        var options = BuildOptions(AppSettings);

        var exception = Assert.Throws<JobOptionsNotFoundException>(() => options.Get<MissingJob>());

        Assert.Equal(nameof(MissingJob), exception.JobName);
    }

    [Fact]
    public void Get_throws_when_the_section_is_absent()
    {
        var options = BuildOptions("{}");

        Assert.Empty(options.Jobs);
        Assert.Throws<JobOptionsNotFoundException>(() => options.Get("ConfiguredJob"));
    }

    [Fact]
    public void Get_does_not_match_a_different_case_of_the_job_name()
    {
        var options = BuildOptions(AppSettings);

        Assert.Throws<JobOptionsNotFoundException>(() => options.Get("configuredjob"));
    }

    private static DomiumJobOptions BuildOptions(string json)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(Encoding.UTF8.GetBytes(json)))
            .Build();

        var services = new ServiceCollection();
        services.AddDomiumJobOptions(configuration);

        using var provider = services.BuildServiceProvider();

        return provider.GetRequiredService<IOptions<DomiumJobOptions>>().Value;
    }

    private sealed class ConfiguredJob : IJob;

    private sealed class MissingJob : IJob;
}
