using System;

namespace Domium.Querying.Abstractions;

/// <summary>
/// Marks a property as sortable through Domium dynamic querying.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class SortableAttribute : Attribute
{
}
