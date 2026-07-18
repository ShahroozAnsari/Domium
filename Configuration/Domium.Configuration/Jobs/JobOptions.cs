namespace Domium.Configuration.Jobs;

public sealed class JobOptions
{
    public string JobName { get; set; } = string.Empty;

    public string CronExpression { get; set; } = string.Empty;

    public int IntervalSeconds { get; set; }

    public bool Enabled { get; set; } = true;

    public bool RunAtStartup { get; set; }

    public int RetryAttempts { get; set; }

    public bool DisableConcurrentExecution { get; set; }

    public string ResolveCronExpression()
    {
        if (IntervalSeconds > 0)
        {
            return BuildCronFromSeconds(IntervalSeconds);
        }

        if (!string.IsNullOrWhiteSpace(CronExpression))
        {
            return CronExpression;
        }

        throw new InvalidOperationException(
            $"Job '{JobName}' has neither IntervalSeconds nor CronExpression configured.");
    }

    private static string BuildCronFromSeconds(int intervalSeconds)
    {
        if (intervalSeconds < 60)
        {
            return $"*/{intervalSeconds} * * * * *";
        }

        if (intervalSeconds % 60 != 0)
        {
            throw new InvalidOperationException(
                "Job intervals of a minute or more must be a whole number of minutes.");
        }

        var minutes = intervalSeconds / 60;

        if (minutes == 1)
        {
            return "* * * * *";
        }

        if (minutes < 60)
        {
            return $"*/{minutes} * * * *";
        }

        if (minutes % 60 != 0)
        {
            throw new InvalidOperationException(
                "Job intervals of an hour or more must be a whole number of hours.");
        }

        var hours = minutes / 60;

        if (hours == 1)
        {
            return "0 * * * *";
        }

        if (hours < 24)
        {
            return $"0 */{hours} * * *";
        }

        throw new InvalidOperationException(
            "Job intervals must be less than 24 hours; use CronExpression for longer schedules.");
    }
}
