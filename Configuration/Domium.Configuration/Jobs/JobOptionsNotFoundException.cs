namespace Domium.Configuration.Jobs;

public sealed class JobOptionsNotFoundException(string jobName)
    : InvalidOperationException($"No job options were configured for job '{jobName}'.")
{
    public string JobName { get; } = jobName;
}
