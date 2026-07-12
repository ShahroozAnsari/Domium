namespace Domium.Querying.Abstractions;

/// <summary>
/// Comparison operators supported by Domium dynamic filtering.
/// </summary>
public enum FilterOperator
{
    Eq,
    Ne,
    Gt,
    Gte,
    Lt,
    Lte,
    Contains,
    StartsWith,
    EndsWith,

    /// <summary>Value is pipe-separated, for example "1|2|3".</summary>
    In,

    /// <summary>Value is "low|high", for example "10|100" (inclusive).</summary>
    Between
}
