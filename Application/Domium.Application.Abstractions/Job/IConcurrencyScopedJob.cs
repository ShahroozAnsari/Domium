namespace Domium.Application.Abstractions.Job;

public interface IConcurrencyScopedJob : IJob
{
    string GetConcurrencyScope();
}
