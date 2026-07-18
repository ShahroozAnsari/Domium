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

    [Theory]
    [InlineData(1, "*/1 * * * * *")]
    [InlineData(5, "*/5 * * * * *")]
    [InlineData(30, "*/30 * * * * *")]
    [InlineData(60, "* * * * *")]
    [InlineData(300, "*/5 * * * *")]
    [InlineData(3600, "0 * * * *")]
    [InlineData(7200, "0 */2 * * *")]
    public void Interval_seconds_build_a_real_cron_expression(int intervalSeconds, string expected)
    {
        var options = new JobOptions { JobName = "X", IntervalSeconds = intervalSeconds };

        Assert.Equal(expected, options.ResolveCronExpression());
    }

    [Fact]
    public void Sub_minute_intervals_are_not_rounded_up_to_a_minute()
    {
        var options = new JobOptions { JobName = "X", IntervalSeconds = 5 };

        Assert.NotEqual("* * * * *", options.ResolveCronExpression());
        Assert.Equal("*/5 * * * * *", options.ResolveCronExpression());
    }

    [Fact]
    public void Interval_seconds_take_precedence_over_a_cron_expression()
    {
        var options = new JobOptions
        {
            JobName = "X",
            IntervalSeconds = 15,
            CronExpression = "0 0 * * *"
        };

        Assert.Equal("*/15 * * * * *", options.ResolveCronExpression());
    }

    [Fact]
    public void Cron_expression_is_used_when_no_interval_is_configured()
    {
        var options = new JobOptions { JobName = "X", CronExpression = "0 0 * * *" };

        Assert.Equal("0 0 * * *", options.ResolveCronExpression());
    }

    [Fact]
    public void Resolving_a_schedule_throws_when_neither_is_configured()
    {
        var options = new JobOptions { JobName = "Orphan" };

        var exception = Assert.Throws<InvalidOperationException>(() => options.ResolveCronExpression());

        Assert.Contains("Orphan", exception.Message);
    }

    [Fact]
    public void Run_at_startup_defaults_to_false()
    {
        var options = BuildOptions(AppSettings);

        Assert.False(options.Get("ConfiguredJob").RunAtStartup);
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
