namespace Domium.Configuration.Jobs;

public sealed class JobOptions
{
    public string JobName { get; set; } = string.Empty;

    public string CronExpression { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;
}
