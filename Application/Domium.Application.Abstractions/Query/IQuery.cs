namespace Domium.Application.Abstractions.Query;

/// <summary>
/// A read-only operation. <typeparamref name="TResult"/> is unconstrained so queries can
/// return nullable references (e.g. <c>IQuery&lt;CustomerDto?&gt;</c>) and value types
/// (e.g. <c>IQuery&lt;int&gt;</c>).
/// </summary>
public interface IQuery<out TResult>
{
}
