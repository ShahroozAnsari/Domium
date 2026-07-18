using Domium.Application.Abstractions.Job;

namespace Domium.Configuration.Jobs;

public sealed class DomiumJobOptions
{
    public const string SectionName = "DomiumJobs";

    public IList<JobOptions> Jobs { get; set; } = new List<JobOptions>();

    public JobOptions Get(string jobName)
    {
        if (string.IsNullOrWhiteSpace(jobName))
        {
            throw new ArgumentException("Job name cannot be empty.", nameof(jobName));
        }

        return Jobs.FirstOrDefault(job => string.Equals(job.JobName, jobName, StringComparison.Ordinal))
            ?? throw new JobOptionsNotFoundException(jobName);
    }

    public JobOptions Get<TJob>()
        where TJob : IJob => Get(typeof(TJob).Name);
}
